#pragma warning disable IDE0005
using Acode.Infrastructure.Providers.Vllm.Lifecycle;
using FluentAssertions;
#pragma warning restore IDE0005

namespace Acode.Infrastructure.Tests.Providers.Vllm.Lifecycle;

/// <summary>
/// Tests for VllmModelLoader Huggingface model validation and loading.
/// </summary>
public class VllmModelLoaderTests
{
    [Theory]
    [InlineData("meta-llama/Llama-2-7b-hf")]
    [InlineData("microsoft/phi-2")]
    [InlineData("google/gemma-2b")]
    [InlineData("mistralai/Mistral-7B-v0.1")]
    public void Test_ValidateModelId_ValidFormat_ReturnsTrue(string modelId)
    {
        // Arrange
        var loader = new VllmModelLoader();

        // Act
        var isValid = loader.IsValidModelIdFormat(modelId);

        // Assert
        isValid.Should().BeTrue($"'{modelId}' should be a valid Huggingface model ID format");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("no-slash")]
    [InlineData("/missing-org")]
    [InlineData("missing-model/")]
    [InlineData("too/many/slashes")]
    [InlineData("org//double-slash")]
    public void Test_ValidateModelId_InvalidFormat_ReturnsFalse(string modelId)
    {
        // Arrange
        var loader = new VllmModelLoader();

        // Act
        var isValid = loader.IsValidModelIdFormat(modelId);

        // Assert
        isValid.Should().BeFalse($"'{modelId}' should be invalid Huggingface model ID format");
    }

    [Fact]
    public void Test_ValidateModelIdAsync_EmptyModelId_ThrowsArgumentException()
    {
        // Arrange
        var loader = new VllmModelLoader();

        // Act
        var act = async () => await loader.ValidateModelIdAsync(string.Empty);

        // Assert
        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Model ID*cannot be empty*");
    }

    [Fact]
    public void Test_ValidateModelIdAsync_InvalidFormat_ThrowsArgumentException()
    {
        // Arrange
        var loader = new VllmModelLoader();

        // Act
        var act = async () => await loader.ValidateModelIdAsync("invalid-format");

        // Assert
        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*org/model-name*");
    }

    [Fact]
    public async Task Test_ValidateModelIdAsync_ValidFormat_NoException()
    {
        // Arrange
        var loader = new VllmModelLoader();

        // Act
        var exception = await Record.ExceptionAsync(
            () => loader.ValidateModelIdAsync("meta-llama/Llama-2-7b-hf"));

        // Assert
        exception.Should().BeNull("Valid model ID format should not throw");
    }

    [Fact]
    public void Test_IsAirgappedMode_Default_ReturnsFalse()
    {
        // Arrange
        var loader = new VllmModelLoader();

        // Act
        var isAirgapped = loader.IsAirgappedMode;

        // Assert
        isAirgapped.Should().BeFalse("Default should not be airgapped");
    }

    [Fact]
    public void Test_SetAirgappedMode_True_SetsCorrectly()
    {
        // Arrange
        var loader = new VllmModelLoader();

        // Act
        loader.SetAirgappedMode(true);

        // Assert
        loader.IsAirgappedMode.Should().BeTrue("Airgapped mode should be enabled");
    }

    [Fact]
    public void Test_SetAirgappedMode_False_SetsCorrectly()
    {
        // Arrange
        var loader = new VllmModelLoader();
        loader.SetAirgappedMode(true);

        // Act
        loader.SetAirgappedMode(false);

        // Assert
        loader.IsAirgappedMode.Should().BeFalse("Airgapped mode should be disabled");
    }

    [Fact]
    public async Task Test_CanLoadModelAsync_AirgappedMode_NotCached_ReturnsFalse()
    {
        // Arrange
        var loader = new VllmModelLoader();
        loader.SetAirgappedMode(true);

        // Act
        var canLoad = await loader.CanLoadModelAsync("meta-llama/Llama-2-7b-hf");

        // Assert
        canLoad.Should().BeFalse("Airgapped mode without cached model should return false");
    }

    [Fact]
    public async Task Test_CanLoadModelAsync_NotAirgapped_ValidFormat_ReturnsTrue()
    {
        // Arrange
        var loader = new VllmModelLoader();
        loader.SetAirgappedMode(false);

        // Act
        var canLoad = await loader.CanLoadModelAsync("meta-llama/Llama-2-7b-hf");

        // Assert
        canLoad.Should().BeTrue("Non-airgapped mode with valid format should return true");
    }

    [Fact]
    public async Task Test_CanLoadModelAsync_InvalidFormat_ReturnsFalse()
    {
        // Arrange
        var loader = new VllmModelLoader();

        // Act
        var canLoad = await loader.CanLoadModelAsync("invalid-format");

        // Assert
        canLoad.Should().BeFalse("Invalid format should return false");
    }

    [Fact]
    public void Test_GetModelLoadError_AirgappedNotCached_ReturnsHelpfulMessage()
    {
        // Arrange
        var loader = new VllmModelLoader();
        loader.SetAirgappedMode(true);

        // Act
        var error = loader.GetModelLoadError("meta-llama/Llama-2-7b-hf");

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("airgapped", "Error should mention airgapped mode");
        error.Should().Contain("pre-download", "Error should suggest pre-downloading");
    }

    [Fact]
    public void Test_GetModelLoadError_InvalidFormat_ReturnsFormatGuidance()
    {
        // Arrange
        var loader = new VllmModelLoader();

        // Act
        var error = loader.GetModelLoadError("invalid");

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("org/model-name", "Error should show correct format");
    }

    [Fact]
    public void Test_GetModelLoadError_ValidModel_ReturnsNull()
    {
        // Arrange
        var loader = new VllmModelLoader();
        loader.SetAirgappedMode(false);

        // Act
        var error = loader.GetModelLoadError("meta-llama/Llama-2-7b-hf");

        // Assert
        error.Should().BeNull("Valid model with no airgapped mode should have no error");
    }

    [Fact]
    public void Test_HfToken_Default_IsNull()
    {
        // Arrange
        var loader = new VllmModelLoader();

        // Act
        var token = loader.HfToken;

        // Assert
        token.Should().BeNull("Default HF token should be null");
    }

    [Fact]
    public void Test_SetHfToken_StoresToken()
    {
        // Arrange
        var loader = new VllmModelLoader();

        // Act
        loader.SetHfToken("hf_test_token_123");

        // Assert
        loader.HfToken.Should().Be("hf_test_token_123");
    }

    [Fact]
    public void Test_HasHfToken_WithToken_ReturnsTrue()
    {
        // Arrange
        var loader = new VllmModelLoader();
        loader.SetHfToken("hf_test_token");

        // Act
        var hasToken = loader.HasHfToken;

        // Assert
        hasToken.Should().BeTrue("Should return true when token is set");
    }

    [Fact]
    public void Test_HasHfToken_NoToken_ReturnsFalse()
    {
        // Arrange
        var loader = new VllmModelLoader();

        // Act
        var hasToken = loader.HasHfToken;

        // Assert
        hasToken.Should().BeFalse("Should return false when no token is set");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Test_SetHfToken_EmptyOrWhitespace_ClearsToken(string token)
    {
        // Arrange
        var loader = new VllmModelLoader();
        loader.SetHfToken("existing_token");

        // Act
        loader.SetHfToken(token);

        // Assert
        loader.HfToken.Should().BeNull("Empty/whitespace token should clear the token");
        loader.HasHfToken.Should().BeFalse();
    }

    [Fact]
    public void Test_GetAuthenticationGuidance_NoToken_ReturnsInstructions()
    {
        // Arrange
        var loader = new VllmModelLoader();

        // Act
        var guidance = loader.GetAuthenticationGuidance();

        // Assert
        guidance.Should().Contain("HF_TOKEN", "Should mention environment variable");
        guidance.Should().Contain("huggingface.co", "Should mention Huggingface URL");
    }

    [Fact]
    public void Test_ParseModelId_ValidFormat_ReturnsComponents()
    {
        // Arrange
        var loader = new VllmModelLoader();

        // Act
        var (org, model) = loader.ParseModelId("meta-llama/Llama-2-7b-hf");

        // Assert
        org.Should().Be("meta-llama");
        model.Should().Be("Llama-2-7b-hf");
    }

    [Fact]
    public void Test_ParseModelId_InvalidFormat_ReturnsNulls()
    {
        // Arrange
        var loader = new VllmModelLoader();

        // Act
        var (org, model) = loader.ParseModelId("invalid");

        // Assert
        org.Should().BeNull();
        model.Should().BeNull();
    }
}
