using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;
using AzureSmartCost.Api.Controllers;
using AzureSmartCost.Shared.Models;
using AzureSmartCost.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureSmartCost.Tests.Controllers
{
    public class PowerBiControllerTests
    {
        private readonly Mock<ILogger<PowerBiController>> _mockLogger;
        private readonly Mock<ICostManagementService> _mockCostManagementService;
        private readonly Mock<ICosmosDbService> _mockCosmosDbService;
        private readonly Mock<IPowerBiService> _mockPowerBiService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly PowerBiController _controller;

        public PowerBiControllerTests()
        {
            _mockLogger = new Mock<ILogger<PowerBiController>>();
            _mockCostManagementService = new Mock<ICostManagementService>();
            _mockCosmosDbService = new Mock<ICosmosDbService>();
            _mockPowerBiService = new Mock<IPowerBiService>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Setup configuration defaults
            _mockConfiguration.Setup(x => x["PowerBI:WorkspaceId"]).Returns("test-workspace-id");
            _mockConfiguration.Setup(x => x["PowerBI:DatasetId"]).Returns("test-dataset-id");

            _controller = new PowerBiController(
                _mockLogger.Object,
                _mockCostManagementService.Object,
                _mockCosmosDbService.Object,
                _mockPowerBiService.Object,
                _mockConfiguration.Object);
        }

        [Fact]
        public async Task GetPowerBiCostData_WithValidDateRange_ReturnsOkResult()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var mockCostRecords = new List<CostRecord>
            {
                new CostRecord
                {
                    Id = "test-1",
                    Date = startDate.AddDays(1),
                    TotalCost = 100.50m,
                    Currency = "USD",
                    ServiceName = "Virtual Machines",
                    ResourceGroup = "rg-test",
                    SubscriptionId = "sub-123",
                    ResourceName = "vm-test",
                    Location = "East US",
                    SubscriptionName = "Test Subscription"
                },
                new CostRecord
                {
                    Id = "test-2",
                    Date = startDate.AddDays(2),
                    TotalCost = 75.25m,
                    Currency = "USD",
                    ServiceName = "Storage",
                    ResourceGroup = "rg-storage",
                    SubscriptionId = "sub-123",
                    ResourceName = "storage-test",
                    Location = "West US",
                    SubscriptionName = "Test Subscription"
                }
            };

            _mockCosmosDbService
                .Setup(x => x.GetCostRecordsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()))
                .ReturnsAsync(mockCostRecords);

            // Act
            var result = await _controller.GetPowerBiCostData(startDate, endDate, null);

            // Assert
            result.Should().BeOfType<ActionResult<PowerBiCostData>>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var powerBiData = okResult.Value.Should().BeOfType<PowerBiCostData>().Subject;

            powerBiData.CostRecords.Should().HaveCount(2);
            powerBiData.TotalCost.Should().Be(175.75m);
            powerBiData.RecordCount.Should().Be(2);
            powerBiData.ServiceBreakdown.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetPowerBiCostData_WithNoData_ReturnsEmptyResult()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;

            _mockCosmosDbService
                .Setup(x => x.GetCostRecordsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()))
                .ReturnsAsync(new List<CostRecord>());

            // Act
            var result = await _controller.GetPowerBiCostData(startDate, endDate, null);

            // Assert
            result.Should().BeOfType<ActionResult<PowerBiCostData>>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var powerBiData = okResult.Value.Should().BeOfType<PowerBiCostData>().Subject;

            powerBiData.CostRecords.Should().BeEmpty();
            powerBiData.TotalCost.Should().Be(0);
            powerBiData.RecordCount.Should().Be(0);
            powerBiData.ServiceBreakdown.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPowerBiCostData_WithException_ReturnsInternalServerError()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;

            _mockCosmosDbService
                .Setup(x => x.GetCostRecordsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.GetPowerBiCostData(startDate, endDate, null);

            // Assert
            result.Should().BeOfType<ActionResult<PowerBiCostData>>();
            var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);

            var apiResponse = statusResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Message.Should().Be("Failed to retrieve Power BI cost data");
            apiResponse.Errors.Should().Contain("Database connection failed");
        }

        [Fact]
        public async Task GetPowerBiEmbedConfig_WithValidReportId_ReturnsOkResult()
        {
            // Arrange
            var reportId = "test-report-id";
            var mockEmbedConfig = new PowerBiEmbedConfig
            {
                Type = "report",
                Id = reportId,
                EmbedUrl = "https://app.powerbi.com/reportEmbed",
                AccessToken = "test-token",
                TokenType = "Embed",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            _mockPowerBiService
                .Setup(x => x.GetEmbedTokenAsync(reportId, It.IsAny<string>()))
                .ReturnsAsync(mockEmbedConfig);

            // Act
            var result = await _controller.GetPowerBiEmbedConfig(reportId, null);

            // Assert
            result.Should().BeOfType<ActionResult<PowerBiEmbedConfig>>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var embedConfig = okResult.Value.Should().BeOfType<PowerBiEmbedConfig>().Subject;

            embedConfig.Id.Should().Be(reportId);
            embedConfig.Type.Should().Be("report");
            embedConfig.AccessToken.Should().Be("test-token");
        }

        [Fact]
        public async Task GetPowerBiEmbedConfig_WithEmptyReportId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetPowerBiEmbedConfig("", null);

            // Assert
            result.Should().BeOfType<ActionResult<PowerBiEmbedConfig>>();
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            
            var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Message.Should().Be("Report ID is required");
        }

        [Fact]
        public async Task RefreshPowerBiDataset_WithSuccess_ReturnsOkResult()
        {
            // Arrange
            _mockPowerBiService
                .Setup(x => x.RefreshDatasetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.RefreshPowerBiDataset(null, null);

            // Assert
            result.Should().BeOfType<ActionResult<ApiResponse<object>>>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;

            apiResponse.Success.Should().BeTrue();
            apiResponse.Message.Should().Be("Dataset refresh triggered successfully");
        }

        [Fact]
        public async Task RefreshPowerBiDataset_WithFailure_ReturnsInternalServerError()
        {
            // Arrange
            _mockPowerBiService
                .Setup(x => x.RefreshDatasetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.RefreshPowerBiDataset(null, null);

            // Assert
            result.Should().BeOfType<ActionResult<ApiResponse<object>>>();
            var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);

            var apiResponse = statusResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Message.Should().Be("Failed to trigger dataset refresh");
        }

        [Fact]
        public void GetPowerBiTemplates_ReturnsListOfTemplates()
        {
            // Act
            var result = _controller.GetPowerBiTemplates();

            // Assert
            result.Should().BeOfType<ActionResult<IEnumerable<PowerBiReport>>>();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var templates = okResult.Value.Should().BeAssignableTo<IEnumerable<PowerBiReport>>().Subject;

            templates.Should().HaveCount(4);
            templates.Should().Contain(t => t.Name == "Azure SmartCost - Executive Dashboard");
            templates.Should().Contain(t => t.Name == "Azure SmartCost - Detailed Cost Analysis");
            templates.Should().Contain(t => t.Name == "Azure SmartCost - Cost Optimization");
            templates.Should().Contain(t => t.Name == "Azure SmartCost - Budget Analysis");
        }
    }
}