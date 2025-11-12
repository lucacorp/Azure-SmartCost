using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzureSmartCost.Shared.Services.Implementation;
using AzureSmartCost.Shared.Interfaces;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Tests.Services;

public class StripeServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ITenantService> _mockTenantService;
    private readonly Mock<ILogger<StripeService>> _mockLogger;
    private readonly StripeService _sut;

    public StripeServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockTenantService = new Mock<ITenantService>();
        _mockLogger = new Mock<ILogger<StripeService>>();
        
        // Setup Stripe configuration
        _mockConfiguration.Setup(x => x["Stripe:SecretKey"]).Returns("sk_test_fake_key");
        _mockConfiguration.Setup(x => x["Stripe:WebhookSecret"]).Returns("whsec_test_fake_secret");
        _mockConfiguration.Setup(x => x["Stripe:ProPriceId"]).Returns("price_pro_monthly");
        _mockConfiguration.Setup(x => x["Stripe:EnterprisePriceId"]).Returns("price_enterprise_monthly");
        
        // Initialize service under test
        _sut = new StripeService(_mockConfiguration.Object, _mockTenantService.Object, _mockLogger.Object);
    }

    [Fact]
    public void StripeService_ShouldInitializeSuccessfully()
    {
        // Assert
        _sut.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAvailablePlans_ShouldReturn3Plans()
    {
        // Act
        var plans = await _sut.GetAvailablePlansAsync();

        // Assert
        plans.Should().HaveCount(3);
        plans.Should().Contain(p => p.Tier == "Free");
        plans.Should().Contain(p => p.Tier == "Pro");
        plans.Should().Contain(p => p.Tier == "Enterprise");
    }

    [Fact]
    public async Task GetPlanByTier_Free_ShouldHaveZeroCost()
    {
        // Act
        var freePlan = await _sut.GetPlanByTierAsync("Free");

        // Assert
        freePlan.Should().NotBeNull();
        freePlan!.Tier.Should().Be("Free");
        freePlan.MonthlyPrice.Should().Be(0);
        freePlan.AnnualPrice.Should().Be(0);
        freePlan.MaxUsers.Should().Be(5);
    }

    [Fact]
    public async Task GetPlanByTier_Pro_ShouldHaveCorrectPrice()
    {
        // Act
        var proPlan = await _sut.GetPlanByTierAsync("Pro");

        // Assert
        proPlan.Should().NotBeNull();
        proPlan!.Tier.Should().Be("Pro");
        proPlan.MonthlyPrice.Should().Be(99);
        proPlan.AnnualPrice.Should().Be(990);
        proPlan.Features.Should().HaveCountGreaterThan(0);
        proPlan.HasAdvancedAnalytics.Should().BeTrue();
        proPlan.HasPowerBIIntegration.Should().BeTrue();
        proPlan.MaxUsers.Should().Be(50);
    }

    [Fact]
    public async Task GetPlanByTier_Enterprise_ShouldHaveUnlimitedLimits()
    {
        // Act
        var enterprisePlan = await _sut.GetPlanByTierAsync("Enterprise");

        // Assert
        enterprisePlan.Should().NotBeNull();
        enterprisePlan!.Tier.Should().Be("Enterprise");
        enterprisePlan.MonthlyPrice.Should().Be(499);
        enterprisePlan.MaxUsers.Should().Be(int.MaxValue);
        enterprisePlan.MaxAzureSubscriptions.Should().Be(int.MaxValue);
        enterprisePlan.HasMLPredictions.Should().BeTrue();
        enterprisePlan.HasCustomBranding.Should().BeTrue();
        enterprisePlan.HasSSOSupport.Should().BeTrue();
    }

    [Fact]
    public void SubscriptionPlan_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var plan = new SubscriptionPlan();

        // Assert
        plan.Currency.Should().Be("usd");
        plan.Tier.Should().Be("Free");
        plan.Features.Should().NotBeNull();
        plan.Features.Should().BeEmpty();
    }
}
