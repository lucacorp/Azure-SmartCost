using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Tests.Models;

public class CostRecordModelTests
{
    [Fact]
    public void CostRecord_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var record = new CostRecord();

        // Assert
        record.SubscriptionId.Should().Be(string.Empty);
        record.SubscriptionName.Should().Be(string.Empty);
        record.ResourceGroup.Should().Be(string.Empty);
        record.ResourceName.Should().Be(string.Empty);
        record.MeterCategory.Should().BeNull();
        record.MeterSubCategory.Should().BeNull();
        record.MeterName.Should().BeNull();
        record.ServiceName.Should().Be(string.Empty);
        record.TotalCost.Should().Be(0);
        record.Currency.Should().Be("USD");
    }

    [Fact]
    public void CostRecord_CanSetAllProperties()
    {
        // Arrange
        var date = DateTime.UtcNow.Date;
        
        // Act
        var record = new CostRecord
        {
            Id = Guid.NewGuid().ToString(),
            SubscriptionId = "sub-123",
            SubscriptionName = "Production",
            ResourceGroup = "rg-prod",
            ResourceName = "vm-web-01",
            MeterCategory = "Virtual Machines",
            MeterSubCategory = "D-Series",
            MeterName = "D2s v3",
            ServiceName = "Virtual Machines",
            TotalCost = 50.25m,
            Currency = "USD",
            Date = date,
            Location = "East US"
        };

        // Assert
        record.SubscriptionId.Should().Be("sub-123");
        record.TotalCost.Should().Be(50.25m);
        record.Date.Should().Be(date);
        record.Location.Should().Be("East US");
    }

    [Fact]
    public void CostRecord_CostAlias_ShouldMapToTotalCost()
    {
        // Arrange & Act
        var record = new CostRecord
        {
            Cost = 10.50m
        };

        // Assert
        record.Cost.Should().Be(10.50m);
        record.TotalCost.Should().Be(10.50m);
    }

    [Theory]
    [InlineData("USD", 100.00)]
    [InlineData("EUR", 85.50)]
    [InlineData("BRL", 450.75)]
    public void CostRecord_SupportsDifferentCurrencies(string currency, decimal cost)
    {
        // Arrange & Act
        var record = new CostRecord
        {
            Currency = currency,
            TotalCost = cost
        };

        // Assert
        record.Currency.Should().Be(currency);
        record.TotalCost.Should().Be(cost);
    }

    [Fact]
    public void CostRecord_CanStoreTags()
    {
        // Arrange & Act
        var record = new CostRecord
        {
            Tags = new Dictionary<string, string>
            {
                { "environment", "prod" },
                { "team", "backend" },
                { "cost-center", "123" }
            }
        };

        // Assert
        record.Tags.Should().NotBeNull();
        record.Tags.Should().ContainKey("environment");
        record.Tags!["environment"].Should().Be("prod");
        record.Tags.Should().HaveCount(3);
    }

    [Fact]
    public void CostRecord_SetPartitionKey_ShouldFormatCorrectly()
    {
        // Arrange
        var record = new CostRecord
        {
            Date = new DateTime(2025, 11, 12)
        };

        // Act
        record.SetPartitionKey();

        // Assert
        record.PartitionKey.Should().Be("2025-11");
    }

    [Fact]
    public void CostRecord_ResourceGroupName_AliasesShouldWork()
    {
        // Arrange & Act
        var record = new CostRecord
        {
            ResourceGroupName = "rg-production"
        };

        // Assert
        record.ResourceGroupName.Should().Be("rg-production");
        record.ResourceGroup.Should().Be("rg-production");
    }

    [Fact]
    public void CostRecord_CanSetConsumedQuantity()
    {
        // Arrange & Act
        var record = new CostRecord
        {
            ConsumedQuantity = 730.5m,
            UnitOfMeasure = "Hours"
        };

        // Assert
        record.ConsumedQuantity.Should().Be(730.5m);
        record.UnitOfMeasure.Should().Be("Hours");
    }
}
