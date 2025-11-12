using System;
using System.Collections.Generic;

namespace AzureSmartCost.Shared.Models.Analytics
{
    /// <summary>
    /// Model for cost prediction analytics
    /// </summary>
    public class CostPrediction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ResourceId { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string SubscriptionId { get; set; } = string.Empty;
        public string ResourceGroupName { get; set; } = string.Empty;
        
        /// <summary>
        /// Date for which the prediction is made
        /// </summary>
        public DateTime PredictionDate { get; set; }
        
        /// <summary>
        /// Predicted cost amount
        /// </summary>
        public decimal PredictedCost { get; set; }
        
        /// <summary>
        /// Confidence level of the prediction (0.0 to 1.0)
        /// </summary>
        public double ConfidenceLevel { get; set; }
        
        /// <summary>
        /// Type of prediction algorithm used
        /// </summary>
        public PredictionAlgorithm Algorithm { get; set; }
        
        /// <summary>
        /// Trend analysis result
        /// </summary>
        public TrendAnalysis Trend { get; set; } = new TrendAnalysis();
        
        /// <summary>
        /// Anomaly detection results
        /// </summary>
        public List<AnomalyAlert> Anomalies { get; set; } = new List<AnomalyAlert>();
        
        /// <summary>
        /// When this prediction was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Historical data used for prediction
        /// </summary>
        public List<HistoricalCostPoint> HistoricalData { get; set; } = new List<HistoricalCostPoint>();
    }

    public enum PredictionAlgorithm
    {
        LinearRegression,
        ExponentialSmoothing,
        ARIMA,
        MachineLearning,
        EnsembleMethod
    }

    public class TrendAnalysis
    {
        public TrendDirection Direction { get; set; }
        public double TrendStrength { get; set; }
        public decimal MonthlyGrowthRate { get; set; }
        public decimal ProjectedMonthlyIncrease { get; set; }
        public bool IsSeasonalPattern { get; set; }
        public List<SeasonalPattern> SeasonalPatterns { get; set; } = new List<SeasonalPattern>();
    }

    public enum TrendDirection
    {
        Increasing,
        Decreasing,
        Stable,
        Volatile
    }

    public class SeasonalPattern
    {
        public string Period { get; set; } = string.Empty; // "Monthly", "Weekly", "Quarterly"
        public double Strength { get; set; }
        public Dictionary<string, decimal> PatternData { get; set; } = new Dictionary<string, decimal>();
    }

    public class AnomalyAlert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public AnomalySeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal ExpectedCost { get; set; }
        public decimal ActualCost { get; set; }
        public double DeviationPercentage { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public enum AnomalySeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class HistoricalCostPoint
    {
        public DateTime Date { get; set; }
        public decimal Cost { get; set; }
        public string Currency { get; set; } = "USD";
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Cost optimization recommendation
    /// </summary>
    public class CostOptimizationRecommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ResourceId { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public OptimizationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PotentialMonthlySavings { get; set; }
        public decimal PotentialAnnualSavings { get; set; }
        public OptimizationPriority Priority { get; set; }
        public double ImplementationEffort { get; set; } // 1.0 (Easy) to 5.0 (Very Hard)
        public List<string> ImplementationSteps { get; set; } = new List<string>();
        public List<string> Risks { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsImplemented { get; set; }
        public DateTime? ImplementedAt { get; set; }
    }

    public enum OptimizationType
    {
        RightsizingVM,
        ReservedInstances,
        SpotInstances,
        StorageOptimization,
        UnusedResources,
        ScheduledShutdown,
        AlternativeService,
        RegionOptimization
    }

    public enum OptimizationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}