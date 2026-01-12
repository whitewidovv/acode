using Acode.Domain.Security.PathProtection;

namespace Acode.Infrastructure.Security.PathProtection;

/// <summary>
/// Infrastructure implementation of IPathNormalizer that uses the file system.
/// Wraps the Domain PathNormalizer and adds file system-specific normalization.
/// </summary>
public sealed class FileSystemPathNormalizer : IPathNormalizer
{
    private readonly PathNormalizer _domainNormalizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemPathNormalizer"/> class.
    /// </summary>
    public FileSystemPathNormalizer()
    {
        _domainNormalizer = new PathNormalizer();
    }

    /// <inheritdoc/>
    public string Normalize(string path)
    {
        // First use domain normalizer
        var normalized = _domainNormalizer.Normalize(path);

        // Then use System.IO.Path for file system specifics
        try
        {
            normalized = Path.GetFullPath(normalized);
        }
        catch
        {
            // If GetFullPath fails, return domain-normalized path
            // This can happen with invalid path characters
        }

        return normalized;
    }
}
