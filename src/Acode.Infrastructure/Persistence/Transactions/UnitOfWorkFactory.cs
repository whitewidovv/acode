// src/Acode.Infrastructure/Persistence/Transactions/UnitOfWorkFactory.cs
namespace Acode.Infrastructure.Persistence.Transactions;

using System.Data;
using Acode.Application.Interfaces.Persistence;
using Microsoft.Extensions.Logging;

/// <summary>
/// Factory for creating Unit of Work instances with transactions.
/// Creates UoW instances using the provided connection factory.
/// </summary>
public sealed class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<UnitOfWork> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkFactory"/> class.
    /// </summary>
    /// <param name="connectionFactory">Connection factory for creating database connections.</param>
    /// <param name="logger">Logger for UnitOfWork instances.</param>
    public UnitOfWorkFactory(
        IConnectionFactory connectionFactory,
        ILogger<UnitOfWork> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IUnitOfWork> CreateAsync(CancellationToken ct)
    {
        return await CreateAsync(IsolationLevel.ReadCommitted, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IUnitOfWork> CreateAsync(IsolationLevel isolationLevel, CancellationToken ct)
    {
        var connection = await _connectionFactory.CreateAsync(ct).ConfigureAwait(false);
        return new UnitOfWork(connection, isolationLevel, _logger);
    }
}
