// tests/Acode.Infrastructure.Benchmarks/Program.cs
namespace Acode.Infrastructure.Benchmarks;

using BenchmarkDotNet.Running;

/// <summary>
/// Entry point for benchmark execution.
/// Run with: dotnet run -c Release --project tests/Acode.Infrastructure.Benchmarks
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
