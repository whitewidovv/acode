namespace Acode.Infrastructure.Truncation;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using Acode.Application.Truncation;

/// <summary>
/// File system-based artifact storage implementation.
/// Stores artifacts in a session-scoped directory.
/// </summary>
public sealed class FileSystemArtifactStore : IArtifactStore, IDisposable
{
    private readonly string sessionDirectory;
    private readonly string artifactDirectory;
    private readonly ConcurrentDictionary<string, Artifact> artifacts = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemArtifactStore"/> class.
    /// </summary>
    /// <param name="sessionDirectory">The session directory path.</param>
    public FileSystemArtifactStore(string sessionDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionDirectory);
        this.sessionDirectory = Path.GetFullPath(sessionDirectory);
        this.artifactDirectory = Path.Combine(this.sessionDirectory, ".acode", "artifacts");
    }

    /// <inheritdoc />
    public async Task<Artifact> CreateAsync(string content, string sourceTool, string contentType)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceTool);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        await writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            EnsureDirectoryExists();

            var artifactId = GenerateArtifactId();
            var extension = GetExtensionForContentType(contentType);
            var fileName = artifactId + extension;
            var filePath = Path.Combine(artifactDirectory, fileName);

            // Validate path stays within session directory
            var fullPath = Path.GetFullPath(filePath);
            if (!fullPath.StartsWith(sessionDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Artifact path escape attempt detected.");
            }

            // Write content to file
            await File.WriteAllTextAsync(filePath, content).ConfigureAwait(false);

            var preview = content.Length > 500
                ? content[..500] + "..."
                : content;

            var artifact = new Artifact
            {
                Id = artifactId,
                Size = content.Length,
                ContentType = contentType,
                SourceTool = sourceTool,
                CreatedAt = DateTimeOffset.UtcNow,
                FilePath = filePath,
                Preview = preview
            };

            artifacts[artifactId] = artifact;
            return artifact;
        }
        finally
        {
            writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetContentAsync(string artifactId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);

        if (!ValidateArtifactId(artifactId))
        {
            return null;
        }

        if (!artifacts.TryGetValue(artifactId, out var artifact))
        {
            return null;
        }

        if (!File.Exists(artifact.FilePath))
        {
            return null;
        }

        return await File.ReadAllTextAsync(artifact.FilePath).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetPartialContentAsync(string artifactId, int startLine, int endLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);

        var content = await GetContentAsync(artifactId).ConfigureAwait(false);
        if (content is null)
        {
            return null;
        }

        var lines = content.Split('\n');
        var start = Math.Max(0, startLine - 1); // Convert to 0-based
        var end = Math.Min(lines.Length, endLine);

        if (start >= lines.Length)
        {
            return string.Empty;
        }

        return string.Join('\n', lines.Skip(start).Take(end - start));
    }

    /// <inheritdoc />
    public Task<Artifact?> GetMetadataAsync(string artifactId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);

        if (!ValidateArtifactId(artifactId))
        {
            return Task.FromResult<Artifact?>(null);
        }

        return Task.FromResult(
            artifacts.TryGetValue(artifactId, out var artifact) ? artifact : null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<Artifact>> ListAsync()
    {
        return Task.FromResult<IReadOnlyCollection<Artifact>>(artifacts.Values.ToList());
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string artifactId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);

        if (!ValidateArtifactId(artifactId))
        {
            return false;
        }

        if (!artifacts.TryRemove(artifactId, out var artifact))
        {
            return false;
        }

        if (File.Exists(artifact.FilePath))
        {
            await Task.Run(() => File.Delete(artifact.FilePath)).ConfigureAwait(false);
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<int> CleanupAsync()
    {
        var count = artifacts.Count;

        foreach (var artifact in artifacts.Values)
        {
            if (File.Exists(artifact.FilePath))
            {
                await Task.Run(() => File.Delete(artifact.FilePath)).ConfigureAwait(false);
            }
        }

        artifacts.Clear();

        // Remove artifact directory if empty
        if (Directory.Exists(artifactDirectory) &&
            !Directory.EnumerateFileSystemEntries(artifactDirectory).Any())
        {
            Directory.Delete(artifactDirectory);
        }

        return count;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string artifactId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);

        if (!ValidateArtifactId(artifactId))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(artifacts.ContainsKey(artifactId));
    }

    /// <summary>
    /// Generates a unique artifact ID in the format art_{timestamp}_{random}.
    /// </summary>
    private static string GenerateArtifactId()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var randomBytes = new byte[6];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var randomPart = Convert.ToHexString(randomBytes).ToLowerInvariant();
        return $"art_{timestamp}_{randomPart}";
    }

    /// <summary>
    /// Validates an artifact ID has the expected format.
    /// </summary>
    private static bool ValidateArtifactId(string artifactId)
    {
        // Pattern: art_{timestamp}_{random}
        if (string.IsNullOrWhiteSpace(artifactId))
        {
            return false;
        }

        // Check for path traversal attempts
        if (artifactId.Contains("..", StringComparison.Ordinal) ||
            artifactId.Contains('/', StringComparison.Ordinal) ||
            artifactId.Contains('\\', StringComparison.Ordinal))
        {
            return false;
        }

        // Must start with "art_"
        if (!artifactId.StartsWith("art_", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the file extension for a content type.
    /// </summary>
    private static string GetExtensionForContentType(string contentType)
    {
        return contentType switch
        {
            "application/json" => ".json",
            "text/plain" => ".txt",
            "text/markdown" => ".md",
            "text/html" => ".html",
            "text/xml" or "application/xml" => ".xml",
            _ => ".txt"
        };
    }

    /// <summary>
    /// Ensures the artifact directory exists.
    /// </summary>
    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(artifactDirectory))
        {
            Directory.CreateDirectory(artifactDirectory);
        }
    }

    /// <summary>
    /// Disposes the artifact store and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        writeLock.Dispose();
        disposed = true;
    }
}
