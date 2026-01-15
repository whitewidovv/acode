using System.Text.Json;

namespace Acode.Infrastructure.Vllm.Health.Errors;

/// <summary>
/// Parses vLLM error responses (OpenAI format).
/// </summary>
public sealed class VllmErrorParser
{
    /// <summary>
    /// Parses a vLLM error response JSON.
    /// </summary>
    /// <param name="json">The error response JSON.</param>
    /// <returns>Parsed error information.</returns>
    public VllmErrorInfo Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new VllmErrorInfo { Message = "Empty error response" };
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("error", out var errorElement))
            {
                return new VllmErrorInfo { Message = "No error object in response" };
            }

            var message = errorElement.TryGetProperty("message", out var msgProp)
                ? msgProp.GetString() ?? "Unknown error"
                : "Unknown error";

            var type = errorElement.TryGetProperty("type", out var typeProp)
                ? typeProp.GetString()
                : null;

            var code = errorElement.TryGetProperty("code", out var codeProp)
                ? codeProp.GetString()
                : null;

            var param = errorElement.TryGetProperty("param", out var paramProp)
                ? paramProp.GetString()
                : null;

            return new VllmErrorInfo
            {
                Message = message,
                Type = type,
                Code = code,
                Param = param
            };
        }
        catch (JsonException)
        {
            return new VllmErrorInfo { Message = "Malformed error response JSON" };
        }
    }
}
