using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzureSmartCost.Shared.Models
{
    /// <summary>
    /// Power BI dataset configuration
    /// </summary>
    public class PowerBiDataset
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("tables")]
        public List<PowerBiTable> Tables { get; set; } = new List<PowerBiTable>();

        [JsonPropertyName("relationships")]
        public List<PowerBiRelationship> Relationships { get; set; } = new List<PowerBiRelationship>();

        [JsonPropertyName("datasources")]
        public List<PowerBiDatasource> Datasources { get; set; } = new List<PowerBiDatasource>();

        [JsonPropertyName("configuredBy")]
        public string ConfiguredBy { get; set; } = string.Empty;

        [JsonPropertyName("isRefreshable")]
        public bool IsRefreshable { get; set; } = true;

        [JsonPropertyName("lastRefresh")]
        public DateTime? LastRefresh { get; set; }
    }

    /// <summary>
    /// Power BI table definition
    /// </summary>
    public class PowerBiTable
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("columns")]
        public List<PowerBiColumn> Columns { get; set; } = new List<PowerBiColumn>();

        [JsonPropertyName("measures")]
        public List<PowerBiMeasure> Measures { get; set; } = new List<PowerBiMeasure>();

        [JsonPropertyName("isHidden")]
        public bool IsHidden { get; set; } = false;
    }

    /// <summary>
    /// Power BI column definition
    /// </summary>
    public class PowerBiColumn
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("dataType")]
        public string DataType { get; set; } = string.Empty; // Int64, Double, DateTime, String, Boolean

        [JsonPropertyName("formatString")]
        public string? FormatString { get; set; }

        [JsonPropertyName("sortByColumn")]
        public string? SortByColumn { get; set; }

        [JsonPropertyName("isHidden")]
        public bool IsHidden { get; set; } = false;

        [JsonPropertyName("summarizeBy")]
        public string SummarizeBy { get; set; } = "Default"; // None, Sum, Count, Min, Max, Average, CountNonNull
    }

    /// <summary>
    /// Power BI measure definition
    /// </summary>
    public class PowerBiMeasure
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("expression")]
        public string Expression { get; set; } = string.Empty; // DAX expression

        [JsonPropertyName("formatString")]
        public string? FormatString { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("displayFolder")]
        public string? DisplayFolder { get; set; }

        [JsonPropertyName("isHidden")]
        public bool IsHidden { get; set; } = false;
    }

    /// <summary>
    /// Power BI relationship definition
    /// </summary>
    public class PowerBiRelationship
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("fromTable")]
        public string FromTable { get; set; } = string.Empty;

        [JsonPropertyName("fromColumn")]
        public string FromColumn { get; set; } = string.Empty;

        [JsonPropertyName("toTable")]
        public string ToTable { get; set; } = string.Empty;

        [JsonPropertyName("toColumn")]
        public string ToColumn { get; set; } = string.Empty;

        [JsonPropertyName("cardinality")]
        public string Cardinality { get; set; } = "OneToMany"; // OneToOne, OneToMany, ManyToOne, ManyToMany

        [JsonPropertyName("crossFilterDirection")]
        public string CrossFilterDirection { get; set; } = "OneDirection"; // OneDirection, BothDirections

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Power BI datasource configuration
    /// </summary>
    public class PowerBiDatasource
    {
        [JsonPropertyName("datasourceType")]
        public string DatasourceType { get; set; } = string.Empty; // RestApi, AzureCosmosDB, etc.

        [JsonPropertyName("connectionDetails")]
        public PowerBiConnectionDetails ConnectionDetails { get; set; } = new PowerBiConnectionDetails();

        [JsonPropertyName("gatewayId")]
        public string? GatewayId { get; set; }
    }

    /// <summary>
    /// Power BI connection details
    /// </summary>
    public class PowerBiConnectionDetails
    {
        [JsonPropertyName("server")]
        public string? Server { get; set; }

        [JsonPropertyName("database")]
        public string? Database { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }
    }

    /// <summary>
    /// Power BI embedding configuration
    /// </summary>
    public class PowerBiEmbedConfig
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "report"; // report, dashboard, tile

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("embedUrl")]
        public string EmbedUrl { get; set; } = string.Empty;

        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("tokenType")]
        public string TokenType { get; set; } = "Embed"; // Embed, Aad

        [JsonPropertyName("settings")]
        public PowerBiEmbedSettings Settings { get; set; } = new PowerBiEmbedSettings();

        [JsonPropertyName("expiresAt")]
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// Power BI embedding settings
    /// </summary>
    public class PowerBiEmbedSettings
    {
        [JsonPropertyName("filterPaneEnabled")]
        public bool FilterPaneEnabled { get; set; } = true;

        [JsonPropertyName("navContentPaneEnabled")]
        public bool NavContentPaneEnabled { get; set; } = true;

        [JsonPropertyName("background")]
        public string Background { get; set; } = "transparent"; // transparent, default

        [JsonPropertyName("theme")]
        public string? Theme { get; set; }

        [JsonPropertyName("customLayout")]
        public PowerBiCustomLayout? CustomLayout { get; set; }

        [JsonPropertyName("bookmarksPane")]
        public PowerBiPane BookmarksPane { get; set; } = new PowerBiPane();

        [JsonPropertyName("fieldsPane")]
        public PowerBiPane FieldsPane { get; set; } = new PowerBiPane();

        [JsonPropertyName("pageNavigation")]
        public PowerBiPageNavigation PageNavigation { get; set; } = new PowerBiPageNavigation();
    }

    /// <summary>
    /// Power BI custom layout configuration
    /// </summary>
    public class PowerBiCustomLayout
    {
        [JsonPropertyName("displayOption")]
        public string DisplayOption { get; set; } = "FitToPage"; // FitToPage, FitToWidth, ActualSize

        [JsonPropertyName("pagesLayout")]
        public Dictionary<string, PowerBiPageLayout> PagesLayout { get; set; } = new Dictionary<string, PowerBiPageLayout>();
    }

    /// <summary>
    /// Power BI page layout
    /// </summary>
    public class PowerBiPageLayout
    {
        [JsonPropertyName("defaultLayout")]
        public PowerBiVisualLayout DefaultLayout { get; set; } = new PowerBiVisualLayout();

        [JsonPropertyName("mobileLayout")]
        public PowerBiVisualLayout? MobileLayout { get; set; }
    }

    /// <summary>
    /// Power BI visual layout
    /// </summary>
    public class PowerBiVisualLayout
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("z")]
        public int Z { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("displayState")]
        public PowerBiDisplayState DisplayState { get; set; } = new PowerBiDisplayState();
    }

    /// <summary>
    /// Power BI display state
    /// </summary>
    public class PowerBiDisplayState
    {
        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "Visible"; // Visible, Hidden
    }

    /// <summary>
    /// Power BI pane configuration
    /// </summary>
    public class PowerBiPane
    {
        [JsonPropertyName("visible")]
        public bool Visible { get; set; } = false;
    }

    /// <summary>
    /// Power BI page navigation configuration
    /// </summary>
    public class PowerBiPageNavigation
    {
        [JsonPropertyName("position")]
        public string Position { get; set; } = "Bottom"; // Bottom, Top, Left, Right
    }

    /// <summary>
    /// Power BI refresh request
    /// </summary>
    public class PowerBiRefreshRequest
    {
        [JsonPropertyName("datasetId")]
        public string DatasetId { get; set; } = string.Empty;

        [JsonPropertyName("refreshType")]
        public string RefreshType { get; set; } = "Full"; // Full, ClearValues, Calculate, DataOnly, Automatic, Add, Complete

        [JsonPropertyName("commitMode")]
        public string CommitMode { get; set; } = "Transactional"; // Transactional, PartialBatch

        [JsonPropertyName("maxParallelism")]
        public int MaxParallelism { get; set; } = 2;

        [JsonPropertyName("retryCount")]
        public int RetryCount { get; set; } = 0;

        [JsonPropertyName("objects")]
        public List<PowerBiRefreshObject>? Objects { get; set; }
    }

    /// <summary>
    /// Power BI refresh object
    /// </summary>
    public class PowerBiRefreshObject
    {
        [JsonPropertyName("table")]
        public string Table { get; set; } = string.Empty;

        [JsonPropertyName("partition")]
        public string? Partition { get; set; }
    }

    /// <summary>
    /// Power BI template configuration for Azure SmartCost
    /// </summary>
    public static class SmartCostPowerBiTemplates
    {
        /// <summary>
        /// Get the standard Azure SmartCost dataset definition
        /// </summary>
        public static PowerBiDataset GetSmartCostDataset()
        {
            return new PowerBiDataset
            {
                Id = "smartcost-dataset",
                Name = "Azure SmartCost Analysis",
                Tables = new List<PowerBiTable>
                {
                    // Cost Records table
                    new PowerBiTable
                    {
                        Name = "CostRecords",
                        Columns = new List<PowerBiColumn>
                        {
                            new PowerBiColumn { Name = "Date", DataType = "DateTime", FormatString = "dd/MM/yyyy", SortByColumn = "Date" },
                            new PowerBiColumn { Name = "Year", DataType = "Int64", SummarizeBy = "None" },
                            new PowerBiColumn { Name = "Month", DataType = "Int64", SummarizeBy = "None" },
                            new PowerBiColumn { Name = "WeekOfYear", DataType = "Int64", SummarizeBy = "None" },
                            new PowerBiColumn { Name = "ServiceName", DataType = "String", SummarizeBy = "None" },
                            new PowerBiColumn { Name = "ResourceGroup", DataType = "String", SummarizeBy = "None" },
                            new PowerBiColumn { Name = "Location", DataType = "String", SummarizeBy = "None" },
                            new PowerBiColumn { Name = "Cost", DataType = "Double", FormatString = "$#,0.00", SummarizeBy = "Sum" },
                            new PowerBiColumn { Name = "Currency", DataType = "String", SummarizeBy = "None" },
                            new PowerBiColumn { Name = "Quantity", DataType = "Double", SummarizeBy = "Sum" },
                            new PowerBiColumn { Name = "UnitOfMeasure", DataType = "String", SummarizeBy = "None" },
                            new PowerBiColumn { Name = "ResourceId", DataType = "String", SummarizeBy = "None" },
                            new PowerBiColumn { Name = "SubscriptionId", DataType = "String", SummarizeBy = "None" }
                        },
                        Measures = new List<PowerBiMeasure>
                        {
                            new PowerBiMeasure 
                            { 
                                Name = "Total Cost", 
                                Expression = "SUM(CostRecords[Cost])", 
                                FormatString = "$#,0.00",
                                Description = "Total cost across all services"
                            },
                            new PowerBiMeasure 
                            { 
                                Name = "Average Daily Cost", 
                                Expression = "AVERAGE(CostRecords[Cost])", 
                                FormatString = "$#,0.00",
                                Description = "Average cost per day"
                            },
                            new PowerBiMeasure 
                            { 
                                Name = "Cost Growth %", 
                                Expression = "DIVIDE([Total Cost] - [Previous Month Cost], [Previous Month Cost], 0) * 100", 
                                FormatString = "0.00%",
                                Description = "Cost growth percentage compared to previous month"
                            },
                            new PowerBiMeasure 
                            { 
                                Name = "Previous Month Cost", 
                                Expression = "CALCULATE([Total Cost], DATEADD(CostRecords[Date], -1, MONTH))", 
                                FormatString = "$#,0.00",
                                Description = "Total cost for the previous month"
                            }
                        }
                    },
                    
                    // Date dimension table
                    new PowerBiTable
                    {
                        Name = "DateDimension",
                        Columns = new List<PowerBiColumn>
                        {
                            new PowerBiColumn { Name = "Date", DataType = "DateTime", FormatString = "dd/MM/yyyy" },
                            new PowerBiColumn { Name = "Year", DataType = "Int64", SummarizeBy = "None" },
                            new PowerBiColumn { Name = "Quarter", DataType = "Int64", SummarizeBy = "None" },
                            new PowerBiColumn { Name = "Month", DataType = "Int64", SummarizeBy = "None" },
                            new PowerBiColumn { Name = "MonthName", DataType = "String", SummarizeBy = "None" },
                            new PowerBiColumn { Name = "WeekOfYear", DataType = "Int64", SummarizeBy = "None" },
                            new PowerBiColumn { Name = "DayOfWeek", DataType = "String", SummarizeBy = "None" },
                            new PowerBiColumn { Name = "IsWeekend", DataType = "Boolean", SummarizeBy = "None" }
                        }
                    }
                },
                Relationships = new List<PowerBiRelationship>
                {
                    new PowerBiRelationship
                    {
                        Name = "CostRecords-DateDimension",
                        FromTable = "CostRecords",
                        FromColumn = "Date",
                        ToTable = "DateDimension",
                        ToColumn = "Date",
                        Cardinality = "ManyToOne",
                        CrossFilterDirection = "OneDirection"
                    }
                }
            };
        }

        /// <summary>
        /// Get the standard Azure SmartCost embedding configuration
        /// </summary>
        public static PowerBiEmbedConfig GetSmartCostEmbedConfig(string reportId, string embedUrl, string accessToken)
        {
            return new PowerBiEmbedConfig
            {
                Type = "report",
                Id = reportId,
                EmbedUrl = embedUrl,
                AccessToken = accessToken,
                TokenType = "Embed",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Settings = new PowerBiEmbedSettings
                {
                    FilterPaneEnabled = true,
                    NavContentPaneEnabled = true,
                    Background = "transparent",
                    BookmarksPane = new PowerBiPane { Visible = false },
                    FieldsPane = new PowerBiPane { Visible = false },
                    PageNavigation = new PowerBiPageNavigation { Position = "Bottom" }
                }
            };
        }
    }

    /// <summary>
    /// Power BI structured cost data for Azure SmartCost
    /// </summary>
    public class PowerBiCostData
    {
        public List<PowerBiCostRecord> CostRecords { get; set; } = new();
        public decimal TotalCost { get; set; }
        public int RecordCount { get; set; }
        public PowerBiDateRange DateRange { get; set; } = new();
        public List<PowerBiDateDimension> DateDimension { get; set; } = new();
        public List<PowerBiServiceBreakdown> ServiceBreakdown { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Power BI cost record with enhanced dimensions
    /// </summary>
    public class PowerBiCostRecord
    {
        // Date dimensions
        public DateTime Date { get; set; }
        public int Year { get; set; }
        public string Quarter { get; set; } = "";
        public string Month { get; set; } = "";
        public string MonthName { get; set; } = "";
        public int WeekOfYear { get; set; }
        public string DayOfWeek { get; set; } = "";
        
        // Cost information
        public decimal Cost { get; set; }
        public string Currency { get; set; } = "";
        public string ServiceName { get; set; } = "";
        public string ResourceGroupName { get; set; } = "";
        public string ResourceName { get; set; } = "";
        public string Location { get; set; } = "";
        public string SubscriptionId { get; set; } = "";
        public string SubscriptionName { get; set; } = "";
        
        // Additional dimensions
        public string MeterCategory { get; set; } = "";
        public string MeterSubCategory { get; set; } = "";
        public string MeterName { get; set; } = "";
        public string UnitOfMeasure { get; set; } = "";
        public decimal ConsumedQuantity { get; set; }
        
        // Calculated fields
        public string CostCategory { get; set; } = "";
        public bool IsWeekend { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Date range information for Power BI
    /// </summary>
    public class PowerBiDateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysIncluded { get; set; }
    }

    /// <summary>
    /// Date dimension table for Power BI
    /// </summary>
    public class PowerBiDateDimension
    {
        public DateTime Date { get; set; }
        public int Year { get; set; }
        public string Quarter { get; set; } = "";
        public string Month { get; set; } = "";
        public string MonthName { get; set; } = "";
        public int WeekOfYear { get; set; }
        public string DayOfWeek { get; set; } = "";
        public int DayOfMonth { get; set; }
        public int DayOfYear { get; set; }
        public bool IsWeekend { get; set; }
        public bool IsHoliday { get; set; }
        public int FiscalYear { get; set; }
        public string FiscalQuarter { get; set; } = "";
    }

    /// <summary>
    /// Service breakdown for Power BI
    /// </summary>
    public class PowerBiServiceBreakdown
    {
        public string ServiceName { get; set; } = "";
        public decimal TotalCost { get; set; }
        public int RecordCount { get; set; }
        public decimal AvgDailyCost { get; set; }
        public string TopResourceGroup { get; set; } = "";
    }

    /// <summary>
    /// Power BI trends data structure
    /// </summary>
    public class PowerBiTrendsData
    {
        public string Granularity { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<PowerBiTrendPoint> Trends { get; set; } = new();
        public PowerBiTrendsSummary? Summary { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Individual trend point for Power BI
    /// </summary>
    public class PowerBiTrendPoint
    {
        public string Period { get; set; } = "";
        public DateTime PeriodDate { get; set; }
        public decimal TotalCost { get; set; }
        public int RecordCount { get; set; }
        public decimal AvgCostPerRecord { get; set; }
        public int UniqueServices { get; set; }
        public string TopService { get; set; } = "";
        public decimal CostVariance { get; set; }
        public decimal CostVariancePercentage { get; set; }
    }

    /// <summary>
    /// Summary statistics for trends
    /// </summary>
    public class PowerBiTrendsSummary
    {
        public decimal TotalCost { get; set; }
        public decimal AvgPeriodCost { get; set; }
        public decimal MaxPeriodCost { get; set; }
        public decimal MinPeriodCost { get; set; }
        public int TotalRecords { get; set; }
        public int PeriodsWithIncrease { get; set; }
        public int PeriodsWithDecrease { get; set; }
        public string OverallTrend { get; set; } = "";
    }
}