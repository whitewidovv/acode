namespace Acode.Domain.Tests.Security.PathProtection;

using Acode.Domain.Security.PathProtection;
using FluentAssertions;

public class PathCategoryTests
{
    [Fact]
    public void PathCategory_ShouldHaveAllRequiredCategories()
    {
        // Arrange & Act - FR-003b from spec defines these categories
        var categories = new[]
        {
            PathCategory.SshKeys,
            PathCategory.GpgKeys,
            PathCategory.CloudCredentials,
            PathCategory.PackageManagerCredentials,
            PathCategory.GitCredentials,
            PathCategory.SystemFiles,
            PathCategory.EnvironmentFiles,
            PathCategory.SecretFiles,
            PathCategory.UserDefined
        };

        // Assert - All categories should exist
        foreach (var category in categories)
        {
            Enum.IsDefined(typeof(PathCategory), category).Should().BeTrue();
        }
    }

    [Fact]
    public void PathCategory_ShouldHaveExactlyNineValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<PathCategory>();

        // Assert
        values.Should().HaveCount(9);
    }

    [Fact]
    public void PathCategory_AllValuesShouldBeDistinct()
    {
        // Arrange & Act
        var values = Enum.GetValues<PathCategory>();

        // Assert
        values.Should().OnlyHaveUniqueItems();
    }
}
