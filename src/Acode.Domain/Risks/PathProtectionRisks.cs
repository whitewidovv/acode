namespace Acode.Domain.Risks;

/// <summary>
/// Defines risk identifiers for path protection violations.
/// These risks are referenced in the DefaultDenylist and error reporting.
/// </summary>
public static class PathProtectionRisks
{
    /// <summary>
    /// Risk: SSH private key exposure (Information Disclosure).
    /// Severity: High - Compromises authentication security.
    /// </summary>
    public const string SshKeyExposure = "RISK-I-003";

    /// <summary>
    /// Risk: Cloud provider credential exposure (Information Disclosure).
    /// Severity: High - Compromises cloud infrastructure security.
    /// </summary>
    public const string CloudCredentialExposure = "RISK-I-003";

    /// <summary>
    /// Risk: GPG/PGP key exposure (Information Disclosure).
    /// Severity: High - Compromises encryption and signing security.
    /// </summary>
    public const string GpgKeyExposure = "RISK-I-003";

    /// <summary>
    /// Risk: System file modification (Elevation/Tampering).
    /// Severity: Critical - Can compromise system integrity.
    /// </summary>
    public const string SystemFileModification = "RISK-E-004";

    /// <summary>
    /// Risk: Environment file exposure (Information Disclosure).
    /// Severity: High - Exposes secrets and configuration.
    /// </summary>
    public const string EnvironmentFileExposure = "RISK-I-002";

    /// <summary>
    /// Risk: Symlink attack bypassing protection (Elevation).
    /// Severity: High - Bypasses security controls.
    /// </summary>
    public const string SymlinkAttack = "RISK-E-005";

    /// <summary>
    /// Risk: Directory traversal bypassing protection (Elevation).
    /// Severity: High - Bypasses security controls.
    /// </summary>
    public const string DirectoryTraversal = "RISK-E-006";

    /// <summary>
    /// Risk: Package manager credential exposure (Information Disclosure).
    /// Severity: Medium - Compromises package repositories.
    /// </summary>
    public const string PackageCredentialExposure = "RISK-I-003";

    /// <summary>
    /// Risk: Git credential exposure (Information Disclosure).
    /// Severity: High - Compromises source code repositories.
    /// </summary>
    public const string GitCredentialExposure = "RISK-I-003";

    /// <summary>
    /// Risk: Secret file exposure (Information Disclosure).
    /// Severity: High - Exposes certificates, keys, and other secrets.
    /// </summary>
    public const string SecretFileExposure = "RISK-I-003";

    /// <summary>
    /// Risk: User-defined protected path access (User-Specified).
    /// Severity: Varies - Depends on user configuration.
    /// </summary>
    public const string UserDefinedPathAccess = "RISK-U-001";
}
