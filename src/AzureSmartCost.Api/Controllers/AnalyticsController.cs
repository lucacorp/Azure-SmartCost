using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AzureSmartCost.Shared.Services;
using AzureSmartCost.Shared.Models;
using AzureSmartCost.Shared.Models.Analytics;

namespace AzureSmartCost.Api.Controllers
{
    /// <summary>
    /// Controller for cost analytics and ML predictions
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly ICostAnalyticsService _analyticsService;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(ICostAnalyticsService analyticsService, ILogger<AnalyticsController> logger)
        {
            _analyticsService = analyticsService;
            _logger = logger;
        }

        /// <summary>
        /// Predict cost for a specific resource
        /// </summary>
        /// <param name="resourceId">Azure resource ID</param>
        /// <param name="daysAhead">Number of days to predict ahead (default: 30)</param>
        /// <returns>Cost prediction with confidence level</returns>
        [HttpGet("predict/resource/{resourceId}")]
        public async Task<ActionResult<ApiResponse<CostPrediction>>> PredictResourceCost(
            string resourceId, 
            [FromQuery] int daysAhead = 30)
        {
            try
            {
                _logger.LogInformation("Generating cost prediction for resource {ResourceId}", resourceId);
                
                if (string.IsNullOrEmpty(resourceId))
                {
                    return BadRequest(new ApiResponse<CostPrediction>
                    {
                        Success = false,
                        Message = "Resource ID is required",
                        Data = null
                    });
                }

                if (daysAhead <= 0 || daysAhead > 365)
                {
                    return BadRequest(new ApiResponse<CostPrediction>
                    {
                        Success = false,
                        Message = "Days ahead must be between 1 and 365",
                        Data = null
                    });
                }

                var prediction = await _analyticsService.PredictResourceCostAsync(resourceId, daysAhead);
                
                return Ok(new ApiResponse<CostPrediction>
                {
                    Success = true,
                    Message = "Cost prediction generated successfully",
                    Data = prediction
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating cost prediction for resource {ResourceId}", resourceId);
                return StatusCode(500, new ApiResponse<CostPrediction>
                {
                    Success = false,
                    Message = "An error occurred while generating cost prediction",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Predict costs for an entire subscription
        /// </summary>
        /// <param name="subscriptionId">Azure subscription ID</param>
        /// <param name="daysAhead">Number of days to predict ahead (default: 30)</param>
        /// <returns>List of cost predictions for all resources</returns>
        [HttpGet("predict/subscription/{subscriptionId}")]
        public async Task<ActionResult<ApiResponse<List<CostPrediction>>>> PredictSubscriptionCosts(
            string subscriptionId, 
            [FromQuery] int daysAhead = 30)
        {
            try
            {
                _logger.LogInformation("Generating cost predictions for subscription {SubscriptionId}", subscriptionId);
                
                var predictions = await _analyticsService.PredictSubscriptionCostsAsync(subscriptionId, daysAhead);
                
                return Ok(new ApiResponse<List<CostPrediction>>
                {
                    Success = true,
                    Message = $"Generated {predictions.Count} cost predictions",
                    Data = predictions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating cost predictions for subscription {SubscriptionId}", subscriptionId);
                return StatusCode(500, new ApiResponse<List<CostPrediction>>
                {
                    Success = false,
                    Message = "An error occurred while generating cost predictions",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Detect cost anomalies for a resource
        /// </summary>
        /// <param name="resourceId">Azure resource ID</param>
        /// <param name="startDate">Start date for anomaly detection</param>
        /// <param name="endDate">End date for anomaly detection</param>
        /// <returns>List of detected anomalies</returns>
        [HttpGet("anomalies/{resourceId}")]
        public async Task<ActionResult<ApiResponse<List<AnomalyAlert>>>> DetectAnomalies(
            string resourceId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Default to last 30 days if dates not provided
                var end = endDate ?? DateTime.UtcNow;
                var start = startDate ?? end.AddDays(-30);

                _logger.LogInformation("Detecting anomalies for resource {ResourceId} from {StartDate} to {EndDate}", 
                    resourceId, start, end);

                var anomalies = await _analyticsService.DetectCostAnomaliesAsync(resourceId, start, end);
                
                return Ok(new ApiResponse<List<AnomalyAlert>>
                {
                    Success = true,
                    Message = $"Found {anomalies.Count} anomalies",
                    Data = anomalies
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting anomalies for resource {ResourceId}", resourceId);
                return StatusCode(500, new ApiResponse<List<AnomalyAlert>>
                {
                    Success = false,
                    Message = "An error occurred while detecting anomalies",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Analyze cost trends for a resource
        /// </summary>
        /// <param name="resourceId">Azure resource ID</param>
        /// <param name="lookbackDays">Number of days to analyze (default: 90)</param>
        /// <returns>Trend analysis results</returns>
        [HttpGet("trends/{resourceId}")]
        public async Task<ActionResult<ApiResponse<TrendAnalysis>>> AnalyzeTrends(
            string resourceId,
            [FromQuery] int lookbackDays = 90)
        {
            try
            {
                _logger.LogInformation("Analyzing trends for resource {ResourceId} over {Days} days", 
                    resourceId, lookbackDays);

                var trends = await _analyticsService.AnalyzeCostTrendsAsync(resourceId, lookbackDays);
                
                return Ok(new ApiResponse<TrendAnalysis>
                {
                    Success = true,
                    Message = "Trend analysis completed successfully",
                    Data = trends
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing trends for resource {ResourceId}", resourceId);
                return StatusCode(500, new ApiResponse<TrendAnalysis>
                {
                    Success = false,
                    Message = "An error occurred while analyzing trends",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get cost optimization recommendations
        /// </summary>
        /// <param name="subscriptionId">Azure subscription ID</param>
        /// <returns>List of optimization recommendations</returns>
        [HttpGet("recommendations/{subscriptionId}")]
        public async Task<ActionResult<ApiResponse<List<CostOptimizationRecommendation>>>> GetRecommendations(
            string subscriptionId)
        {
            try
            {
                _logger.LogInformation("Generating recommendations for subscription {SubscriptionId}", subscriptionId);

                var recommendations = await _analyticsService.GetOptimizationRecommendationsAsync(subscriptionId);
                
                return Ok(new ApiResponse<List<CostOptimizationRecommendation>>
                {
                    Success = true,
                    Message = $"Generated {recommendations.Count} optimization recommendations",
                    Data = recommendations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recommendations for subscription {SubscriptionId}", subscriptionId);
                return StatusCode(500, new ApiResponse<List<CostOptimizationRecommendation>>
                {
                    Success = false,
                    Message = "An error occurred while generating recommendations",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Train ML models with latest data
        /// </summary>
        /// <returns>Training result</returns>
        [HttpPost("train")]
        public async Task<ActionResult<ApiResponse<string>>> TrainModels()
        {
            try
            {
                _logger.LogInformation("Starting ML model training");

                await _analyticsService.TrainModelsAsync();
                
                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "ML models trained successfully",
                    Data = "Training completed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training ML models");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "An error occurred while training models",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get ML model performance metrics
        /// </summary>
        /// <returns>Model performance data</returns>
        [HttpGet("model-performance")]
        public async Task<ActionResult<ApiResponse<ModelPerformanceMetrics>>> GetModelPerformance()
        {
            try
            {
                _logger.LogInformation("Retrieving model performance metrics");

                var performance = await _analyticsService.GetModelPerformanceAsync();
                
                return Ok(new ApiResponse<ModelPerformanceMetrics>
                {
                    Success = true,
                    Message = "Model performance metrics retrieved successfully",
                    Data = performance
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving model performance");
                return StatusCode(500, new ApiResponse<ModelPerformanceMetrics>
                {
                    Success = false,
                    Message = "An error occurred while retrieving model performance",
                    Data = null
                });
            }
        }

        /// <summary>
        /// Get comprehensive analytics dashboard data
        /// </summary>
        /// <param name="subscriptionId">Azure subscription ID</param>
        /// <returns>Dashboard data including predictions, trends, and recommendations</returns>
        [HttpGet("dashboard/{subscriptionId}")]
        public async Task<ActionResult<ApiResponse<AnalyticsDashboard>>> GetDashboard(string subscriptionId)
        {
            try
            {
                _logger.LogInformation("Generating analytics dashboard for subscription {SubscriptionId}", subscriptionId);

                // Get all analytics data for dashboard
                var predictions = await _analyticsService.PredictSubscriptionCostsAsync(subscriptionId, 30);
                var recommendations = await _analyticsService.GetOptimizationRecommendationsAsync(subscriptionId);
                var modelPerformance = await _analyticsService.GetModelPerformanceAsync();

                // Calculate summary statistics
                var totalPredictedCost = predictions.Sum(p => p.PredictedCost);
                var totalPotentialSavings = recommendations.Sum(r => r.PotentialMonthlySavings);
                var avgConfidence = predictions.Any() ? predictions.Average(p => p.ConfidenceLevel) : 0;

                var dashboard = new AnalyticsDashboard
                {
                    SubscriptionId = subscriptionId,
                    TotalPredictedMonthlyCost = totalPredictedCost,
                    TotalPotentialMonthlySavings = totalPotentialSavings,
                    AverageConfidenceLevel = avgConfidence,
                    PredictionsCount = predictions.Count,
                    RecommendationsCount = recommendations.Count,
                    TopPredictions = predictions.OrderByDescending(p => p.PredictedCost).Take(10).ToList(),
                    TopRecommendations = recommendations.Take(5).ToList(),
                    ModelPerformance = modelPerformance,
                    GeneratedAt = DateTime.UtcNow
                };

                return Ok(new ApiResponse<AnalyticsDashboard>
                {
                    Success = true,
                    Message = "Analytics dashboard generated successfully",
                    Data = dashboard
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dashboard for subscription {SubscriptionId}", subscriptionId);
                return StatusCode(500, new ApiResponse<AnalyticsDashboard>
                {
                    Success = false,
                    Message = "An error occurred while generating dashboard",
                    Data = null
                });
            }
        }
    }

    /// <summary>
    /// Analytics dashboard data model
    /// </summary>
    public class AnalyticsDashboard
    {
        public string SubscriptionId { get; set; } = string.Empty;
        public decimal TotalPredictedMonthlyCost { get; set; }
        public decimal TotalPotentialMonthlySavings { get; set; }
        public double AverageConfidenceLevel { get; set; }
        public int PredictionsCount { get; set; }
        public int RecommendationsCount { get; set; }
        public List<CostPrediction> TopPredictions { get; set; } = new List<CostPrediction>();
        public List<CostOptimizationRecommendation> TopRecommendations { get; set; } = new List<CostOptimizationRecommendation>();
        public ModelPerformanceMetrics ModelPerformance { get; set; } = new ModelPerformanceMetrics();
        public DateTime GeneratedAt { get; set; }
    }
}