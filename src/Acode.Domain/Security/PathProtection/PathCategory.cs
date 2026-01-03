namespace Acode.Domain.Security.PathProtection;

/// <summary>
/// Categories of protected paths in the default denylist.
/// Used to organize and document why specific paths are protected.
/// </summary>
public enum PathCategory
{
    /// <summary>
    /// SSH private keys and related files.
    /// Examples: ~/.ssh/id_rsa, ~/.ssh/authorized_keys.
    /// </summary>
    SshKeys,

    /// <summary>
    /// GPG/PGP keyrings and private keys.
    /// Examples: ~/.gnupg/, ~/.gpg/.
    /// </summary>
    GpgKeys,

    /// <summary>
    /// Cloud provider credentials and configuration.
    /// Examples: ~/.aws/credentials, ~/.gcloud/, ~/.azure/.
    /// </summary>
    CloudCredentials,

    /// <summary>
    /// Package manager authentication tokens.
    /// Examples: ~/.npmrc, ~/.pypirc, ~/.nuget/NuGet.Config.
    /// </summary>
    PackageManagerCredentials,

    /// <summary>
    /// Git credentials and configuration.
    /// Examples: ~/.gitconfig, ~/.git-credentials, ~/.netrc.
    /// </summary>
    GitCredentials,

    /// <summary>
    /// Operating system files and directories.
    /// Examples: /etc/, C:\Windows\, /System/.
    /// </summary>
    SystemFiles,

    /// <summary>
    /// Environment files containing variables and secrets.
    /// Examples: .env, .env.local, .env.production.
    /// </summary>
    EnvironmentFiles,

    /// <summary>
    /// Secret files identified by pattern or name.
    /// Examples: *.pem, *.key, secrets/, private/.
    /// </summary>
    SecretFiles,

    /// <summary>
    /// User-defined protected paths from configuration.
    /// Paths added via .agent/config.yml security.additional_protected_paths.
    /// </summary>
    UserDefined
}
