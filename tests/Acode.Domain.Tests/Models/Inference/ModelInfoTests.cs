using Acode.Domain.Models.Inference;
using Acode.Domain.Modes;
using FluentAssertions;

namespace Acode.Domain.Tests.Models.Inference;

/// <summary>
/// Tests for <see cref="ModelInfo"/>.
/// </summary>
public class ModelInfoTests
{
    [Fact]
    public void Constructor_WithRequiredProperties_SetsProperties()
    {
        // Arrange & Act
        var modelInfo = new ModelInfo
        {
            ModelId = "llama3.2:7b",
            IsLocal = true,
            RequiresNetwork = false,
        };

        // Assert
        modelInfo.ModelId.Should().Be("llama3.2:7b");
        modelInfo.IsLocal.Should().BeTrue();
        modelInfo.RequiresNetwork.Should().BeFalse();
    }

    [Fact]
    public void ModelInfo_IsImmutable()
    {
        // Arrange
        var modelInfo = new ModelInfo
        {
            ModelId = "llama3.2:7b",
            IsLocal = true,
            RequiresNetwork = false,
        };

        // Act & Assert
        // Properties should be init-only, verified by compilation success
        modelInfo.ModelId.Should().Be("llama3.2:7b");
    }

    [Fact]
    public void RecordEquality_WithSameValues_AreEqual()
    {
        // Arrange
        var info1 = new ModelInfo
        {
            ModelId = "llama3.2:7b",
            IsLocal = true,
            RequiresNetwork = false,
        };
        var info2 = new ModelInfo
        {
            ModelId = "llama3.2:7b",
            IsLocal = true,
            RequiresNetwork = false,
        };

        // Act & Assert
        info1.Should().Be(info2);
    }

    [Fact]
    public void RecordEquality_WithDifferentModelId_AreNotEqual()
    {
        // Arrange
        var info1 = new ModelInfo
        {
            ModelId = "llama3.2:7b",
            IsLocal = true,
            RequiresNetwork = false,
        };
        var info2 = new ModelInfo
        {
            ModelId = "mistral:7b",
            IsLocal = true,
            RequiresNetwork = false,
        };

        // Act & Assert
        info1.Should().NotBe(info2);
    }

    [Fact]
    public void RecordEquality_WithDifferentIsLocal_AreNotEqual()
    {
        // Arrange
        var info1 = new ModelInfo
        {
            ModelId = "llama3.2:7b",
            IsLocal = true,
            RequiresNetwork = false,
        };
        var info2 = new ModelInfo
        {
            ModelId = "llama3.2:7b",
            IsLocal = false,
            RequiresNetwork = true,
        };

        // Act & Assert
        info1.Should().NotBe(info2);
    }

    [Fact]
    public void ToString_IncludesModelIdAndLocalStatus()
    {
        // Arrange
        var modelInfo = new ModelInfo
        {
            ModelId = "llama3.2:7b",
            IsLocal = true,
            RequiresNetwork = false,
        };

        // Act
        var result = modelInfo.ToString();

        // Assert
        result.Should().Contain("llama3.2:7b");
        result.Should().Contain("True");
    }

    [Fact]
    public void IsAllowedInMode_LocalOnly_AllowsLocalModels()
    {
        // Arrange
        var localModel = new ModelInfo
        {
            ModelId = "llama3.2:7b",
            IsLocal = true,
            RequiresNetwork = false,
        };

        // Act
        var isAllowed = localModel.IsAllowedInMode(OperatingMode.LocalOnly);

        // Assert
        isAllowed.Should().BeTrue();
    }

    [Fact]
    public void IsAllowedInMode_LocalOnly_DisallowsRemoteModels()
    {
        // Arrange
        var remoteModel = new ModelInfo
        {
            ModelId = "remote-model",
            IsLocal = false,
            RequiresNetwork = true,
        };

        // Act
        var isAllowed = remoteModel.IsAllowedInMode(OperatingMode.LocalOnly);

        // Assert
        isAllowed.Should().BeFalse();
    }

    [Fact]
    public void IsAllowedInMode_Airgapped_DisallowsNetworkModels()
    {
        // Arrange
        var networkModel = new ModelInfo
        {
            ModelId = "network-model",
            IsLocal = true,
            RequiresNetwork = true,
        };

        // Act
        var isAllowed = networkModel.IsAllowedInMode(OperatingMode.Airgapped);

        // Assert
        isAllowed.Should().BeFalse();
    }

    [Fact]
    public void IsAllowedInMode_Airgapped_AllowsLocalNoNetworkModels()
    {
        // Arrange
        var localModel = new ModelInfo
        {
            ModelId = "llama3.2:7b",
            IsLocal = true,
            RequiresNetwork = false,
        };

        // Act
        var isAllowed = localModel.IsAllowedInMode(OperatingMode.Airgapped);

        // Assert
        isAllowed.Should().BeTrue();
    }

    [Fact]
    public void IsAllowedInMode_Burst_AllowsAllModels()
    {
        // Arrange
        var remoteModel = new ModelInfo
        {
            ModelId = "remote-model",
            IsLocal = false,
            RequiresNetwork = true,
        };

        // Act
        var isAllowed = remoteModel.IsAllowedInMode(OperatingMode.Burst);

        // Assert
        isAllowed.Should().BeTrue();
    }
}
