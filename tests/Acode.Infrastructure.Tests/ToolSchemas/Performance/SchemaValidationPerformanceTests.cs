namespace Acode.Infrastructure.Tests.ToolSchemas.Performance;

using System.Diagnostics;
using Acode.Domain.Models.Inference;
using Acode.Infrastructure.ToolSchemas.Providers;
using FluentAssertions;

/// <summary>
/// Performance tests for schema operations.
/// </summary>
public sealed class SchemaValidationPerformanceTests
{
    private readonly CoreToolsProvider provider = new();

    [Fact]
    public void Single_Tool_Access_Should_Complete_Under_1ms_Average()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);

        var stopwatch = Stopwatch.StartNew();
        const int iterations = 1000;

        for (int i = 0; i < iterations; i++)
        {
            var tool = tools["read_file"];
            var properties = tool.Parameters.GetProperty("properties");
            var path = properties.GetProperty("path");
            _ = path.GetProperty("type").GetString();
        }

        stopwatch.Stop();

        var averageMs = (double)stopwatch.ElapsedMilliseconds / iterations;
        averageMs.Should().BeLessThan(
            1.0,
            $"Single tool access should average <1ms (actual: {averageMs:F3}ms)");
    }

    [Fact]
    public void All_17_Schemas_Access_Should_Complete_Under_20ms()
    {
        var stopwatch = Stopwatch.StartNew();

        var tools = this.provider.GetToolDefinitions().ToList();

        // Access each schema's properties and count total property types accessed
        var totalPropertiesAccessed = tools
            .Select(tool => tool.Parameters.GetProperty("properties"))
            .SelectMany(props => props.EnumerateObject())
            .Count(prop => prop.Value.TryGetProperty("type", out _));

        stopwatch.Stop();

        totalPropertiesAccessed.Should().BeGreaterThan(0);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(
            20,
            $"Accessing all 17 schemas should complete in <20ms (actual: {stopwatch.ElapsedMilliseconds}ms)");
    }

    [Fact]
    public void GetToolDefinitions_Should_Be_Lazy_And_Fast()
    {
        var stopwatch = Stopwatch.StartNew();
        const int iterations = 100;

        for (int i = 0; i < iterations; i++)
        {
            // Each call should be fast as it uses yield return
            var tools = this.provider.GetToolDefinitions();
            var first = tools.First();
            first.Name.Should().NotBeNullOrEmpty();
        }

        stopwatch.Stop();

        var averageMs = (double)stopwatch.ElapsedMilliseconds / iterations;
        averageMs.Should().BeLessThan(
            5.0,
            $"Getting first tool definition should average <5ms (actual: {averageMs:F3}ms)");
    }

    [Fact]
    public void Schema_Enumeration_Should_Be_Consistent()
    {
        // Verify that multiple enumerations produce same results
        var firstRun = this.provider.GetToolDefinitions().Select(t => t.Name).ToList();
        var secondRun = this.provider.GetToolDefinitions().Select(t => t.Name).ToList();
        var thirdRun = this.provider.GetToolDefinitions().Select(t => t.Name).ToList();

        firstRun.Should().BeEquivalentTo(secondRun);
        secondRun.Should().BeEquivalentTo(thirdRun);
        firstRun.Should().HaveCount(17);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2001:AvoidCallingProblematicMethods",
        Justification = "GC.Collect() is intentional for memory measurement in performance tests")]
    public void Memory_Usage_Should_Be_Reasonable()
    {
        // Force GC before measurement to get accurate baseline
        // This is intentional for memory measurement tests
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(true);

        // Create multiple providers and materialize all schemas
        // Store both providers and tools to prevent GC optimization
        var providers = new List<CoreToolsProvider>();
        var allTools = new List<IReadOnlyList<ToolDefinition>>();
        for (int i = 0; i < 10; i++)
        {
            var p = new CoreToolsProvider();
            var tools = p.GetToolDefinitions().ToList();
            providers.Add(p);
            allTools.Add(tools);
        }

        var memoryAfter = GC.GetTotalMemory(true);
        var memoryUsed = memoryAfter - memoryBefore;

        // 10 providers with 17 schemas each should use less than 10MB
        var memoryUsedMB = memoryUsed / (1024.0 * 1024.0);
        memoryUsedMB.Should().BeLessThan(
            10.0,
            $"10 providers should use <10MB (actual: {memoryUsedMB:F2}MB)");

        // Keep references to prevent optimization
        providers.Count.Should().Be(10);
        allTools.Count.Should().Be(10);
    }
}
