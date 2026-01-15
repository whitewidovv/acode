using System.Net;
using Acode.Infrastructure.Vllm.Health.Errors;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm.Health.Errors;

public class VllmErrorClassifierTests
{
    [Fact]
    public void Should_Classify_400_As_Permanent()
    {
        // Arrange
        var classifier = new VllmErrorClassifier();

        // Act
        var isTransient = classifier.IsTransient(HttpStatusCode.BadRequest);

        // Assert
        isTransient.Should().BeFalse("400 errors are permanent");
    }

    [Fact]
    public void Should_Classify_401_As_Permanent()
    {
        // Arrange
        var classifier = new VllmErrorClassifier();

        // Act
        var isTransient = classifier.IsTransient(HttpStatusCode.Unauthorized);

        // Assert
        isTransient.Should().BeFalse("401 errors are permanent");
    }

    [Fact]
    public void Should_Classify_403_As_Permanent()
    {
        // Arrange
        var classifier = new VllmErrorClassifier();

        // Act
        var isTransient = classifier.IsTransient(HttpStatusCode.Forbidden);

        // Assert
        isTransient.Should().BeFalse("403 errors are permanent");
    }

    [Fact]
    public void Should_Classify_404_As_Permanent()
    {
        // Arrange
        var classifier = new VllmErrorClassifier();

        // Act
        var isTransient = classifier.IsTransient(HttpStatusCode.NotFound);

        // Assert
        isTransient.Should().BeFalse("404 errors are permanent");
    }

    [Fact]
    public void Should_Classify_429_As_Transient()
    {
        // Arrange
        var classifier = new VllmErrorClassifier();

        // Act
        var isTransient = classifier.IsTransient(HttpStatusCode.TooManyRequests);

        // Assert
        isTransient.Should().BeTrue("429 errors are transient");
    }

    [Fact]
    public void Should_Classify_500_As_Transient()
    {
        // Arrange
        var classifier = new VllmErrorClassifier();

        // Act
        var isTransient = classifier.IsTransient(HttpStatusCode.InternalServerError);

        // Assert
        isTransient.Should().BeTrue("500 errors are transient");
    }

    [Fact]
    public void Should_Classify_502_As_Transient()
    {
        // Arrange
        var classifier = new VllmErrorClassifier();

        // Act
        var isTransient = classifier.IsTransient(HttpStatusCode.BadGateway);

        // Assert
        isTransient.Should().BeTrue("502 errors are transient");
    }

    [Fact]
    public void Should_Classify_503_As_Transient()
    {
        // Arrange
        var classifier = new VllmErrorClassifier();

        // Act
        var isTransient = classifier.IsTransient(HttpStatusCode.ServiceUnavailable);

        // Assert
        isTransient.Should().BeTrue("503 errors are transient");
    }

    [Fact]
    public void Should_Classify_504_As_Transient()
    {
        // Arrange
        var classifier = new VllmErrorClassifier();

        // Act
        var isTransient = classifier.IsTransient(HttpStatusCode.GatewayTimeout);

        // Assert
        isTransient.Should().BeTrue("504 errors are transient");
    }

    [Fact]
    public void Should_Classify_Other_Errors_As_Permanent()
    {
        // Arrange
        var classifier = new VllmErrorClassifier();

        // Act
        var isTransient = classifier.IsTransient(HttpStatusCode.MethodNotAllowed); // 405

        // Assert
        isTransient.Should().BeFalse("405 errors are permanent");
    }

    [Fact]
    public void Should_Classify_Success_As_Permanent()
    {
        // Arrange
        var classifier = new VllmErrorClassifier();

        // Act
        var isTransient = classifier.IsTransient(HttpStatusCode.OK);

        // Assert
        isTransient.Should().BeFalse("200 success should not be considered transient");
    }
}
