namespace Acode.Domain.Security.PathProtection;

/// <summary>
/// Immutable default denylist of protected paths.
/// SECURITY CRITICAL: Changes require security review.
/// Cannot be modified at runtime or by user configuration.
/// </summary>
public static class DefaultDenylist
{
    /// <summary>
    /// Gets all default protected path entries.
    /// This collection is immutable and cannot be reduced.
    /// </summary>
    public static IReadOnlyList<DenylistEntry> Entries { get; } = CreateEntries();

    private static IReadOnlyList<DenylistEntry> CreateEntries()
    {
        var entries = new List<DenylistEntry>();

        // SSH Keys (FR-003b-26 to FR-003b-30, FR-003b-40)
        // AC-016 to AC-026
        entries.Add(new DenylistEntry
        {
            Pattern = "~/.ssh/",
            Reason = "SSH directory containing private keys",
            RiskId = "RISK-I-003",
            Category = PathCategory.SshKeys,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.ssh/id_*",
            Reason = "SSH private key files (wildcard)",
            RiskId = "RISK-I-003",
            Category = PathCategory.SshKeys,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.ssh/id_rsa",
            Reason = "SSH RSA private key",
            RiskId = "RISK-I-003",
            Category = PathCategory.SshKeys,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.ssh/id_ed25519",
            Reason = "SSH Ed25519 private key",
            RiskId = "RISK-I-003",
            Category = PathCategory.SshKeys,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.ssh/id_ecdsa",
            Reason = "SSH ECDSA private key",
            RiskId = "RISK-I-003",
            Category = PathCategory.SshKeys,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.ssh/id_dsa",
            Reason = "SSH DSA private key (legacy)",
            RiskId = "RISK-I-003",
            Category = PathCategory.SshKeys,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.ssh/known_hosts",
            Reason = "SSH known hosts file",
            RiskId = "RISK-I-003",
            Category = PathCategory.SshKeys,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.ssh/authorized_keys",
            Reason = "SSH authorized keys file",
            RiskId = "RISK-I-003",
            Category = PathCategory.SshKeys,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.ssh/config",
            Reason = "SSH configuration file",
            RiskId = "RISK-I-003",
            Category = PathCategory.SshKeys,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = @"%USERPROFILE%\.ssh\",
            Reason = "SSH directory on Windows",
            RiskId = "RISK-I-003",
            Category = PathCategory.SshKeys,
            Platforms = new[] { Platform.Windows }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = @"%USERPROFILE%\.ssh\id_*",
            Reason = "SSH private keys on Windows",
            RiskId = "RISK-I-003",
            Category = PathCategory.SshKeys,
            Platforms = new[] { Platform.Windows }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = @"C:\Users\*\.ssh\",
            Reason = "SSH directory for all Windows users",
            RiskId = "RISK-I-003",
            Category = PathCategory.SshKeys,
            Platforms = new[] { Platform.Windows }
        });

        // GPG Keys (FR-003b-31 to FR-003b-32)
        // AC-023 to AC-024
        entries.Add(new DenylistEntry
        {
            Pattern = "~/.gnupg/",
            Reason = "GPG keyring directory",
            RiskId = "RISK-I-003",
            Category = PathCategory.GpgKeys,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.gnupg/private-keys-v1.d/",
            Reason = "GPG private keys directory",
            RiskId = "RISK-I-003",
            Category = PathCategory.GpgKeys,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.gnupg/secring.gpg",
            Reason = "GPG secret keyring file (legacy)",
            RiskId = "RISK-I-003",
            Category = PathCategory.GpgKeys,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.gpg/",
            Reason = "Alternate GPG directory",
            RiskId = "RISK-I-003",
            Category = PathCategory.GpgKeys,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = @"%APPDATA%\gnupg\",
            Reason = "GPG directory on Windows",
            RiskId = "RISK-I-003",
            Category = PathCategory.GpgKeys,
            Platforms = new[] { Platform.Windows }
        });

        // Cloud Credentials (FR-003b-33 to FR-003b-39)
        // AC-027 to AC-036
        entries.Add(new DenylistEntry
        {
            Pattern = "~/.aws/",
            Reason = "AWS credentials and configuration",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.aws/credentials",
            Reason = "AWS credentials file",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.aws/config",
            Reason = "AWS configuration file",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = @"%USERPROFILE%\.aws\",
            Reason = "AWS directory on Windows",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.Windows }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = @"%USERPROFILE%\.aws\credentials",
            Reason = "AWS credentials on Windows",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.Windows }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.azure/",
            Reason = "Azure CLI credentials",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.azure/credentials",
            Reason = "Azure credentials file",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.azure/accessTokens.json",
            Reason = "Azure access tokens",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = @"%USERPROFILE%\.azure\",
            Reason = "Azure directory on Windows",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.Windows }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.gcloud/",
            Reason = "Google Cloud SDK credentials (legacy location)",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.config/gcloud/",
            Reason = "Google Cloud configuration",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.config/gcloud/credentials.db",
            Reason = "GCloud credentials database",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.config/gcloud/access_tokens.db",
            Reason = "GCloud access tokens",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.config/gcloud/application_default_credentials.json",
            Reason = "GCloud application default credentials",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = @"%APPDATA%\gcloud\",
            Reason = "GCloud directory on Windows",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.Windows }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.kube/",
            Reason = "Kubernetes configuration and credentials",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.kube/config",
            Reason = "Kubernetes configuration file",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = @"%USERPROFILE%\.kube\",
            Reason = "Kubernetes directory on Windows",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.Windows }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.docker/config.json",
            Reason = "Docker Hub credentials",
            RiskId = "RISK-I-003",
            Category = PathCategory.CloudCredentials,
            Platforms = new[] { Platform.All }
        });

        // Package Manager Credentials (FR-003b-41 to FR-003b-50)
        entries.Add(new DenylistEntry
        {
            Pattern = "~/.netrc",
            Reason = "Network credentials file",
            RiskId = "RISK-I-003",
            Category = PathCategory.PackageManagerCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.npmrc",
            Reason = "npm registry authentication tokens",
            RiskId = "RISK-I-003",
            Category = PathCategory.PackageManagerCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.pypirc",
            Reason = "PyPI credentials",
            RiskId = "RISK-I-003",
            Category = PathCategory.PackageManagerCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.nuget/NuGet.Config",
            Reason = "NuGet feed credentials",
            RiskId = "RISK-I-003",
            Category = PathCategory.PackageManagerCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.gem/credentials",
            Reason = "RubyGems API keys",
            RiskId = "RISK-I-003",
            Category = PathCategory.PackageManagerCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.cargo/credentials",
            Reason = "Cargo registry tokens",
            RiskId = "RISK-I-003",
            Category = PathCategory.PackageManagerCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.composer/auth.json",
            Reason = "Composer authentication",
            RiskId = "RISK-I-003",
            Category = PathCategory.PackageManagerCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.m2/settings.xml",
            Reason = "Maven repository credentials",
            RiskId = "RISK-I-003",
            Category = PathCategory.PackageManagerCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.gradle/gradle.properties",
            Reason = "Gradle credentials",
            RiskId = "RISK-I-003",
            Category = PathCategory.PackageManagerCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.config/gh/hosts.yml",
            Reason = "GitHub CLI credentials",
            RiskId = "RISK-I-003",
            Category = PathCategory.PackageManagerCredentials,
            Platforms = new[] { Platform.All }
        });

        // Git Credentials (FR-003b-51 to FR-003b-52)
        entries.Add(new DenylistEntry
        {
            Pattern = "~/.gitconfig",
            Reason = "Git configuration may contain credential helpers",
            RiskId = "RISK-I-003",
            Category = PathCategory.GitCredentials,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/.git-credentials",
            Reason = "Git stored plaintext credentials",
            RiskId = "RISK-I-003",
            Category = PathCategory.GitCredentials,
            Platforms = new[] { Platform.All }
        });

        // Environment Files (FR-003b-71 to FR-003b-85)
        // AC-070 to AC-079
        entries.Add(new DenylistEntry
        {
            Pattern = ".env",
            Reason = "Environment file may contain secrets",
            RiskId = "RISK-I-002",
            Category = PathCategory.EnvironmentFiles,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = ".env.local",
            Reason = "Local environment overrides",
            RiskId = "RISK-I-002",
            Category = PathCategory.EnvironmentFiles,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = ".env.development",
            Reason = "Development environment configuration",
            RiskId = "RISK-I-002",
            Category = PathCategory.EnvironmentFiles,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = ".env.production",
            Reason = "Production environment configuration",
            RiskId = "RISK-I-002",
            Category = PathCategory.EnvironmentFiles,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = ".env.*",
            Reason = "Environment file variants (wildcard)",
            RiskId = "RISK-I-002",
            Category = PathCategory.EnvironmentFiles,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "**/.env",
            Reason = "Nested environment files",
            RiskId = "RISK-I-002",
            Category = PathCategory.EnvironmentFiles,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "**/.env.*",
            Reason = "Nested environment file variants",
            RiskId = "RISK-I-002",
            Category = PathCategory.EnvironmentFiles,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "secrets/",
            Reason = "Secrets directory",
            RiskId = "RISK-I-002",
            Category = PathCategory.EnvironmentFiles,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "**/secrets/",
            Reason = "Nested secrets directories",
            RiskId = "RISK-I-002",
            Category = PathCategory.EnvironmentFiles,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "private/",
            Reason = "Private files directory",
            RiskId = "RISK-I-002",
            Category = PathCategory.EnvironmentFiles,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "**/private/",
            Reason = "Nested private directories",
            RiskId = "RISK-I-002",
            Category = PathCategory.EnvironmentFiles,
            Platforms = new[] { Platform.All }
        });

        // Secret Files (FR-003b-53 to FR-003b-55)
        // AC-080 to AC-084
        entries.Add(new DenylistEntry
        {
            Pattern = "**/*.pem",
            Reason = "PEM certificate files",
            RiskId = "RISK-I-003",
            Category = PathCategory.SecretFiles,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "**/*.key",
            Reason = "Private key files",
            RiskId = "RISK-I-003",
            Category = PathCategory.SecretFiles,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "**/*.p12",
            Reason = "PKCS#12 certificate files",
            RiskId = "RISK-I-003",
            Category = PathCategory.SecretFiles,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "**/*.pfx",
            Reason = "PFX certificate files (Windows)",
            RiskId = "RISK-I-003",
            Category = PathCategory.SecretFiles,
            Platforms = new[] { Platform.All }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "**/*.jks",
            Reason = "Java KeyStore files",
            RiskId = "RISK-I-003",
            Category = PathCategory.SecretFiles,
            Platforms = new[] { Platform.All }
        });

        // System Paths Unix (FR-003b-56 to FR-003b-62)
        // AC-051 to AC-058
        entries.Add(new DenylistEntry
        {
            Pattern = "/etc/",
            Reason = "Unix system configuration directory",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "/etc/passwd",
            Reason = "Unix user accounts file",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "/etc/shadow",
            Reason = "Unix password hashes",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.Linux }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "/etc/sudoers",
            Reason = "Sudo privileges configuration",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "/etc/sudoers.d/",
            Reason = "Sudo drop-in configuration directory",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "/etc/ssh/",
            Reason = "System-wide SSH configuration",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "/etc/ssl/private/",
            Reason = "System SSL private keys",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "/root/",
            Reason = "Root user home directory",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.Linux }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "/var/log/",
            Reason = "System log files (may contain sensitive data)",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.Linux, Platform.MacOS }
        });

        // System Paths Windows (FR-003b-63 to FR-003b-67)
        // AC-059 to AC-064
        entries.Add(new DenylistEntry
        {
            Pattern = @"C:\Windows\",
            Reason = "Windows system directory",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.Windows }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = @"C:\Windows\System32\",
            Reason = "Windows core binaries (64-bit)",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.Windows }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = @"C:\Windows\SysWOW64\",
            Reason = "Windows core binaries (32-bit on 64-bit)",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.Windows }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = @"C:\ProgramData\",
            Reason = "Windows program data",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.Windows }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = @"C:\Users\*\AppData\",
            Reason = "User application data (may contain credentials)",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.Windows }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = @"HKEY_*",
            Reason = "Windows Registry paths (not filesystem but blocked in path validation)",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.Windows }
        });

        // System Paths macOS (FR-003b-68 to FR-003b-70)
        // AC-065 to AC-069
        entries.Add(new DenylistEntry
        {
            Pattern = "/System/",
            Reason = "macOS system files",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "/Library/",
            Reason = "macOS system libraries",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/Library/",
            Reason = "macOS user libraries and preferences",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "~/Library/Keychains/",
            Reason = "macOS Keychain credentials",
            RiskId = "RISK-I-003",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.MacOS }
        });

        entries.Add(new DenylistEntry
        {
            Pattern = "/private/var/",
            Reason = "macOS system variable data",
            RiskId = "RISK-E-004",
            Category = PathCategory.SystemFiles,
            Platforms = new[] { Platform.MacOS }
        });

        return entries.AsReadOnly();
    }
}
