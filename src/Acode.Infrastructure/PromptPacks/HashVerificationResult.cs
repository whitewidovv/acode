using Acode.Domain.PromptPacks;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Represents the result of a hash verification operation.
/// </summary>
/// <param name="IsValid">Gets a value indicating whether the hash matched.</param>
/// <param name="ExpectedHash">Gets the expected hash from the manifest.</param>
/// <param name="ActualHash">Gets the actual computed hash.</param>
public sealed record HashVerificationResult(
    bool IsValid,
    ContentHash? ExpectedHash,
    ContentHash ActualHash);
