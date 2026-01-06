// src/Acode.Infrastructure/Persistence/Connections/ConnectionFactorySelector.cs
namespace Acode.Infrastructure.Persistence.Connections;

using System.Data;
using Acode.Application.Interfaces.Persistence;
using Acode.Domain.Enums;
using Acode.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

/// <summary>
/// Selects the appropriate connection factory based on configuration.
/// Routes to SQLite or PostgreSQL factory based on provider setting.
/// </summary>
public sealed class ConnectionFactorySelector : IConnectionFactory
{
    private readonly IConnectionFactory _selectedFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionFactorySelector"/> class.
    /// </summary>
    /// <param name="options">Database configuration options.</param>
    /// <param name="sqliteFactory">SQLite connection factory.</param>
    /// <param name="postgresFactory">PostgreSQL connection factory.</param>
    public ConnectionFactorySelector(
        IOptions<DatabaseOptions> options,
        SqliteConnectionFactory sqliteFactory,
        PostgresConnectionFactory postgresFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(sqliteFactory);
        ArgumentNullException.ThrowIfNull(postgresFactory);

        var provider = options.Value.Provider.ToLowerInvariant();

        _selectedFactory = provider switch
        {
            "sqlite" => sqliteFactory,
            "postgresql" or "postgres" => postgresFactory,
            _ => throw new InvalidOperationException(
                $"Unsupported database provider: {provider}. Supported providers: sqlite, postgresql"),
        };
    }

    /// <inheritdoc/>
    public DatabaseType DatabaseType => _selectedFactory.DatabaseType;

    /// <inheritdoc/>
    public Task<IDbConnection> CreateAsync(CancellationToken ct) =>
        _selectedFactory.CreateAsync(ct);
}
