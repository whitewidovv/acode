using BenchmarkDotNet.Running;

namespace Acode.Performance.Tests;

/// <summary>
/// Entry point for BenchmarkDotNet performance tests.
/// Run with: dotnet run -c Release --project tests/Acode.Performance.Tests.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
