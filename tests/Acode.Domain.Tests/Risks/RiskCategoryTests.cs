namespace Acode.Domain.Tests.Risks;

using Acode.Domain.Risks;
using FluentAssertions;

public class RiskCategoryTests
{
    [Fact]
    public void RiskCategory_ShouldHaveSpoofingValue()
    {
        // Arrange & Act
        var category = RiskCategory.Spoofing;

        // Assert
        category.Should().Be(RiskCategory.Spoofing);
    }

    [Fact]
    public void RiskCategory_ShouldHaveTamperingValue()
    {
        // Arrange & Act
        var category = RiskCategory.Tampering;

        // Assert
        category.Should().Be(RiskCategory.Tampering);
    }

    [Fact]
    public void RiskCategory_ShouldHaveRepudiationValue()
    {
        // Arrange & Act
        var category = RiskCategory.Repudiation;

        // Assert
        category.Should().Be(RiskCategory.Repudiation);
    }

    [Fact]
    public void RiskCategory_ShouldHaveInformationDisclosureValue()
    {
        // Arrange & Act
        var category = RiskCategory.InformationDisclosure;

        // Assert
        category.Should().Be(RiskCategory.InformationDisclosure);
    }

    [Fact]
    public void RiskCategory_ShouldHaveDenialOfServiceValue()
    {
        // Arrange & Act
        var category = RiskCategory.DenialOfService;

        // Assert
        category.Should().Be(RiskCategory.DenialOfService);
    }

    [Fact]
    public void RiskCategory_ShouldHaveElevationOfPrivilegeValue()
    {
        // Arrange & Act
        var category = RiskCategory.ElevationOfPrivilege;

        // Assert
        category.Should().Be(RiskCategory.ElevationOfPrivilege);
    }

    [Fact]
    public void RiskCategory_ShouldHaveExactlySixValues_ForStride()
    {
        // Arrange & Act
        var values = Enum.GetValues<RiskCategory>();

        // Assert - STRIDE has exactly 6 categories
        values.Should().HaveCount(6);
    }

    [Fact]
    public void RiskCategory_AllValuesShouldBeDistinct()
    {
        // Arrange & Act
        var values = Enum.GetValues<RiskCategory>();

        // Assert
        values.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void RiskCategory_ShouldFollowStrideAcronym()
    {
        // Arrange - STRIDE acronym
        var expectedCategories = new[]
        {
            RiskCategory.Spoofing, // S
            RiskCategory.Tampering, // T
            RiskCategory.Repudiation, // R
            RiskCategory.InformationDisclosure, // I
            RiskCategory.DenialOfService, // D
            RiskCategory.ElevationOfPrivilege // E
        };

        // Act
        var actualCategories = Enum.GetValues<RiskCategory>();

        // Assert - All STRIDE categories exist
        foreach (var expected in expectedCategories)
        {
            actualCategories.Should().Contain(expected);
        }
    }
}
