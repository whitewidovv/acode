using Acode.Application.Configuration;
using Acode.Infrastructure.Configuration;
using BenchmarkDotNet.Attributes;

namespace Acode.Performance.Tests.Configuration;

/// <summary>
/// Performance benchmarks for configuration operations.
/// Validates performance targets from Task 002.b spec lines 873-886.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ConfigurationBenchmarks
{
    private string _minimalConfigYaml = string.Empty;
    private string _fullConfigYaml = string.Empty;
    private string _interpolationYaml = string.Empty;
    private YamlConfigReader _reader = null!;
    private ConfigValidator _validator = null!;
    private DefaultValueApplicator _applicator = null!;
    private EnvironmentInterpolator _interpolator = null!;

    [GlobalSetup]
    public void Setup()
    {
        _reader = new YamlConfigReader();
        _validator = new ConfigValidator();
        _applicator = new DefaultValueApplicator();
        _interpolator = new EnvironmentInterpolator();

        // Minimal config (target: parse < 10ms)
        _minimalConfigYaml = "schema_version: \"1.0.0\"\n";

        // Full config with all optional fields (target: parse < 30ms)
        _fullConfigYaml = @"
schema_version: ""1.0.0""
project:
  name: ""benchmark-project""
  type: ""dotnet""
  description: ""Performance benchmark configuration""
  languages:
    - ""csharp""
    - ""fsharp""
  paths:
    source:
      - ""./src""
    tests:
      - ""./tests""
    output:
      - ""./bin""
    docs:
      - ""./docs""
mode:
  default: ""local-only""
  allow_burst: true
  airgapped_lock: false
model:
  provider: ""ollama""
  name: ""codellama:13b""
  endpoint: ""http://localhost:11434""
  timeout_seconds: 120
  retry_count: 3
  parameters:
    temperature: 0.7
    max_tokens: 4096
    top_p: 0.95
commands:
  setup: ""dotnet restore""
  build: ""dotnet build""
  test: ""dotnet test""
  lint: ""dotnet format --verify-no-changes""
  format: ""dotnet format""
  start: ""dotnet run""
ignore:
  patterns:
    - ""bin/""
    - ""obj/""
    - ""*.user""
    - "".vs/""
    - ""*.log""
    - ""coverage/""
    - ""node_modules/""
    - ""dist/""
    - ""__pycache__/""
    - "".env""
  additional:
    - ""temp/""
    - ""*.tmp""
storage:
  mode: ""local_cache_only""
  local:
    type: ""sqlite""
    sqlite_path: "".acode/workspace.db""
  sync:
    enabled: false
    batch_size: 100
    conflict_policy: ""lww""
    retry_policy:
      max_attempts: 3
      backoff_ms: 1000
search:
  max_results: 100
  timeout_seconds: 30
";

        // Config for interpolation benchmarks (100 variables)
        var interpolationBuilder = new System.Text.StringBuilder("schema_version: \"1.0.0\"\nproject:\n  name: \"test\"\n");
        for (var i = 0; i < 100; i++)
        {
            interpolationBuilder.AppendLine($"  var_{i}: \"${{TEST_VAR_{i}}}\"");
            Environment.SetEnvironmentVariable($"TEST_VAR_{i}", $"value_{i}");
        }

        _interpolationYaml = interpolationBuilder.ToString();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // Clean up environment variables
        for (var i = 0; i < 100; i++)
        {
            Environment.SetEnvironmentVariable($"TEST_VAR_{i}", null);
        }
    }

    /// <summary>
    /// Benchmark 1: Parse minimal config.
    /// Target: &lt; 10ms.
    /// </summary>
    [Benchmark]
    public void ParseMinimalConfig()
    {
        _ = _reader.Read(_minimalConfigYaml);
    }

    /// <summary>
    /// Benchmark 2: Parse full config.
    /// Target: &lt; 30ms.
    /// </summary>
    [Benchmark]
    public void ParseFullConfig()
    {
        _ = _reader.Read(_fullConfigYaml);
    }

    /// <summary>
    /// Benchmark 3: Validate minimal config.
    /// Target: &lt; 10ms.
    /// </summary>
    [Benchmark]
    public void ValidateMinimalConfig()
    {
        var config = _reader.Read(_minimalConfigYaml);
        var configWithDefaults = _applicator.Apply(config);
        _ = _validator.Validate(configWithDefaults!);
    }

    /// <summary>
    /// Benchmark 4: Validate full config.
    /// Target: &lt; 30ms.
    /// </summary>
    [Benchmark]
    public void ValidateFullConfig()
    {
        var config = _reader.Read(_fullConfigYaml);
        _ = _validator.Validate(config);
    }

    /// <summary>
    /// Benchmark 5: Total load (parse + validate + defaults).
    /// Target: &lt; 100ms.
    /// </summary>
    [Benchmark]
    public void TotalLoadMinimalConfig()
    {
        var config = _reader.Read(_minimalConfigYaml);
        var configWithDefaults = _applicator.Apply(config);
        _ = _validator.Validate(configWithDefaults!);
    }

    /// <summary>
    /// Benchmark 6: Cached config access (object creation).
    /// Target: &lt; 1ms.
    /// Note: Testing object access performance after initial parse.
    /// </summary>
    [Benchmark]
    public void CachedConfigAccess()
    {
        var config = _reader.Read(_minimalConfigYaml);
        var configWithDefaults = _applicator.Apply(config);

        // Simulate cached access pattern
        _ = configWithDefaults!.Mode!.Default;
        _ = configWithDefaults.Model!.Provider;
        _ = configWithDefaults.SchemaVersion;
    }

    /// <summary>
    /// Benchmark 7: Memory - parse 1MB file.
    /// Target: &lt; 5MB peak memory.
    /// </summary>
    [Benchmark]
    public void ParseLargeFile()
    {
        // Generate ~1MB YAML content
        var largeYaml = new System.Text.StringBuilder("schema_version: \"1.0.0\"\nproject:\n  name: \"large-config\"\n");
        for (var i = 0; i < 10000; i++)
        {
            largeYaml.AppendLine($"  field_{i}: \"This is field {i} with some content to increase size\"");
        }

        _ = _reader.Read(largeYaml.ToString());
    }

    /// <summary>
    /// Benchmark 8: Memory - config object size.
    /// Target: &lt; 100KB.
    /// </summary>
    [Benchmark]
    public void ConfigObjectMemory()
    {
        var config = _reader.Read(_fullConfigYaml);
        var configWithDefaults = _applicator.Apply(config);

        // Force object allocation
        _ = configWithDefaults!.ToString();
    }

    /// <summary>
    /// Benchmark 9: Interpolation (100 variables).
    /// Target: &lt; 10ms.
    /// </summary>
    [Benchmark]
    public void InterpolateHundredVariables()
    {
        _ = _interpolator.Interpolate(_interpolationYaml);
    }

    /// <summary>
    /// Benchmark 10: Default value application.
    /// Target: &lt; 5ms.
    /// </summary>
    [Benchmark]
    public void ApplyDefaultValues()
    {
        var config = _reader.Read(_minimalConfigYaml);
        _ = _applicator.Apply(config);
    }
}
