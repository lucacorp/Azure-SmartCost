using System;
using Xunit;
using FluentAssertions;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Tests.Models;

public class TenantModelTests
{
    [Fact]
    public void Tenant_ShouldInitializeWithGuid()
    {
        // Arrange & Act
        var tenant = new Tenant();

        // Assert
        Guid.TryParse(tenant.Id, out _).Should().BeTrue();
    }

    [Fact]
    public void Tenant_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var tenant = new Tenant();

        // Assert
        tenant.Name.Should().Be(string.Empty);
        tenant.CompanyName.Should().Be(string.Empty);
        tenant.Domain.Should().Be(string.Empty);
        tenant.ContactEmail.Should().Be(string.Empty);
        tenant.ContactPhone.Should().Be(string.Empty);
        tenant.SubscriptionTier.Should().Be("Free");
        tenant.IsActive.Should().BeTrue();
        tenant.IsTrialActive.Should().BeTrue();
        tenant.MaxUsers.Should().Be(5);
        tenant.MaxAzureSubscriptions.Should().Be(1);
        tenant.MaxMonthlyApiCalls.Should().Be(10000);
        tenant.HasAdvancedAnalytics.Should().BeFalse();
        tenant.HasMLPredictions.Should().BeFalse();
        tenant.HasPowerBIIntegration.Should().BeFalse();
        tenant.HasCustomBranding.Should().BeFalse();
        tenant.HasSSOSupport.Should().BeFalse();
        tenant.CurrentUserCount.Should().Be(0);
        tenant.CurrentAzureSubscriptionCount.Should().Be(0);
        tenant.CurrentMonthApiCalls.Should().Be(0);
    }

    [Theory]
    [InlineData("Free", 5, 1, 10000)]
    [InlineData("Pro", 50, 5, 100000)]
    [InlineData("Enterprise", int.MaxValue, int.MaxValue, int.MaxValue)]
    public void Tenant_CanSetTierLimits(string tier, int users, int subs, int calls)
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            SubscriptionTier = tier,
            MaxUsers = users,
            MaxAzureSubscriptions = subs,
            MaxMonthlyApiCalls = calls
        };

        // Assert
        tenant.SubscriptionTier.Should().Be(tier);
        tenant.MaxUsers.Should().Be(users);
        tenant.MaxAzureSubscriptions.Should().Be(subs);
        tenant.MaxMonthlyApiCalls.Should().Be(calls);
    }

    [Fact]
    public void Tenant_CanSetFeatures()
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            HasAdvancedAnalytics = true,
            HasMLPredictions = true,
            HasPowerBIIntegration = true,
            HasCustomBranding = true,
            HasSSOSupport = true
        };

        // Assert
        tenant.HasAdvancedAnalytics.Should().BeTrue();
        tenant.HasMLPredictions.Should().BeTrue();
        tenant.HasPowerBIIntegration.Should().BeTrue();
        tenant.HasCustomBranding.Should().BeTrue();
        tenant.HasSSOSupport.Should().BeTrue();
    }

    [Fact]
    public void Tenant_CanSetStripeInformation()
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            StripeCustomerId = "cus_123",
            StripeSubscriptionId = "sub_456",
            SubscriptionStartDate = DateTime.UtcNow,
            SubscriptionEndDate = DateTime.UtcNow.AddYears(1)
        };

        // Assert
        tenant.StripeCustomerId.Should().Be("cus_123");
        tenant.StripeSubscriptionId.Should().Be("sub_456");
        tenant.SubscriptionStartDate.Should().NotBeNull();
        tenant.SubscriptionEndDate.Should().NotBeNull();
    }

    [Fact]
    public void Tenant_CanSetAzureConfiguration()
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            AzureTenantId = "azure-tenant-123",
            AzureClientId = "azure-client-456",
            AzureClientSecret = "azure-secret-789"
        };

        // Assert
        tenant.AzureTenantId.Should().Be("azure-tenant-123");
        tenant.AzureClientId.Should().Be("azure-client-456");
        tenant.AzureClientSecret.Should().Be("azure-secret-789");
    }

    [Fact]
    public void Tenant_MetadataCanStoreValues()
    {
        // Arrange & Act
        var tenant = new Tenant();
        tenant.Metadata.Add("key1", "value1");
        tenant.Metadata.Add("key2", "value2");

        // Assert
        tenant.Metadata.Should().HaveCount(2);
        tenant.Metadata["key1"].Should().Be("value1");
        tenant.Metadata["key2"].Should().Be("value2");
    }

    [Fact]
    public void Tenant_CanTrackUsage()
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            CurrentUserCount = 10,
            CurrentAzureSubscriptionCount = 2,
            CurrentMonthApiCalls = 5000,
            LastApiCallReset = DateTime.UtcNow.AddDays(-5)
        };

        // Assert
        tenant.CurrentUserCount.Should().Be(10);
        tenant.CurrentAzureSubscriptionCount.Should().Be(2);
        tenant.CurrentMonthApiCalls.Should().Be(5000);
        tenant.LastApiCallReset.Should().BeCloseTo(DateTime.UtcNow.AddDays(-5), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Tenant_CanSetTrialInformation()
    {
        // Arrange & Act
        var trialEnd = DateTime.UtcNow.AddDays(14);
        var tenant = new Tenant
        {
            IsTrialActive = true,
            TrialEndDate = trialEnd
        };

        // Assert
        tenant.IsTrialActive.Should().BeTrue();
        tenant.TrialEndDate.Should().Be(trialEnd);
    }
}
