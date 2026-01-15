using System.Text.Json;
using Acode.Infrastructure.Vllm.Exceptions;
using Acode.Infrastructure.Vllm.Models;

namespace Acode.Infrastructure.Vllm.Client.Serialization;

/// <summary>
/// Parser for vLLM non-streaming responses.
/// </summary>
/// <remarks>
/// FR-032 through FR-040: VllmResponseParser implementation.
/// Parses and validates vLLM API responses with proper error handling.
/// </remarks>
public static class VllmResponseParser
{
    /// <summary>
    /// Parses a non-streaming response JSON into VllmResponse.
    /// </summary>
    /// <param name="json">JSON string.</param>
    /// <returns>Parsed response.</returns>
    /// <exception cref="VllmParseException">Failed to parse or validate response.</exception>
    public static VllmResponse Parse(string json)
    {
        try
        {
            // FR-032: Deserialize complete JSON response using source generators
            var response = JsonSerializer.Deserialize<VllmResponse>(
                json,
                VllmJsonSerializerContext.Default.VllmResponse);

            if (response == null)
            {
                throw new VllmParseException("Response was null after deserialization");
            }

            // FR-040: Validate required fields present
            if (response.Choices == null || response.Choices.Count == 0)
            {
                throw new VllmParseException("Response missing required 'choices' array");
            }

            // FR-033: Extract choices array (already done by deserializer)
            // FR-034: Extract message from first choice
            var firstChoice = response.Choices[0];
            if (firstChoice.Message == null)
            {
                throw new VllmParseException("First choice missing required 'message' field");
            }

            // FR-035: Extract content from message (validated by model)
            // FR-036: Extract tool_calls from message (optional, handled by model)
            // FR-037: Extract finish_reason from choice (validated by model)
            // FR-038: Extract usage from response (optional, handled by model)
            // FR-039: Handle missing optional fields (handled by JsonIgnoreCondition.WhenWritingNull)
            return response;
        }
        catch (JsonException ex)
        {
            throw new VllmParseException($"Failed to parse vLLM response: {ex.Message}", ex);
        }
    }
}
