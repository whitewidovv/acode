namespace Acode.Domain.Risks;

/// <summary>
/// Risk categories following the STRIDE threat modeling methodology.
/// STRIDE is an acronym for: Spoofing, Tampering, Repudiation,
/// Information Disclosure, Denial of Service, Elevation of Privilege.
/// </summary>
public enum RiskCategory
{
    /// <summary>
    /// Spoofing - Pretending to be something or someone other than yourself.
    /// Examples: Fake user identity, impersonated model output, forged credentials.
    /// Mapped to STRIDE: S.
    /// </summary>
    Spoofing,

    /// <summary>
    /// Tampering - Modifying data or code without authorization.
    /// Examples: Altered config files, modified source code, corrupted audit logs.
    /// Mapped to STRIDE: T.
    /// </summary>
    Tampering,

    /// <summary>
    /// Repudiation - Denying that an action was performed.
    /// Examples: No audit trail, missing logs, unsigned operations.
    /// Mapped to STRIDE: R.
    /// </summary>
    Repudiation,

    /// <summary>
    /// Information Disclosure - Exposing information to unauthorized parties.
    /// Examples: Leaked secrets, exposed credentials, visible sensitive data.
    /// Mapped to STRIDE: I.
    /// </summary>
    InformationDisclosure,

    /// <summary>
    /// Denial of Service - Making system or service unavailable.
    /// Examples: Resource exhaustion, infinite loops, crashed processes.
    /// Mapped to STRIDE: D.
    /// </summary>
    DenialOfService,

    /// <summary>
    /// Elevation of Privilege - Gaining capabilities without authorization.
    /// Examples: Escaped sandbox, bypassed access controls, root access gained.
    /// Mapped to STRIDE: E.
    /// </summary>
    ElevationOfPrivilege
}
