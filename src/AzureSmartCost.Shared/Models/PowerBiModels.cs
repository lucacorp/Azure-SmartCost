using System;
using System.ComponentModel.DataAnnotations;

namespace AzureSmartCost.Shared.Models
{
    /// <summary>
    /// Power BI report information
    /// </summary>
    public class PowerBiReport
    {
        /// <summary>
        /// Report unique identifier
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// Report name
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Web URL for the report
        /// </summary>
        public string WebUrl { get; set; } = "";

        /// <summary>
        /// Embed URL for the report
        /// </summary>
        public string EmbedUrl { get; set; } = "";

        /// <summary>
        /// Associated dataset ID
        /// </summary>
        public string DatasetId { get; set; } = "";

        /// <summary>
        /// Report creation date
        /// </summary>
        public DateTime CreatedDateTime { get; set; }

        /// <summary>
        /// Last modified date
        /// </summary>
        public DateTime ModifiedDateTime { get; set; }

        /// <summary>
        /// Report description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Report users and permissions
        /// </summary>
        public PowerBiReportUser[]? Users { get; set; }
    }

    /// <summary>
    /// Power BI report user information
    /// </summary>
    public class PowerBiReportUser
    {
        /// <summary>
        /// User email or identifier
        /// </summary>
        public string EmailAddress { get; set; } = "";

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName { get; set; } = "";

        /// <summary>
        /// User identifier
        /// </summary>
        public string Identifier { get; set; } = "";

        /// <summary>
        /// Principal type (User, Group, etc.)
        /// </summary>
        public string PrincipalType { get; set; } = "";

        /// <summary>
        /// Access right (Read, ReadWrite, etc.)
        /// </summary>
        public string AccessRight { get; set; } = "";
    }

    /// <summary>
    /// Power BI workspace information
    /// </summary>
    public class PowerBiWorkspace
    {
        /// <summary>
        /// Workspace unique identifier
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// Workspace name
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Workspace description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Workspace type (Personal, Group, etc.)
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// Workspace state (Active, Removing, etc.)
        /// </summary>
        public string State { get; set; } = "";

        /// <summary>
        /// Whether the workspace is read-only
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Whether the workspace is on dedicated capacity
        /// </summary>
        public bool IsOnDedicatedCapacity { get; set; }

        /// <summary>
        /// Capacity ID (if on dedicated capacity)
        /// </summary>
        public string? CapacityId { get; set; }

        /// <summary>
        /// Default dataset storage format
        /// </summary>
        public string? DefaultDatasetStorageFormat { get; set; }
    }

    /// <summary>
    /// Power BI import request for datasets
    /// </summary>
    public class PowerBiImportRequest
    {
        /// <summary>
        /// Dataset model to import
        /// </summary>
        [Required]
        public PowerBiDataset Dataset { get; set; } = new();

        /// <summary>
        /// Name conflict action (Abort, Overwrite, CreateOrOverwrite, GenerateUniqueName)
        /// </summary>
        public string NameConflict { get; set; } = "GenerateUniqueName";

        /// <summary>
        /// Whether to skip the report creation
        /// </summary>
        public bool SkipReport { get; set; } = false;

        /// <summary>
        /// Override model label
        /// </summary>
        public string? OverrideModelLabel { get; set; }

        /// <summary>
        /// Override report label
        /// </summary>
        public string? OverrideReportLabel { get; set; }
    }

    /// <summary>
    /// Power BI export request for reports
    /// </summary>
    public class PowerBiExportRequest
    {
        /// <summary>
        /// Export format (PDF, PPTX, PNG, etc.)
        /// </summary>
        [Required]
        public string Format { get; set; } = "PDF";

        /// <summary>
        /// Power BI configuration for export
        /// </summary>
        public PowerBiExportConfiguration? PowerBiConfiguration { get; set; }

        /// <summary>
        /// Report level filters
        /// </summary>
        public PowerBiExportFilter[]? ReportLevelFilters { get; set; }

        /// <summary>
        /// Display names for export
        /// </summary>
        public string? DisplayName { get; set; }
    }

    /// <summary>
    /// Power BI export configuration
    /// </summary>
    public class PowerBiExportConfiguration
    {
        /// <summary>
        /// Default bookmark
        /// </summary>
        public PowerBiBookmark? DefaultBookmark { get; set; }

        /// <summary>
        /// Include hidden pages
        /// </summary>
        public bool IncludeHiddenPages { get; set; } = false;

        /// <summary>
        /// Export pages settings
        /// </summary>
        public PowerBiExportPageSettings? PageSettings { get; set; }
    }

    /// <summary>
    /// Power BI export page settings
    /// </summary>
    public class PowerBiExportPageSettings
    {
        /// <summary>
        /// Page size format
        /// </summary>
        public string? PageSizeType { get; set; }

        /// <summary>
        /// Custom page height
        /// </summary>
        public int? PageHeight { get; set; }

        /// <summary>
        /// Custom page width
        /// </summary>
        public int? PageWidth { get; set; }
    }

    /// <summary>
    /// Power BI export filter
    /// </summary>
    public class PowerBiExportFilter
    {
        /// <summary>
        /// Filter target table
        /// </summary>
        public string Table { get; set; } = "";

        /// <summary>
        /// Filter target column
        /// </summary>
        public string Column { get; set; } = "";

        /// <summary>
        /// Filter operator
        /// </summary>
        public string Operator { get; set; } = "";

        /// <summary>
        /// Filter values
        /// </summary>
        public object[]? Values { get; set; }
    }

    /// <summary>
    /// Power BI bookmark
    /// </summary>
    public class PowerBiBookmark
    {
        /// <summary>
        /// Bookmark name
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Bookmark state
        /// </summary>
        public string State { get; set; } = "";
    }

    /// <summary>
    /// Power BI analytics information
    /// </summary>
    public class PowerBiAnalytics
    {
        /// <summary>
        /// Total report views
        /// </summary>
        public long TotalViews { get; set; }

        /// <summary>
        /// Unique viewers
        /// </summary>
        public long UniqueViewers { get; set; }

        /// <summary>
        /// Average session duration (minutes)
        /// </summary>
        public double AvgSessionDuration { get; set; }

        /// <summary>
        /// Most viewed pages
        /// </summary>
        public PowerBiPageAnalytics[]? TopPages { get; set; }

        /// <summary>
        /// View trends over time
        /// </summary>
        public PowerBiViewTrend[]? ViewTrends { get; set; }
    }

    /// <summary>
    /// Power BI page analytics
    /// </summary>
    public class PowerBiPageAnalytics
    {
        /// <summary>
        /// Page name
        /// </summary>
        public string PageName { get; set; } = "";

        /// <summary>
        /// View count
        /// </summary>
        public long ViewCount { get; set; }

        /// <summary>
        /// Average time spent (minutes)
        /// </summary>
        public double AvgTimeSpent { get; set; }
    }

    /// <summary>
    /// Power BI view trend data
    /// </summary>
    public class PowerBiViewTrend
    {
        /// <summary>
        /// Date of trend data
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// View count for the date
        /// </summary>
        public long ViewCount { get; set; }

        /// <summary>
        /// Unique viewers for the date
        /// </summary>
        public long UniqueViewers { get; set; }
    }

    /// <summary>
    /// SmartCost-specific Power BI report templates
    /// </summary>
    public static class SmartCostPowerBiReports
    {
        /// <summary>
        /// Executive dashboard report template
        /// </summary>
        public static PowerBiReport GetExecutiveDashboard()
        {
            return new PowerBiReport
            {
                Id = "smartcost-executive-dashboard",
                Name = "Azure SmartCost - Executive Dashboard",
                Description = "High-level cost overview and trends for executives",
                DatasetId = "smartcost-dataset",
                CreatedDateTime = DateTime.UtcNow,
                ModifiedDateTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Detailed cost analysis report template
        /// </summary>
        public static PowerBiReport GetDetailedCostAnalysis()
        {
            return new PowerBiReport
            {
                Id = "smartcost-detailed-analysis",
                Name = "Azure SmartCost - Detailed Cost Analysis",
                Description = "Detailed breakdown of Azure costs by service, region, and resource group",
                DatasetId = "smartcost-dataset",
                CreatedDateTime = DateTime.UtcNow,
                ModifiedDateTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Cost optimization recommendations report
        /// </summary>
        public static PowerBiReport GetCostOptimizationReport()
        {
            return new PowerBiReport
            {
                Id = "smartcost-optimization",
                Name = "Azure SmartCost - Cost Optimization",
                Description = "Cost optimization recommendations and savings opportunities",
                DatasetId = "smartcost-dataset",
                CreatedDateTime = DateTime.UtcNow,
                ModifiedDateTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Budget vs actual spending report
        /// </summary>
        public static PowerBiReport GetBudgetAnalysisReport()
        {
            return new PowerBiReport
            {
                Id = "smartcost-budget-analysis",
                Name = "Azure SmartCost - Budget Analysis",
                Description = "Budget vs actual spending analysis with forecasting",
                DatasetId = "smartcost-dataset",
                CreatedDateTime = DateTime.UtcNow,
                ModifiedDateTime = DateTime.UtcNow
            };
        }
    }
}