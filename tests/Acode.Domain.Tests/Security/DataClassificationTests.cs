namespace Acode.Domain.Tests.Security;

using Acode.Domain.Security;
using FluentAssertions;
using Xunit;

public class DataClassificationTests
{
    [Fact]
    public void DataClassification_ShouldHavePublicValue()
    {
        // Arrange & Act
        var classification = DataClassification.Public;

        // Assert
        classification.Should().Be(DataClassification.Public);
        ((int)classification).Should().Be(0);
    }

    [Fact]
    public void DataClassification_ShouldHaveInternalValue()
    {
        // Arrange & Act
        var classification = DataClassification.Internal;

        // Assert
        classification.Should().Be(DataClassification.Internal);
        ((int)classification).Should().Be(1);
    }

    [Fact]
    public void DataClassification_ShouldHaveConfidentialValue()
    {
        // Arrange & Act
        var classification = DataClassification.Confidential;

        // Assert
        classification.Should().Be(DataClassification.Confidential);
        ((int)classification).Should().Be(2);
    }

    [Fact]
    public void DataClassification_ShouldHaveSecretValue()
    {
        // Arrange & Act
        var classification = DataClassification.Secret;

        // Assert
        classification.Should().Be(DataClassification.Secret);
        ((int)classification).Should().Be(3);
    }

    [Fact]
    public void DataClassification_ShouldHaveExactlyFourValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<DataClassification>();

        // Assert
        values.Should().HaveCount(4);
    }

    [Fact]
    public void DataClassification_ShouldBeOrderedBySensitivityAscending()
    {
        // Arrange & Act
        var publicLevel = (int)DataClassification.Public;
        var internalLevel = (int)DataClassification.Internal;
        var confidentialLevel = (int)DataClassification.Confidential;
        var secretLevel = (int)DataClassification.Secret;

        // Assert
        publicLevel.Should().BeLessThan(internalLevel);
        internalLevel.Should().BeLessThan(confidentialLevel);
        confidentialLevel.Should().BeLessThan(secretLevel);
    }

    [Fact]
    public void DataClassification_AllValuesShouldBeDistinct()
    {
        // Arrange & Act
        var values = Enum.GetValues<DataClassification>();

        // Assert
        values.Should().OnlyHaveUniqueItems();
    }
}
