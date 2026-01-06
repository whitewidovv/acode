namespace Acode.Cli.Tests;

/// <summary>
/// Collection definition for tests that must run sequentially.
/// </summary>
/// <remarks>
/// Tests manipulating shared global state (like Console.Out) should be
/// in this collection to prevent parallelization issues.
/// </remarks>
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialCollection
{
}
