using Acode.Application.Security;
using Acode.Domain.Security.PathProtection;
using Acode.Infrastructure.Security;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Acode.Performance.Tests.Security;

/// <summary>
/// Performance benchmarks for path protection validation.
/// Gap #30 - Verify performance targets are met.
/// </summary>
/// <remarks>
/// Target: Single path check &lt; 1ms (NFR-003b-010).
/// </remarks>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class PathMatchingBenchmarks
{
    private IProtectedPathValidator _validator = null!;
    private IPathMatcher _pathMatcher = null!;
    private string[] _testPaths = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create validator with all components
        var pathMatcher = new GlobMatcher(caseSensitive: false);
        var pathNormalizer = new PathNormalizer();
        var symlinkResolver = new SymlinkResolver();
        _validator = new ProtectedPathValidator(pathMatcher, pathNormalizer, symlinkResolver);

        // Create path matcher for pattern-specific benchmarks
        _pathMatcher = pathMatcher;

        // Prepare test paths for bulk benchmarks
        _testPaths = new[]
        {
            "src/Program.cs",
            "README.md",
            ".ssh/id_rsa",
            ".env",
            ".aws/credentials",
            "tests/UnitTest.cs",
            "package.json",
            ".gitignore",
            "docs/README.md",
            "config/app.json"
        };
    }

    /// <summary>
    /// Benchmark 1: Single path check (target: &lt; 1ms).
    /// Validates that checking a single path meets performance target.
    /// </summary>
    [Benchmark(Description = "Single Path Check - Normal Source File")]
    public void SinglePathCheck_NormalFile()
    {
        _ = _validator.Validate("src/Program.cs");
    }

    /// <summary>
    /// Benchmark 2: Single path check against protected path.
    /// Verifies performance when path is blocked.
    /// </summary>
    [Benchmark(Description = "Single Path Check - Protected SSH Key")]
    public void SinglePathCheck_ProtectedSshKey()
    {
        _ = _validator.Validate(".ssh/id_rsa");
    }

    /// <summary>
    /// Benchmark 3: Single path check with normalization.
    /// Tests performance with path traversal that needs normalization.
    /// </summary>
    [Benchmark(Description = "Single Path Check - With Normalization")]
    public void SinglePathCheck_WithNormalization()
    {
        _ = _validator.Validate(".ssh/foo/../id_rsa");
    }

    /// <summary>
    /// Benchmark 4: 1000 path checks (throughput test).
    /// Measures throughput for bulk validation.
    /// </summary>
    [Benchmark(Description = "1000 Path Checks - Mixed Paths")]
    public void ThousandPathChecks()
    {
        for (var i = 0; i < 100; i++)
        {
            foreach (var path in _testPaths)
            {
                _ = _validator.Validate(path);
            }
        }
    }

    /// <summary>
    /// Benchmark 5: Exact pattern matching.
    /// Tests performance of exact path matching.
    /// </summary>
    [Benchmark(Description = "Pattern Match - Exact Path")]
    public void PatternMatch_ExactPath()
    {
        _ = _pathMatcher.Matches(".ssh/id_rsa", ".ssh/id_rsa");
    }

    /// <summary>
    /// Benchmark 6: Single wildcard matching.
    /// Tests performance of * wildcard.
    /// </summary>
    [Benchmark(Description = "Pattern Match - Single Wildcard")]
    public void PatternMatch_SingleWildcard()
    {
        _ = _pathMatcher.Matches(".ssh/id_*", ".ssh/id_rsa");
    }

    /// <summary>
    /// Benchmark 7: Double glob matching.
    /// Tests performance of ** recursive wildcard.
    /// </summary>
    [Benchmark(Description = "Pattern Match - Double Glob")]
    public void PatternMatch_DoubleGlob()
    {
        _ = _pathMatcher.Matches("**/.env", "path/to/.env");
    }

    /// <summary>
    /// Benchmark 8: Character class matching.
    /// Tests performance of [abc] character classes.
    /// </summary>
    [Benchmark(Description = "Pattern Match - Character Class")]
    public void PatternMatch_CharacterClass()
    {
        _ = _pathMatcher.Matches("id_[re]*", "id_rsa");
    }

    /// <summary>
    /// Benchmark 9: Complex pattern with multiple wildcards.
    /// Tests performance of patterns with multiple wildcards.
    /// </summary>
    [Benchmark(Description = "Pattern Match - Complex Pattern")]
    public void PatternMatch_ComplexPattern()
    {
        _ = _pathMatcher.Matches("**/config/*.json", "path/to/config/app.json");
    }

    /// <summary>
    /// Benchmark 10: ReDoS resistance - pathological input.
    /// CRITICAL: Verifies linear-time algorithm prevents ReDoS.
    /// </summary>
    /// <remarks>
    /// This benchmark tests a pathological input that would cause
    /// exponential backtracking in regex-based matchers.
    /// Our linear-time algorithm should complete quickly.
    /// </remarks>
    [Benchmark(Description = "ReDoS Resistance - Pathological Input")]
    public void RedosResistance_PathologicalInput()
    {
        // Pathological pattern: multiple wildcards
        var pattern = "a*b*c*d*e*f*g*h*i*j*k*l*m*n*o*p*q*r*s*t*u*v*w*x*y*z*";
        var path = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"; // No match, worst case

        _ = _pathMatcher.Matches(pattern, path);
    }

    /// <summary>
    /// Benchmark 11: ReDoS resistance - nested double globs.
    /// Tests performance with multiple ** patterns.
    /// </summary>
    [Benchmark(Description = "ReDoS Resistance - Nested Double Globs")]
    public void RedosResistance_NestedDoubleGlobs()
    {
        var pattern = "**/a/**/b/**/c/**/d/**";
        var path = "x/y/z/a/p/q/r/s/t/u/v/w/x/y/z";

        _ = _pathMatcher.Matches(pattern, path);
    }

    /// <summary>
    /// Benchmark 12: Full denylist scan.
    /// Measures performance of checking path against all 118 denylist entries.
    /// </summary>
    [Benchmark(Description = "Full Denylist Scan - Normal Source File")]
    public void FullDenylistScan_NormalFile()
    {
        // This tests the full validator which scans all denylist entries
        _ = _validator.Validate("src/services/UserService.cs");
    }

    /// <summary>
    /// Benchmark 13: Full denylist scan with early match.
    /// Measures performance when path matches early in denylist.
    /// </summary>
    [Benchmark(Description = "Full Denylist Scan - Early Match")]
    public void FullDenylistScan_EarlyMatch()
    {
        // SSH keys are early in the denylist
        _ = _validator.Validate(".ssh/id_rsa");
    }

    /// <summary>
    /// Benchmark 14: Case-sensitive matching.
    /// Tests performance of case-sensitive pattern matching.
    /// </summary>
    [Benchmark(Description = "Pattern Match - Case Sensitive")]
    public void PatternMatch_CaseSensitive()
    {
        var caseSensitiveMatcher = new GlobMatcher(caseSensitive: true);
        _ = caseSensitiveMatcher.Matches(".SSH/id_rsa", ".ssh/id_rsa");
    }

    /// <summary>
    /// Benchmark 15: Case-insensitive matching.
    /// Tests performance of case-insensitive pattern matching (Windows default).
    /// </summary>
    [Benchmark(Description = "Pattern Match - Case Insensitive")]
    public void PatternMatch_CaseInsensitive()
    {
        var caseInsensitiveMatcher = new GlobMatcher(caseSensitive: false);
        _ = caseInsensitiveMatcher.Matches(".SSH/id_rsa", ".ssh/id_rsa");
    }

    /// <summary>
    /// Benchmark 16: Very long path.
    /// Tests performance with paths > 260 characters.
    /// </summary>
    [Benchmark(Description = "Long Path - 500 Characters")]
    public void LongPath_500Characters()
    {
        var longPath = string.Join("/", Enumerable.Repeat("verylongdirectoryname", 20));
        _ = _validator.Validate(longPath);
    }

    /// <summary>
    /// Benchmark 17: Deep nesting with multiple dots.
    /// Tests normalization performance with deep directory traversal.
    /// </summary>
    [Benchmark(Description = "Normalization - Deep Directory Traversal")]
    public void Normalization_DeepTraversal()
    {
        _ = _validator.Validate("a/b/c/../../d/../e/./f/../g/file.txt");
    }

    /// <summary>
    /// Benchmark 18: Unicode paths.
    /// Tests performance with Unicode characters in paths.
    /// </summary>
    [Benchmark(Description = "Unicode Path - International Characters")]
    public void UnicodePath_InternationalCharacters()
    {
        _ = _validator.Validate("文件/путь/αρχείο/文档.txt");
    }

    /// <summary>
    /// Benchmark 19: Worst-case pattern matching.
    /// Tests worst-case scenario: path that doesn't match any pattern
    /// (requires scanning entire denylist).
    /// </summary>
    [Benchmark(Description = "Worst Case - No Match After Full Scan")]
    public void WorstCase_NoMatchFullScan()
    {
        // Path that won't match any denylist entry
        _ = _validator.Validate("src/services/business/logic/implementation/UserService.cs");
    }

    /// <summary>
    /// Benchmark 20: Environment file glob matching.
    /// Tests common pattern **/.env* performance.
    /// </summary>
    [Benchmark(Description = "Pattern Match - Environment File Glob")]
    public void PatternMatch_EnvironmentFileGlob()
    {
        _ = _pathMatcher.Matches("**/.env*", "path/to/project/.env.production");
    }
}
