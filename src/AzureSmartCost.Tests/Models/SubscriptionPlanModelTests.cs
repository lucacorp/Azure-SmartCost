using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Tests.Models;

public class SubscriptionPlanModelTests
{
    [Fact]
    public void SubscriptionPlan_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var plan = new SubscriptionPlan();

        // Assert
        plan.Id.Should().Be(string.Empty);
        plan.Name.Should().Be(string.Empty);
        plan.StripePriceId.Should().Be(string.Empty);
        plan.StripeProductId.Should().Be(string.Empty);
        plan.MonthlyPrice.Should().Be(0);
        plan.AnnualPrice.Should().Be(0);
        plan.Currency.Should().Be("usd");
        plan.Tier.Should().Be("Free");
        plan.Features.Should().NotBeNull();
        plan.Features.Should().BeEmpty();
        plan.Description.Should().Be(string.Empty);
        plan.IsPopular.Should().BeFalse();
    }

    [Fact]
    public void SubscriptionPlan_CanSetAllProperties()
    {
        // Arrange & Act
        var plan = new SubscriptionPlan
        {
            Id = "plan_123",
            Name = "Professional",
            StripePriceId = "price_456",
            StripeProductId = "prod_789",
            MonthlyPrice = 99,
            AnnualPrice = 990,
            Currency = "usd",
            Tier = "Pro",
            MaxUsers = 50,
            MaxAzureSubscriptions = 5,
            MaxMonthlyApiCalls = 100000,
            HasAdvancedAnalytics = true,
            HasMLPredictions = false,
            HasPowerBIIntegration = true,
            HasCustomBranding = false,
            HasSSOSupport = false,
            HasPrioritySupport = true,
            Description = "For growing teams",
            IsPopular = true,
            SortOrder = 2
        };

        // Assert
        plan.Id.Should().Be("plan_123");
        plan.Name.Should().Be("Professional");
        plan.MonthlyPrice.Should().Be(99);
        plan.AnnualPrice.Should().Be(990);
        plan.Tier.Should().Be("Pro");
        plan.MaxUsers.Should().Be(50);
        plan.IsPopular.Should().BeTrue();
    }

    [Fact]
    public void SubscriptionPlan_CanAddFeatures()
    {
        // Arrange & Act
        var plan = new SubscriptionPlan();
        plan.Features.Add("Feature 1");
        plan.Features.Add("Feature 2");
        plan.Features.Add("Feature 3");

        // Assert
        plan.Features.Should().HaveCount(3);
        plan.Features.Should().Contain("Feature 1");
    }

    [Theory]
    [InlineData("Free", 0, 0, 5, 1, 10000)]
    [InlineData("Pro", 99, 990, 50, 5, 100000)]
    [InlineData("Enterprise", 499, 4990, int.MaxValue, int.MaxValue, int.MaxValue)]
    public void SubscriptionPlan_CorrectTierConfiguration(
        string tier,
        decimal monthly,
        decimal annual,
        int users,
        int subs,
        int calls)
    {
        // Arrange & Act
        var plan = new SubscriptionPlan
        {
            Tier = tier,
            MonthlyPrice = monthly,
            AnnualPrice = annual,
            MaxUsers = users,
            MaxAzureSubscriptions = subs,
            MaxMonthlyApiCalls = calls
        };

        // Assert
        plan.Tier.Should().Be(tier);
        plan.MonthlyPrice.Should().Be(monthly);
        plan.AnnualPrice.Should().Be(annual);
        plan.MaxUsers.Should().Be(users);
        plan.MaxAzureSubscriptions.Should().Be(subs);
        plan.MaxMonthlyApiCalls.Should().Be(calls);
    }
}
