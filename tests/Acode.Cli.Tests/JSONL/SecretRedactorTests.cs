// <copyright file="SecretRedactorTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.Tests.JSONL;

using Acode.Cli.JSONL;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="SecretRedactor"/>.
/// </summary>
public class SecretRedactorTests
{
    private readonly SecretRedactor _sut = new();

    /// <summary>
    /// Verifies Redact shows last 4 characters.
    /// </summary>
    [Fact]
    public void Redact_LongValue_ShouldShowLastFourChars()
    {
        var result = _sut.Redact("sk-abc123xyz789", "api_key");

        result.Should().Be("***z789");
    }

    /// <summary>
    /// Verifies short values are fully masked.
    /// </summary>
    [Fact]
    public void Redact_ShortValue_ShouldBeFullyMasked()
    {
        var result = _sut.Redact("abc", "password");

        result.Should().Be("***");
    }

    /// <summary>
    /// Verifies null value returns masked.
    /// </summary>
    [Fact]
    public void Redact_NullValue_ShouldReturnMask()
    {
        var result = _sut.Redact(null!, "type");

        result.Should().Be("***");
    }

    /// <summary>
    /// Verifies empty value returns masked.
    /// </summary>
    [Fact]
    public void Redact_EmptyValue_ShouldReturnMask()
    {
        var result = _sut.Redact(string.Empty, "type");

        result.Should().Be("***");
    }

    /// <summary>
    /// Verifies IsSecret detects api_key.
    /// </summary>
    [Fact]
    public void IsSecret_ApiKey_ShouldReturnTrue()
    {
        _sut.IsSecret("api_key").Should().BeTrue();
        _sut.IsSecret("apikey").Should().BeTrue();
        _sut.IsSecret("API_KEY").Should().BeTrue();
    }

    /// <summary>
    /// Verifies IsSecret detects password.
    /// </summary>
    [Fact]
    public void IsSecret_Password_ShouldReturnTrue()
    {
        _sut.IsSecret("password").Should().BeTrue();
        _sut.IsSecret("passwd").Should().BeTrue();
        _sut.IsSecret("pwd").Should().BeTrue();
    }

    /// <summary>
    /// Verifies IsSecret detects token.
    /// </summary>
    [Fact]
    public void IsSecret_Token_ShouldReturnTrue()
    {
        _sut.IsSecret("token").Should().BeTrue();
        _sut.IsSecret("auth_token").Should().BeTrue();
        _sut.IsSecret("bearer").Should().BeTrue();
    }

    /// <summary>
    /// Verifies IsSecret detects secrets.
    /// </summary>
    [Fact]
    public void IsSecret_Secret_ShouldReturnTrue()
    {
        _sut.IsSecret("secret").Should().BeTrue();
        _sut.IsSecret("client_secret").Should().BeTrue();
    }

    /// <summary>
    /// Verifies IsSecret detects credential.
    /// </summary>
    [Fact]
    public void IsSecret_Credential_ShouldReturnTrue()
    {
        _sut.IsSecret("credential").Should().BeTrue();
        _sut.IsSecret("credentials").Should().BeTrue();
    }

    /// <summary>
    /// Verifies IsSecret returns false for non-secrets.
    /// </summary>
    [Fact]
    public void IsSecret_NonSecret_ShouldReturnFalse()
    {
        _sut.IsSecret("name").Should().BeFalse();
        _sut.IsSecret("path").Should().BeFalse();
        _sut.IsSecret("message").Should().BeFalse();
        _sut.IsSecret("file").Should().BeFalse();
    }

    /// <summary>
    /// Verifies IsSecret handles null.
    /// </summary>
    [Fact]
    public void IsSecret_Null_ShouldReturnFalse()
    {
        _sut.IsSecret(null!).Should().BeFalse();
    }

    /// <summary>
    /// Verifies IsSecret handles empty.
    /// </summary>
    [Fact]
    public void IsSecret_Empty_ShouldReturnFalse()
    {
        _sut.IsSecret(string.Empty).Should().BeFalse();
    }

    /// <summary>
    /// Verifies RedactDictionary redacts secrets.
    /// </summary>
    [Fact]
    public void RedactDictionary_WithSecrets_ShouldRedactSecretValues()
    {
        var data = new Dictionary<string, object>
        {
            { "api_key", "sk-abcd1234efgh5678" },
            { "name", "test" },
            { "password", "mysecretpassword" },
        };

        var result = _sut.RedactDictionary(data);

        result["api_key"].Should().Be("***5678");
        result["name"].Should().Be("test");
        result["password"].Should().Be("***word");
    }

    /// <summary>
    /// Verifies RedactDictionary preserves non-secrets.
    /// </summary>
    [Fact]
    public void RedactDictionary_WithoutSecrets_ShouldPreserveValues()
    {
        var data = new Dictionary<string, object> { { "name", "test" }, { "path", "/home/user" } };

        var result = _sut.RedactDictionary(data);

        result["name"].Should().Be("test");
        result["path"].Should().Be("/home/user");
    }

    /// <summary>
    /// Verifies RedactDictionary throws on null.
    /// </summary>
    [Fact]
    public void RedactDictionary_Null_ShouldThrow()
    {
        var act = () => _sut.RedactDictionary(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
