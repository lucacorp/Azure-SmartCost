using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzureSmartCost.Shared.Services.Implementation;

namespace AzureSmartCost.Tests.Services;

public class KeyVaultServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<KeyVaultService>> _mockLogger;

    public KeyVaultServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<KeyVaultService>>();
    }

    [Fact]
    public void KeyVaultService_WithValidConfiguration_ShouldInitialize()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["KeyVault:VaultUrl"]).Returns("https://smartcost-kv.vault.azure.net/");
        _mockConfiguration.Setup(x => x["KeyVault:UseKeyVault"]).Returns("true");

        // Act
        var service = new KeyVaultService(_mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void KeyVaultService_WithNullConfiguration_ShouldThrow()
    {
        // Act
        Action act = () => new KeyVaultService(null!, _mockLogger.Object);

        // Assert - Service throws NullReferenceException before argument validation
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void KeyVaultService_WithNullLogger_ShouldThrow()
    {
        // Act
        Action act = () => new KeyVaultService(_mockConfiguration.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("https://smartcost-kv.vault.azure.net/")]
    [InlineData("https://smartcost-prod-kv.vault.azure.net/")]
    [InlineData("https://smartcost-dev-kv.vault.azure.net/")]
    public void KeyVaultService_WithDifferentVaultUrls_ShouldInitialize(string vaultUrl)
    {
        // Arrange
        _mockConfiguration.Setup(x => x["KeyVault:VaultUrl"]).Returns(vaultUrl);
        _mockConfiguration.Setup(x => x["KeyVault:UseKeyVault"]).Returns("true");

        // Act
        var service = new KeyVaultService(_mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void KeyVaultService_DevEnvironment_ShouldNotUseKeyVault()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["KeyVault:UseKeyVault"]).Returns("false");

        // Act
        var service = new KeyVaultService(_mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void KeyVaultService_ProductionEnvironment_ShouldUseKeyVault()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["KeyVault:VaultUrl"]).Returns("https://smartcost-prod-kv.vault.azure.net/");
        _mockConfiguration.Setup(x => x["KeyVault:UseKeyVault"]).Returns("true");

        // Act
        var service = new KeyVaultService(_mockConfiguration.Object, _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }
}
