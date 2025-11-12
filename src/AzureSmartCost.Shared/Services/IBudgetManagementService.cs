using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureSmartCost.Shared.Models.Budget;

namespace AzureSmartCost.Shared.Services
{
    /// <summary>
    /// Service interface for budget management and cost allocation
    /// </summary>
    public interface IBudgetManagementService
    {
        // Budget Management
        Task<Budget> CreateBudgetAsync(Budget budget);
        Task<Budget> UpdateBudgetAsync(Budget budget);
        Task<bool> DeleteBudgetAsync(string budgetId);
        Task<List<Budget>> GetBudgetsAsync(string subscriptionId);
        Task<BudgetUtilization> GetBudgetUtilizationAsync(string budgetId);

        // Expense Approval
        Task<ExpenseApproval> CreateExpenseApprovalAsync(ExpenseApproval approval);
        Task<ExpenseApproval> ApproveExpenseAsync(string approvalId, string approverId, string comments);
        Task<ExpenseApproval> RejectExpenseAsync(string approvalId, string approverId, string comments);
        Task<List<ExpenseApproval>> GetPendingApprovalsAsync(string approverId);

        // Cost Allocation
        Task<CostAllocation> CreateCostAllocationAsync(CostAllocation allocation);
        Task<Dictionary<string, decimal>> CalculateAllocationsAsync(string allocationId, DateTime startDate, DateTime endDate);
        Task<List<CostAllocation>> GetActiveAllocationsAsync();

        // Department Management
        Task<Department> CreateDepartmentAsync(Department department);
        Task<List<Department>> GetDepartmentsAsync();
        Task<BudgetUtilization> GetDepartmentUtilizationAsync(string departmentId);
    }
}