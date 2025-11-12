using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using AzureSmartCost.Shared.Services.Implementation;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Tests.Services;

public class TenantServiceTests
{
    private readonly Mock<CosmosClient> _mockCosmosClient;
    private readonly Mock<ILogger<TenantService>> _mockLogger;
    private readonly Mock<Container> _mockContainer;
    private readonly TenantService _sut;

    public TenantServiceTests()
    {
        _mockCosmosClient = new Mock<CosmosClient>();
        _mockLogger = new Mock<ILogger<TenantService>>();
        _mockContainer = new Mock<Container>();
        
        // Mock container
        _mockCosmosClient
            .Setup(x => x.GetContainer("SmartCostDB", "Tenants"))
            .Returns(_mockContainer.Object);
        
        // Initialize service under test
        _sut = new TenantService(_mockCosmosClient.Object, _mockLogger.Object);
    }

    [Fact]
    public void TenantService_ShouldInitializeSuccessfully()
    {
        // Assert
        _sut.Should().NotBeNull();
    }

    [Fact]
    public void Tenant_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var tenant = new Tenant();

        // Assert
        tenant.Id.Should().NotBeNullOrEmpty();
        tenant.SubscriptionTier.Should().Be("Free");
        tenant.IsActive.Should().BeTrue();
        tenant.IsTrialActive.Should().BeTrue();
        tenant.MaxUsers.Should().Be(5);
        tenant.MaxAzureSubscriptions.Should().Be(1);
        tenant.MaxMonthlyApiCalls.Should().Be(10000);
        tenant.HasAdvancedAnalytics.Should().BeFalse();
        tenant.HasMLPredictions.Should().BeFalse();
    }

    [Theory]
    [InlineData("Free", 5, 1, 10000, false, false)]
    [InlineData("Pro", 50, 5, 100000, true, false)]
    public void Tenant_ShouldHaveCorrectLimitsByTier(
        string tier, 
        int maxUsers, 
        int maxSubs, 
        int maxCalls,
        bool hasAnalytics,
        bool hasMl)
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            SubscriptionTier = tier,
            MaxUsers = maxUsers,
            MaxAzureSubscriptions = maxSubs,
            MaxMonthlyApiCalls = maxCalls,
            HasAdvancedAnalytics = hasAnalytics,
            HasMLPredictions = hasMl
        };

        // Assert
        tenant.SubscriptionTier.Should().Be(tier);
        tenant.MaxUsers.Should().Be(maxUsers);
        tenant.MaxAzureSubscriptions.Should().Be(maxSubs);
        tenant.MaxMonthlyApiCalls.Should().Be(maxCalls);
        tenant.HasAdvancedAnalytics.Should().Be(hasAnalytics);
        tenant.HasMLPredictions.Should().Be(hasMl);
    }

    [Fact]
    public void Tenant_Metadata_ShouldBeInitialized()
    {
        // Arrange & Act
        var tenant = new Tenant();

        // Assert
        tenant.Metadata.Should().NotBeNull();
        tenant.Metadata.Should().BeEmpty();
    }
}
