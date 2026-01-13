namespace Acode.Infrastructure.Ollama.ToolCall;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Inference;
using Acode.Domain.Models.Inference;
using Acode.Infrastructure.Ollama.ToolCall.Exceptions;
using Acode.Infrastructure.Ollama.ToolCall.Models;

/// <summary>
/// Handles retry logic for failed tool call parsing.
/// Re-requests from model when tool calls cannot be parsed.
/// </summary>
/// <remarks>
/// FR-053: Retry on malformed tool call JSON (configurable).
/// Uses exponential backoff and custom retry prompts.
/// </remarks>
public sealed class ToolCallRetryHandler
{
    private readonly RetryConfig config;
    private readonly ToolCallParser parser;
    private readonly IModelProvider provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCallRetryHandler"/> class.
    /// </summary>
    /// <param name="config">Retry configuration.</param>
    /// <param name="parser">Tool call parser.</param>
    /// <param name="provider">Model provider for re-requesting.</param>
    public ToolCallRetryHandler(RetryConfig config, ToolCallParser parser, IModelProvider provider)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(provider);

        this.config = config;
        this.parser = parser;
        this.provider = provider;
    }

    /// <summary>
    /// Parses tool calls with automatic retry on failure.
    /// </summary>
    /// <param name="toolCalls">Tool calls to parse.</param>
    /// <param name="originalRequest">Original chat request for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parse result with all successful tool calls.</returns>
    /// <exception cref="ToolCallRetryExhaustedException">Thrown when all retry attempts fail.</exception>
    public async Task<ToolCallParseResult> ParseWithRetryAsync(
        OllamaToolCall[] toolCalls,
        ChatRequest originalRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolCalls);
        ArgumentNullException.ThrowIfNull(originalRequest);

        // First attempt: parse without retry
        var result = parser.Parse(toolCalls);

        // Save repairs from initial parse (important for accumulation across retries)
        var accumulatedRepairs = result.Repairs.ToList();

        if (result.AllSucceeded)
        {
            return result;
        }

        // Check if retries are enabled
        if (config.MaxRetries == 0)
        {
            throw new ToolCallRetryExhaustedException(
                $"Tool call parsing failed and retries are disabled (MaxRetries=0). Errors: {result.Errors.Count}");
        }

        // Retry loop with exponential backoff
        var attempt = 0;
        while (!result.AllSucceeded && attempt < config.MaxRetries)
        {
            attempt++;

            // Exponential backoff: delay * 2^(attempt-1)
            var delayMs = config.RetryDelayMs * (int)Math.Pow(2, attempt - 1);
            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);

            // Build retry prompt with error details
            var retryPrompt = BuildRetryPrompt(result.Errors.ToArray());

            // Build retry request by appending retry prompt
            var retryRequest = BuildRetryRequest(originalRequest, retryPrompt);

            // Re-invoke model
            var retryResponse = await provider.ChatAsync(retryRequest, cancellationToken).ConfigureAwait(false);

            // Extract tool calls from response
            // Note: This is simplified - in real implementation would need to handle
            // response format properly. For now, assume response contains corrected tool calls
            // in a format we can extract.
            var newToolCalls = ExtractToolCalls(retryResponse, toolCalls);

            // Parse new tool calls
            var retryResult = parser.Parse(newToolCalls);

            // Accumulate repairs from retry attempts
            foreach (var repair in retryResult.Repairs)
            {
                accumulatedRepairs.Add(repair);
            }

            // Update result but preserve accumulated repairs
            result = new ToolCallParseResult
            {
                ToolCalls = retryResult.ToolCalls,
                Errors = retryResult.Errors,
                Repairs = accumulatedRepairs
            };
        }

        // Check if we succeeded after retries
        if (!result.AllSucceeded)
        {
            throw new ToolCallRetryExhaustedException(
                $"Failed to parse tool calls after {attempt} retry attempts. " +
                $"Total errors: {result.Errors.Count}");
        }

        return result;
    }

    /// <summary>
    /// Builds retry prompt from parsing errors.
    /// </summary>
    /// <param name="errors">Parsing errors.</param>
    /// <returns>Retry prompt for model.</returns>
    public string BuildRetryPrompt(ToolCallError[] errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Length == 0)
        {
            return "Please provide valid tool call arguments.";
        }

        var firstError = errors[0];

        // Use template from config
        var prompt = config.RetryPromptTemplate
            .Replace("{error_message}", firstError.Message ?? "Unknown error", StringComparison.Ordinal)
            .Replace("{error_position}", firstError.ErrorPosition?.ToString() ?? "unknown", StringComparison.Ordinal)
            .Replace("{malformed_json}", firstError.RawArguments ?? string.Empty, StringComparison.Ordinal)
            .Replace("{tool_name}", firstError.ToolName ?? "unknown tool", StringComparison.Ordinal)
            .Replace("{schema_example}", "{}", StringComparison.Ordinal); // Simplified - would need actual schema

        return prompt;
    }

    /// <summary>
    /// Builds retry request by adding retry prompt to conversation.
    /// </summary>
    private static ChatRequest BuildRetryRequest(ChatRequest original, string retryPrompt)
    {
        // Append retry prompt as user message
        var messages = new List<ChatMessage>(original.Messages)
        {
            ChatMessage.CreateUser(retryPrompt),
        };

        return new ChatRequest(
            messages: messages.ToArray(),
            modelParameters: original.ModelParameters,
            tools: original.Tools,
            stream: false); // Never stream retries
    }

    /// <summary>
    /// Extracts tool calls from retry response.
    /// </summary>
    /// <remarks>
    /// Implements FR-048: Retry MUST extract new tool call from response.
    /// 1. Checks if response contains tool calls via Message.ToolCalls
    /// 2. Extracts each tool call from the response message
    /// 3. Maps domain ToolCall back to OllamaToolCall format for re-parsing
    /// If the model's retry response doesn't contain tool calls (e.g., returned
    /// a text explanation instead), falls back to the original tool calls for
    /// another retry attempt.
    /// </remarks>
    private static OllamaToolCall[] ExtractToolCalls(ChatResponse response, OllamaToolCall[] original)
    {
        // FR-048: Extract new tool calls from retry response
        if (response.Message.ToolCalls != null && response.Message.ToolCalls.Count > 0)
        {
            // Convert domain ToolCall back to OllamaToolCall for re-parsing
            return response.Message.ToolCalls.Select(tc => new OllamaToolCall
            {
                Id = tc.Id,
                Function = new OllamaFunction
                {
                    Name = tc.Name,
                    Arguments = tc.Arguments.GetRawText(),
                },
            }).ToArray();
        }

        // Model didn't return tool calls in retry (e.g., returned text explanation)
        // Return original for next retry attempt
        return original;
    }
}
