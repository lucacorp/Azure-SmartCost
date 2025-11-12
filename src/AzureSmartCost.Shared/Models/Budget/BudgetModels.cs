using System;
using System.Collections.Generic;

namespace AzureSmartCost.Shared.Models.Budget
{
    /// <summary>
    /// Budget management models for cost allocation and approval workflows
    /// </summary>
    public class Budget
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public BudgetType Type { get; set; }
        public BudgetScope Scope { get; set; } = new BudgetScope();
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public BudgetPeriod Period { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public BudgetStatus Status { get; set; } = BudgetStatus.Active;
        public List<BudgetThreshold> Thresholds { get; set; } = new List<BudgetThreshold>();
        public List<string> Approvers { get; set; } = new List<string>();
        public string OwnerId { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModified { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public enum BudgetType
    {
        Subscription,
        ResourceGroup,
        Department,
        Project,
        CostCenter,
        Service
    }

    public class BudgetScope
    {
        public string SubscriptionId { get; set; } = string.Empty;
        public List<string> ResourceGroups { get; set; } = new List<string>();
        public List<string> Resources { get; set; } = new List<string>();
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
        public List<string> Services { get; set; } = new List<string>();
    }

    public enum BudgetPeriod
    {
        Monthly,
        Quarterly,
        Annually,
        Custom
    }

    public enum BudgetStatus
    {
        Draft,
        Active,
        Exceeded,
        Suspended,
        Expired
    }

    public class BudgetThreshold
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public decimal Percentage { get; set; } // e.g., 80 for 80%
        public ThresholdType Type { get; set; }
        public List<NotificationAction> Actions { get; set; } = new List<NotificationAction>();
        public bool IsTriggered { get; set; }
        public DateTime? LastTriggered { get; set; }
    }

    public enum ThresholdType
    {
        Warning,
        Critical,
        Emergency
    }

    public class NotificationAction
    {
        public ActionType Type { get; set; }
        public List<string> Recipients { get; set; } = new List<string>();
        public string Template { get; set; } = string.Empty;
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }

    public enum ActionType
    {
        Email,
        Webhook,
        AutoShutdown,
        RequireApproval,
        BlockProvisioning
    }

    /// <summary>
    /// Expense approval workflow
    /// </summary>
    public class ExpenseApproval
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public ExpenseCategory Category { get; set; }
        public string RequesterId { get; set; } = string.Empty;
        public string RequesterEmail { get; set; } = string.Empty;
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovalDate { get; set; }
        public List<ApprovalStep> ApprovalSteps { get; set; } = new List<ApprovalStep>();
        public string BusinessJustification { get; set; } = string.Empty;
        public List<string> Attachments { get; set; } = new List<string>();
        public BudgetScope Scope { get; set; } = new BudgetScope();
    }

    public enum ExpenseCategory
    {
        ComputeResources,
        Storage,
        Networking,
        Database,
        Analytics,
        Security,
        DevOps,
        Other
    }

    public enum ApprovalStatus
    {
        Pending,
        Approved,
        Rejected,
        Escalated,
        Expired
    }

    public class ApprovalStep
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ApproverEmail { get; set; } = string.Empty;
        public string ApproverRole { get; set; } = string.Empty;
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
        public DateTime? ResponseDate { get; set; }
        public string Comments { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsRequired { get; set; } = true;
    }

    /// <summary>
    /// Cost allocation and chargeback
    /// </summary>
    public class CostAllocation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public AllocationMethod Method { get; set; }
        public List<AllocationRule> Rules { get; set; } = new List<AllocationRule>();
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public Dictionary<string, decimal> Allocations { get; set; } = new Dictionary<string, decimal>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum AllocationMethod
    {
        EqualSplit,
        ProportionalByUsage,
        ProportionalByRevenue,
        FixedPercentage,
        Custom
    }

    public class AllocationRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TargetEntity { get; set; } = string.Empty; // Department, Project, etc.
        public decimal Percentage { get; set; }
        public AllocationCriteria Criteria { get; set; } = new AllocationCriteria();
        public int Priority { get; set; } = 1;
    }

    public class AllocationCriteria
    {
        public List<string> ResourceTypes { get; set; } = new List<string>();
        public List<string> Services { get; set; } = new List<string>();
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
        public List<string> ResourceGroups { get; set; } = new List<string>();
    }

    /// <summary>
    /// Department budget model
    /// </summary>
    public class Department
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string ManagerEmail { get; set; } = string.Empty;
        public decimal MonthlyBudget { get; set; }
        public decimal YearlyBudget { get; set; }
        public List<string> Projects { get; set; } = new List<string>();
        public List<CostCenter> CostCenters { get; set; } = new List<CostCenter>();
        public bool IsActive { get; set; } = true;
    }

    public class CostCenter
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public string DepartmentId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Budget utilization tracking
    /// </summary>
    public class BudgetUtilization
    {
        public string BudgetId { get; set; } = string.Empty;
        public decimal SpentAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
        public List<SpendingByCategory> CategoryBreakdown { get; set; } = new List<SpendingByCategory>();
        public TrendProjection Projection { get; set; } = new TrendProjection();
    }

    public class SpendingByCategory
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class TrendProjection
    {
        public decimal ProjectedTotal { get; set; }
        public decimal ProjectedOverrun { get; set; }
        public DateTime ProjectedExhaustionDate { get; set; }
        public double Confidence { get; set; }
    }
}