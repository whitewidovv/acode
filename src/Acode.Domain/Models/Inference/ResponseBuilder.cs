namespace Acode.Domain.Models.Inference;

using System;
using System.Collections.Generic;

/// <summary>
/// Fluent builder for constructing ChatResponse instances.
/// </summary>
/// <remarks>
/// FR-004b-078: ResponseBuilder MUST provide fluent API for constructing ChatResponse.
/// FR-004b-079 to FR-004b-088: WithX methods, auto-generation, validation.
/// </remarks>
public sealed class ResponseBuilder
{
    private string? id;
    private ChatMessage? message;
    private FinishReason? finishReason;
    private UsageInfo? usage;
    private ResponseMetadata? metadata;
    private string? model;
    private string? refusal;
    private IReadOnlyList<ContentFilterResult>? contentFilterResults;

    /// <summary>
    /// Sets the response ID.
    /// </summary>
    /// <param name="id">The response ID.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// FR-004b-079: ResponseBuilder MUST have WithId(string) method.
    /// </remarks>
    public ResponseBuilder WithId(string id)
    {
        this.id = id;
        return this;
    }

    /// <summary>
    /// Sets the message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// FR-004b-080: ResponseBuilder MUST have WithMessage(ChatMessage) method.
    /// </remarks>
    public ResponseBuilder WithMessage(ChatMessage message)
    {
        this.message = message;
        return this;
    }

    /// <summary>
    /// Sets the finish reason.
    /// </summary>
    /// <param name="finishReason">The finish reason.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// FR-004b-081: ResponseBuilder MUST have WithFinishReason(FinishReason) method.
    /// </remarks>
    public ResponseBuilder WithFinishReason(FinishReason finishReason)
    {
        this.finishReason = finishReason;
        return this;
    }

    /// <summary>
    /// Sets the usage information.
    /// </summary>
    /// <param name="usage">The usage information.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// FR-004b-082: ResponseBuilder MUST have WithUsage(UsageInfo) method.
    /// </remarks>
    public ResponseBuilder WithUsage(UsageInfo usage)
    {
        this.usage = usage;
        return this;
    }

    /// <summary>
    /// Sets the metadata.
    /// </summary>
    /// <param name="metadata">The metadata.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// FR-004b-083: ResponseBuilder MUST have WithMetadata(ResponseMetadata) method.
    /// </remarks>
    public ResponseBuilder WithMetadata(ResponseMetadata metadata)
    {
        this.metadata = metadata;
        return this;
    }

    /// <summary>
    /// Sets the model identifier.
    /// </summary>
    /// <param name="model">The model identifier.</param>
    /// <returns>The builder for chaining.</returns>
    public ResponseBuilder WithModel(string model)
    {
        this.model = model;
        return this;
    }

    /// <summary>
    /// Sets the refusal message.
    /// </summary>
    /// <param name="refusal">The refusal message.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// FR-004b-084: ResponseBuilder MUST have WithRefusal(string?) method.
    /// </remarks>
    public ResponseBuilder WithRefusal(string? refusal)
    {
        this.refusal = refusal;
        return this;
    }

    /// <summary>
    /// Sets the content filter results.
    /// </summary>
    /// <param name="results">The content filter results.</param>
    /// <returns>The builder for chaining.</returns>
    public ResponseBuilder WithContentFilterResults(IReadOnlyList<ContentFilterResult>? results)
    {
        this.contentFilterResults = results;
        return this;
    }

    /// <summary>
    /// Builds the ChatResponse with validation.
    /// </summary>
    /// <returns>A validated ChatResponse.</returns>
    /// <exception cref="InvalidOperationException">Thrown if required fields not set.</exception>
    /// <remarks>
    /// FR-004b-085: ResponseBuilder MUST have Build() method returning validated ChatResponse.
    /// FR-004b-086: ResponseBuilder MUST generate Id if not explicitly provided (GUID).
    /// FR-004b-087: ResponseBuilder MUST set Created timestamp automatically.
    /// FR-004b-088: ResponseBuilder MUST validate required fields on Build().
    /// </remarks>
    public ChatResponse Build()
    {
        // FR-004b-088: Validate required fields
        if (this.message is null)
        {
            throw new InvalidOperationException("Message is required.");
        }

        if (!this.finishReason.HasValue)
        {
            throw new InvalidOperationException("FinishReason is required.");
        }

        if (this.usage is null)
        {
            throw new InvalidOperationException("Usage is required.");
        }

        if (this.metadata is null)
        {
            throw new InvalidOperationException("Metadata is required.");
        }

        if (string.IsNullOrWhiteSpace(this.model))
        {
            throw new InvalidOperationException("Model is required.");
        }

        // FR-004b-086: Auto-generate Id if not set
        var responseId = string.IsNullOrWhiteSpace(this.id)
            ? Guid.NewGuid().ToString()
            : this.id;

        // FR-004b-087: Create response with auto-generated Created timestamp
        var response = new ChatResponse(
            Id: responseId,
            Message: this.message,
            FinishReason: this.finishReason.Value,
            Usage: this.usage,
            Metadata: this.metadata,
            Created: DateTimeOffset.UtcNow,
            Model: this.model,
            Refusal: this.refusal);

        // Set ContentFilterResults via init syntax if provided
        if (this.contentFilterResults is not null)
        {
            response = response with { ContentFilterResults = this.contentFilterResults };
        }

        return response;
    }
}
