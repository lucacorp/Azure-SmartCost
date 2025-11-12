using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureSmartCost.Shared.Models.Monitoring
{
    /// <summary>
    /// Model for system monitoring and alerting
    /// </summary>
    public class MonitoringAlert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public AlertType Type { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public string SubscriptionId { get; set; } = string.Empty;
        public AlertCondition Condition { get; set; } = new AlertCondition();
        public List<string> NotificationChannels { get; set; } = new List<string>();
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastTriggered { get; set; }
        public int TriggerCount { get; set; } = 0;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public enum AlertType
    {
        CostThreshold,
        AnomalyDetection,
        BudgetExceeded,
        UnusedResources,
        PerformanceDegradation,
        SecurityIncident,
        ComplianceViolation
    }

    public enum AlertSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Critical = 3
    }

    public class AlertCondition
    {
        public string MetricName { get; set; } = string.Empty;
        public ComparisonOperator Operator { get; set; }
        public double Threshold { get; set; }
        public TimeSpan EvaluationWindow { get; set; } = TimeSpan.FromMinutes(5);
        public int EvaluationFrequency { get; set; } = 1; // minutes
        public string Query { get; set; } = string.Empty; // KQL query for Application Insights
    }

    public enum ComparisonOperator
    {
        GreaterThan,
        LessThan,
        Equals,
        NotEquals,
        GreaterThanOrEqual,
        LessThanOrEqual
    }

    /// <summary>
    /// Performance monitoring dashboard model
    /// </summary>
    public class PerformanceDashboard
    {
        public string SubscriptionId { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public List<MetricSummary> Metrics { get; set; } = new List<MetricSummary>();
        public List<AlertSummary> ActiveAlerts { get; set; } = new List<AlertSummary>();
        public SystemHealth OverallHealth { get; set; } = new SystemHealth();
        public List<RecommendedAction> Recommendations { get; set; } = new List<RecommendedAction>();
    }

    public class MetricSummary
    {
        public string Name { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public double PreviousValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public TrendDirection Trend { get; set; }
        public List<DataPoint> TimeSeries { get; set; } = new List<DataPoint>();
    }

    public class DataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }

    public enum TrendDirection
    {
        Up,
        Down,
        Stable
    }

    public class AlertSummary
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
        public DateTime TriggeredAt { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class SystemHealth
    {
        public HealthStatus Status { get; set; }
        public double OverallScore { get; set; } // 0-100
        public List<HealthComponent> Components { get; set; } = new List<HealthComponent>();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public enum HealthStatus
    {
        Healthy,
        Warning,
        Critical,
        Unknown
    }

    public class HealthComponent
    {
        public string Name { get; set; } = string.Empty;
        public HealthStatus Status { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    }

    public class RecommendedAction
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ActionPriority Priority { get; set; }
        public List<string> Steps { get; set; } = new List<string>();
        public TimeSpan EstimatedTime { get; set; }
    }

    public enum ActionPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }

    /// <summary>
    /// Application Insights integration model
    /// </summary>
    public class ApplicationInsightsConfig
    {
        public string InstrumentationKey { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public string WorkspaceId { get; set; } = string.Empty;
        public List<CustomMetric> CustomMetrics { get; set; } = new List<CustomMetric>();
        public List<KqlQuery> AlertQueries { get; set; } = new List<KqlQuery>();
    }

    public class CustomMetric
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public MetricType Type { get; set; }
        public Dictionary<string, string> Dimensions { get; set; } = new Dictionary<string, string>();
    }

    public enum MetricType
    {
        Counter,
        Gauge,
        Histogram,
        Timer
    }

    public class KqlQuery
    {
        public string Name { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeSpan TimeRange { get; set; } = TimeSpan.FromHours(1);
        public bool IsAlert { get; set; }
        public AlertCondition? AlertCondition { get; set; }
    }
}