using Acode.Domain.Configuration;

namespace Acode.Application.Configuration;

/// <summary>
/// Redacts sensitive fields from configuration before displaying or logging.
/// Per NFR-002b-06 through NFR-002b-10.
/// </summary>
public sealed class ConfigRedactor
{
    /// <summary>
    /// Redacts sensitive fields from the configuration.
    /// </summary>
    /// <param name="config">The configuration to redact.</param>
    /// <returns>A new configuration instance with sensitive fields redacted.</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    /// <remarks>
    /// <para>
    /// Sensitive field names per NFR-002b-07: api_key, token, password, secret, dsn.
    /// </para>
    /// <para>
    /// Redaction format per NFR-002b-08: "[REDACTED:field_name]".
    /// </para>
    /// <para>
    /// This method returns a new configuration instance and does not mutate the original.
    /// </para>
    /// </remarks>
    public AcodeConfig Redact(AcodeConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        // Redact Storage.Remote.Postgres.Dsn if present
        var redactedStorage = RedactStorage(config.Storage);

        // Return new config with redacted storage
        return config with
        {
            Storage = redactedStorage
        };
    }

    private static StorageConfig? RedactStorage(StorageConfig? storage)
    {
        if (storage is null)
        {
            return null;
        }

        var redactedRemote = RedactRemoteStorage(storage.Remote);

        // If remote storage changed, return new StorageConfig
        if (redactedRemote != storage.Remote)
        {
            return storage with
            {
                Remote = redactedRemote
            };
        }

        return storage;
    }

    private static StorageRemoteConfig? RedactRemoteStorage(StorageRemoteConfig? remote)
    {
        if (remote is null)
        {
            return null;
        }

        var redactedPostgres = RedactPostgres(remote.Postgres);

        // If postgres changed, return new RemoteStorageConfig
        if (redactedPostgres != remote.Postgres)
        {
            return remote with
            {
                Postgres = redactedPostgres
            };
        }

        return remote;
    }

    private static StoragePostgresConfig? RedactPostgres(StoragePostgresConfig? postgres)
    {
        if (postgres is null)
        {
            return null;
        }

        // Redact DSN if it's not null or empty
        // Per NFR-002b-08: Format is "[REDACTED:field_name]"
        if (!string.IsNullOrEmpty(postgres.Dsn))
        {
            return postgres with
            {
                Dsn = "[REDACTED:dsn]"
            };
        }

        return postgres;
    }
}
