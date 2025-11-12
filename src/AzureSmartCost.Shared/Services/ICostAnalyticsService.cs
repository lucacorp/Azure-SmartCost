using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureSmartCost.Shared.Models.Analytics;

namespace AzureSmartCost.Shared.Services
{
    /// <summary>
    /// Service interface for cost analytics and ML predictions
    /// </summary>
    public interface ICostAnalyticsService
    {
        /// <summary>
        /// Generate cost predictions for a specific resource
        /// </summary>
        Task<CostPrediction> PredictResourceCostAsync(string resourceId, int daysAhead = 30);
        
        /// <summary>
        /// Generate cost predictions for an entire subscription
        /// </summary>
        Task<List<CostPrediction>> PredictSubscriptionCostsAsync(string subscriptionId, int daysAhead = 30);
        
        /// <summary>
        /// Detect anomalies in cost data
        /// </summary>
        Task<List<AnomalyAlert>> DetectCostAnomaliesAsync(string resourceId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Analyze cost trends for a resource
        /// </summary>
        Task<TrendAnalysis> AnalyzeCostTrendsAsync(string resourceId, int lookbackDays = 90);
        
        /// <summary>
        /// Get cost optimization recommendations
        /// </summary>
        Task<List<CostOptimizationRecommendation>> GetOptimizationRecommendationsAsync(string subscriptionId);
        
        /// <summary>
        /// Train ML models with latest cost data
        /// </summary>
        Task TrainModelsAsync();
        
        /// <summary>
        /// Get model performance metrics
        /// </summary>
        Task<ModelPerformanceMetrics> GetModelPerformanceAsync();
    }

    public class ModelPerformanceMetrics
    {
        public double Accuracy { get; set; }
        public double MeanAbsoluteError { get; set; }
        public double RootMeanSquareError { get; set; }
        public DateTime LastTrainingDate { get; set; }
        public int TrainingDataPoints { get; set; }
        public Dictionary<PredictionAlgorithm, double> AlgorithmPerformance { get; set; } = new Dictionary<PredictionAlgorithm, double>();
    }
}