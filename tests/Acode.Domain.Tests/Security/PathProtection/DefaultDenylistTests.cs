namespace Acode.Domain.Tests.Security.PathProtection;

using Acode.Domain.Security.PathProtection;
using FluentAssertions;

/// <summary>
/// Comprehensive tests for DefaultDenylist verifying all required entries.
/// Spec: task-003b lines 817-1125.
/// </summary>
public class DefaultDenylistTests
{
    private readonly IReadOnlyList<DenylistEntry> _entries = DefaultDenylist.Entries;

    [Fact]
    public void Should_Include_All_SSH_Paths()
    {
        // Arrange - Spec lines 855-868
        var expectedPatterns = new[]
        {
            "~/.ssh/",
            "~/.ssh/id_*",
            "~/.ssh/id_rsa",
            "~/.ssh/id_ed25519",
            "~/.ssh/id_ecdsa",
            "~/.ssh/id_dsa",
            "~/.ssh/authorized_keys",
            "~/.ssh/known_hosts",
            "~/.ssh/config",
            @"%USERPROFILE%\.ssh\",
            @"%USERPROFILE%\.ssh\id_*"
        };

        // Act
        var sshEntries = _entries
            .Where(e => e.Category == PathCategory.SshKeys)
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            sshEntries.Should().Contain(
                pattern,
                because: $"SSH path {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_GPG_Paths()
    {
        // Arrange - Spec lines 885-894
        var expectedPatterns = new[]
        {
            "~/.gnupg/",
            "~/.gnupg/private-keys-v1.d/",
            "~/.gnupg/secring.gpg",
            @"%APPDATA%\gnupg\"
        };

        // Act
        var gpgEntries = _entries
            .Where(e => e.Category == PathCategory.GpgKeys)
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            gpgEntries.Should().Contain(
                pattern,
                because: $"GPG path {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_AWS_Paths()
    {
        // Arrange - Spec lines 911-921
        var expectedPatterns = new[]
        {
            "~/.aws/",
            "~/.aws/credentials",
            "~/.aws/config",
            @"%USERPROFILE%\.aws\",
            @"%USERPROFILE%\.aws\credentials"
        };

        // Act
        var awsEntries = _entries
            .Where(e => e.Category == PathCategory.CloudCredentials)
            .Where(e => e.Pattern.Contains("aws", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            awsEntries.Should().Contain(
                pattern,
                because: $"AWS path {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_Azure_Paths()
    {
        // Arrange - Spec lines 939-948
        var expectedPatterns = new[]
        {
            "~/.azure/",
            "~/.azure/credentials",
            "~/.azure/accessTokens.json",
            @"%USERPROFILE%\.azure\"
        };

        // Act
        var azureEntries = _entries
            .Where(e => e.Category == PathCategory.CloudCredentials)
            .Where(e => e.Pattern.Contains("azure", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            azureEntries.Should().Contain(
                pattern,
                because: $"Azure path {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_GCloud_Paths()
    {
        // Arrange - Spec lines 966-976
        var expectedPatterns = new[]
        {
            "~/.config/gcloud/",
            "~/.config/gcloud/credentials.db",
            "~/.config/gcloud/access_tokens.db",
            "~/.config/gcloud/application_default_credentials.json",
            @"%APPDATA%\gcloud\"
        };

        // Act
        var gcloudEntries = _entries
            .Where(e => e.Category == PathCategory.CloudCredentials)
            .Where(e => e.Pattern.Contains("gcloud", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            gcloudEntries.Should().Contain(
                pattern,
                because: $"GCloud path {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_Kube_Paths()
    {
        // Arrange - Spec lines 994-1002
        var expectedPatterns = new[]
        {
            "~/.kube/",
            "~/.kube/config",
            @"%USERPROFILE%\.kube\"
        };

        // Act
        var kubeEntries = _entries
            .Where(e => e.Category == PathCategory.CloudCredentials)
            .Where(e => e.Pattern.Contains("kube", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            kubeEntries.Should().Contain(
                pattern,
                because: $"Kubernetes path {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_PackageManager_Paths()
    {
        // Arrange - Package manager credential files
        var expectedPatterns = new[]
        {
            "~/.netrc",
            "~/.npmrc",
            "~/.pypirc",
            "~/.nuget/NuGet.Config",
            "~/.gem/credentials",
            "~/.cargo/credentials",
            "~/.composer/auth.json",
            "~/.m2/settings.xml",
            "~/.gradle/gradle.properties",
            "~/.config/gh/hosts.yml"
        };

        // Act
        var packageManagerEntries = _entries
            .Where(e => e.Category == PathCategory.PackageManagerCredentials)
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            packageManagerEntries.Should().Contain(
                pattern,
                because: $"Package manager path {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_Git_Paths()
    {
        // Arrange - Git credential files
        var expectedPatterns = new[]
        {
            "~/.gitconfig",
            "~/.git-credentials"
        };

        // Act
        var gitEntries = _entries
            .Where(e => e.Category == PathCategory.GitCredentials)
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            gitEntries.Should().Contain(
                pattern,
                because: $"Git credential path {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_System_Unix_Paths()
    {
        // Arrange - Unix system paths
        var expectedPatterns = new[]
        {
            "/etc/",
            "/etc/passwd",
            "/etc/shadow",
            "/etc/sudoers",
            "/etc/sudoers.d/",
            "/etc/ssh/",
            "/etc/ssl/private/",
            "/root/",
            "/var/log/"
        };

        // Act
        var unixSystemEntries = _entries
            .Where(e => e.Category == PathCategory.SystemFiles)
            .Where(e => e.Platforms.Contains(Platform.Linux) || e.Platforms.Contains(Platform.MacOS))
            .Where(e => e.Pattern.StartsWith("/"))
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            unixSystemEntries.Should().Contain(
                pattern,
                because: $"Unix system path {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_System_Windows_Paths()
    {
        // Arrange - Windows system paths
        var expectedPatterns = new[]
        {
            @"C:\Windows\",
            @"C:\Windows\System32\",
            @"C:\Windows\SysWOW64\",
            @"C:\ProgramData\",
            @"C:\Users\*\AppData\",
            @"HKEY_*"
        };

        // Act
        var windowsSystemEntries = _entries
            .Where(e => e.Category == PathCategory.SystemFiles)
            .Where(e => e.Platforms.Contains(Platform.Windows))
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            windowsSystemEntries.Should().Contain(
                pattern,
                because: $"Windows system path {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_System_MacOS_Paths()
    {
        // Arrange - macOS system paths
        var expectedPatterns = new[]
        {
            "/System/",
            "/Library/",
            "~/Library/",
            "~/Library/Keychains/",
            "/private/var/"
        };

        // Act
        var macosSystemEntries = _entries
            .Where(e => e.Category == PathCategory.SystemFiles)
            .Where(e => e.Platforms.Contains(Platform.MacOS))
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            macosSystemEntries.Should().Contain(
                pattern,
                because: $"macOS system path {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_EnvFile_Patterns()
    {
        // Arrange - Spec lines 1020-1032
        var expectedPatterns = new[]
        {
            ".env",
            ".env.*",
            ".env.local",
            ".env.development",
            ".env.production",
            "**/.env",
            "**/.env.*"
        };

        // Act
        var envEntries = _entries
            .Where(e => e.Category == PathCategory.EnvironmentFiles)
            .Where(e => e.Pattern.Contains(".env", StringComparison.Ordinal))
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            envEntries.Should().Contain(
                pattern,
                because: $"Environment file pattern {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_SecretFile_Patterns()
    {
        // Arrange - Secret file extensions
        var expectedPatterns = new[]
        {
            "**/*.pem",
            "**/*.key",
            "**/*.p12",
            "**/*.pfx",
            "**/*.jks"
        };

        // Act
        var secretFileEntries = _entries
            .Where(e => e.Category == PathCategory.SecretFiles)
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            secretFileEntries.Should().Contain(
                pattern,
                because: $"Secret file pattern {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Be_Immutable()
    {
        // Arrange
        var entries = DefaultDenylist.Entries;

        // Act & Assert - Spec lines 1049-1065
        entries.Should().BeAssignableTo<IReadOnlyList<DenylistEntry>>(
            because: "denylist must be immutable");

        // Verify we cannot cast to mutable
        var asList = entries as IList<DenylistEntry>;
        if (asList != null)
        {
            asList.IsReadOnly.Should().BeTrue(
                because: "denylist backing list must be read-only");
        }
    }

    [Fact]
    public void Should_Have_Reason_For_Each_Entry()
    {
        // Act & Assert - Spec lines 1068-1078
        foreach (var entry in _entries)
        {
            entry.Reason.Should().NotBeNullOrWhiteSpace(
                because: $"entry {entry.Pattern} must have a reason");
            entry.Reason.Length.Should().BeGreaterThan(
                10,
                because: "reason should be descriptive");
        }
    }

    [Fact]
    public void Should_Have_RiskId_For_Each_Entry()
    {
        // Arrange - Spec lines 1081-1094
        var validRiskIdPattern = @"^RISK-[EIC]-\d{3}$";

        // Act & Assert
        foreach (var entry in _entries)
        {
            entry.RiskId.Should().NotBeNullOrWhiteSpace(
                because: $"entry {entry.Pattern} must have a risk ID");
            entry.RiskId.Should().MatchRegex(
                validRiskIdPattern,
                because: "risk ID must follow RISK-X-NNN format");
        }
    }

    [Fact]
    public void Should_Have_Valid_Category_For_Each_Entry()
    {
        // Act & Assert - Spec lines 1097-1105
        foreach (var entry in _entries)
        {
            entry.Category.Should().BeDefined(
                because: $"entry {entry.Pattern} must have a valid category");
        }
    }

    [Fact]
    public void Should_Have_At_Least_One_Platform_For_Each_Entry()
    {
        // Act & Assert - Spec lines 1108-1116
        foreach (var entry in _entries)
        {
            entry.Platforms.Should().NotBeEmpty(
                because: $"entry {entry.Pattern} must specify at least one platform");
        }
    }

    [Fact]
    public void Should_Contain_Minimum_Required_Entries()
    {
        // Spec lines 1119-1124 - requires >= 100 entries
        // AC-119 from spec line 809
        _entries.Count.Should().BeGreaterOrEqualTo(
            100,
            because: "denylist should cover all security-sensitive paths (spec requirement)");
    }
}
