using PayFlowPro.Models.Entities;
using PayFlowPro.Models.DTOs.Leave;
using PayFlowPro.Shared.DTOs;

namespace PayFlowPro.Core.Interfaces
{
    public interface IAutoApprovalService
    {
        /// <summary>
        /// Evaluates all active auto-approval rules for a leave request
        /// </summary>
        /// <param name="leaveRequestId">The ID of the leave request to evaluate</param>
        /// <returns>Auto-approval result with evaluation details</returns>
        Task<AutoApprovalEvaluationResult> EvaluateLeaveRequestAsync(int leaveRequestId);
        
        /// <summary>
        /// Evaluates a specific auto-approval rule for a leave request
        /// </summary>
        /// <param name="ruleId">The ID of the rule to evaluate</param>
        /// <param name="leaveRequestId">The ID of the leave request to evaluate</param>
        /// <returns>Auto-approval result for the specific rule</returns>
        Task<AutoApprovalEvaluationResult> EvaluateRuleAsync(int ruleId, int leaveRequestId);
        
        /// <summary>
        /// Processes auto-approval for a leave request and updates its status
        /// </summary>
        /// <param name="leaveRequestId">The ID of the leave request to process</param>
        /// <returns>True if auto-approval was applied, false if manual approval required</returns>
        Task<bool> ProcessAutoApprovalAsync(int leaveRequestId);
        
        /// <summary>
        /// Gets all active auto-approval rules with optional filtering
        /// </summary>
        /// <param name="leaveTypeId">Optional leave type filter</param>
        /// <param name="employeeLevel">Optional employee level filter</param>
        /// <param name="departmentId">Optional department filter</param>
        /// <returns>List of applicable auto-approval rules</returns>
        Task<List<AutoApprovalRule>> GetApplicableRulesAsync(int? leaveTypeId = null, AutoApprovalEmployeeLevel? employeeLevel = null, int? departmentId = null);
        
        /// <summary>
        /// Validates and creates a new auto-approval rule
        /// </summary>
        /// <param name="rule">The rule to create</param>
        /// <returns>Created rule with ID assigned</returns>
        Task<AutoApprovalRule> CreateRuleAsync(AutoApprovalRule rule);
        
        /// <summary>
        /// Updates an existing auto-approval rule
        /// </summary>
        /// <param name="rule">The rule to update</param>
        /// <returns>Updated rule</returns>
        Task<AutoApprovalRule> UpdateRuleAsync(AutoApprovalRule rule);
        
        /// <summary>
        /// Deletes an auto-approval rule
        /// </summary>
        /// <param name="ruleId">The ID of the rule to delete</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteRuleAsync(int ruleId);
        
        /// <summary>
        /// Gets auto-approval logs for a specific leave request
        /// </summary>
        /// <param name="leaveRequestId">The ID of the leave request</param>
        /// <returns>List of approval logs for the request</returns>
        Task<List<AutoApprovalRuleLog>> GetApprovalLogsAsync(int leaveRequestId);
        
        /// <summary>
        /// Tests a rule against sample data without saving results
        /// </summary>
        /// <param name="rule">The rule to test</param>
        /// <param name="testScenario">Test scenario parameters</param>
        /// <returns>Test evaluation result</returns>
        Task<AutoApprovalEvaluationResult> TestRuleAsync(AutoApprovalRule rule, RuleTestScenario testScenario);
        
        // CRUD Operations for Auto-Approval Rules Management
        
        /// <summary>
        /// Gets all auto-approval rules with pagination and filtering
        /// </summary>
        /// <returns>List of auto-approval rules</returns>
        Task<ServiceResponse<List<AutoApprovalRule>>> GetAutoApprovalRulesAsync();
        
        /// <summary>
        /// Gets a single auto-approval rule by ID
        /// </summary>
        /// <param name="ruleId">The ID of the rule to retrieve</param>
        /// <returns>The auto-approval rule</returns>
        Task<ServiceResponse<AutoApprovalRule>> GetAutoApprovalRuleAsync(int ruleId);
        
        /// <summary>
        /// Creates a new auto-approval rule
        /// </summary>
        /// <param name="createRuleDto">The rule data to create</param>
        /// <returns>Created auto-approval rule</returns>
        Task<ServiceResponse<AutoApprovalRule>> CreateAutoApprovalRuleAsync(CreateAutoApprovalRuleDto createRuleDto);
        
        /// <summary>
        /// Updates an existing auto-approval rule
        /// </summary>
        /// <param name="ruleId">The ID of the rule to update</param>
        /// <param name="updateRuleDto">The updated rule data</param>
        /// <returns>Updated auto-approval rule</returns>
        Task<ServiceResponse<AutoApprovalRule>> UpdateAutoApprovalRuleAsync(int ruleId, CreateAutoApprovalRuleDto updateRuleDto);
        
        /// <summary>
        /// Deletes an auto-approval rule
        /// </summary>
        /// <param name="ruleId">The ID of the rule to delete</param>
        /// <returns>Success indicator</returns>
        Task<ServiceResponse<bool>> DeleteAutoApprovalRuleAsync(int ruleId);
        
        /// <summary>
        /// Toggles the active status of an auto-approval rule
        /// </summary>
        /// <param name="ruleId">The ID of the rule to toggle</param>
        /// <returns>Success indicator</returns>
        Task<ServiceResponse<bool>> ToggleAutoApprovalRuleAsync(int ruleId);
    }
    
    /// <summary>
    /// Result of auto-approval rule evaluation
    /// </summary>
    public class AutoApprovalEvaluationResult
    {
        public AutoApprovalResult Result { get; set; }
        public string ResultReason { get; set; } = string.Empty;
        public List<string> EvaluationDetails { get; set; } = new();
        public int? AppliedRuleId { get; set; }
        public string? AppliedRuleName { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public bool IsAutoApproved => Result == AutoApprovalResult.Approved;
        public bool RequiresManualReview => Result == AutoApprovalResult.RequiresManualApproval;
        public bool IsApproved => IsAutoApproved;
        public string ApprovalComments => IsAutoApproved ? $"Auto-approved by rule: {AppliedRuleName}" : ResultReason;
    }
    
    /// <summary>
    /// Test scenario for rule validation
    /// </summary>
    public class RuleTestScenario
    {
        public int EmployeeId { get; set; }
        public int LeaveTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal DaysRequested { get; set; }
        public int MinutesNotice { get; set; }
        public decimal CurrentLeaveBalance { get; set; }
        public bool HasSupportingDocuments { get; set; }
        public bool IsHalfDay { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}