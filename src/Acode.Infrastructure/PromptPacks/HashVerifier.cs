using Acode.Domain.PromptPacks;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Provides hash verification functionality for prompt packs.
/// </summary>
public sealed class HashVerifier
{
    private readonly ContentHasher _hasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashVerifier"/> class.
    /// </summary>
    /// <param name="hasher">The content hasher.</param>
    public HashVerifier(ContentHasher hasher)
    {
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
    }

    /// <summary>
    /// Verifies the integrity of a pack by comparing computed hash with manifest hash.
    /// </summary>
    /// <param name="manifest">The pack manifest containing the expected hash.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The verification result.</returns>
    public async Task<HashVerificationResult> VerifyAsync(
        PackManifest manifest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var componentPaths = manifest.Components.Select(c => c.Path);
        var actualHash = await _hasher.ComputeHashAsync(
            manifest.PackPath,
            componentPaths,
            cancellationToken).ConfigureAwait(false);

        var isValid = manifest.ContentHash?.Matches(actualHash) ?? true;

        return new HashVerificationResult(isValid, manifest.ContentHash, actualHash);
    }
}
