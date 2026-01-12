using Acode.Domain.Security.PathProtection;

namespace Acode.Infrastructure.Security.PathProtection;

/// <summary>
/// Infrastructure implementation of ISymlinkResolver that uses the file system.
/// Wraps the Domain SymlinkResolver.
/// </summary>
public sealed class FileSystemSymlinkResolver : ISymlinkResolver
{
    private readonly SymlinkResolver _domainResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemSymlinkResolver"/> class.
    /// </summary>
    /// <param name="maxDepth">Maximum symlink chain depth to follow (default: 40).</param>
    public FileSystemSymlinkResolver(int maxDepth = 40)
    {
        _domainResolver = new SymlinkResolver(maxDepth);
    }

    /// <inheritdoc/>
    public SymlinkResolutionResult Resolve(string path)
    {
        return _domainResolver.Resolve(path);
    }
}
