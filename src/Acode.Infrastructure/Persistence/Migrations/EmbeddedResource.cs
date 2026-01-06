// src/Acode.Infrastructure/Persistence/Migrations/EmbeddedResource.cs
namespace Acode.Infrastructure.Persistence.Migrations;

/// <summary>
/// Represents an embedded migration resource.
/// </summary>
/// <param name="Name">The resource name (e.g., "001_initial.sql").</param>
/// <param name="Content">The SQL content of the migration.</param>
public sealed record EmbeddedResource(string Name, string Content);
