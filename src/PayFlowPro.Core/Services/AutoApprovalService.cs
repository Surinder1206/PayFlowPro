using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PayFlowPro.Core.Interfaces;
using PayFlowPro.Data.Context;
using PayFlowPro.Models.Entities;
using PayFlowPro.Models.Enums;
using PayFlowPro.Models.DTOs.Leave;
using PayFlowPro.Shared.DTOs;
using System.Diagnostics;
using System.Text.Json;

namespace PayFlowPro.Core.Services
{
    public class AutoApprovalService : IAutoApprovalService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AutoApprovalService> _logger;

        public AutoApprovalService(ApplicationDbContext context, ILogger<AutoApprovalService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AutoApprovalEvaluationResult> EvaluateLeaveRequestAsync(int leaveRequestId)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var leaveRequest = await _context.LeaveRequests
                    .Include(lr => lr.Employee)
                    .ThenInclude(e => e.Department)
                    .Include(lr => lr.LeaveType)
                    .FirstOrDefaultAsync(lr => lr.Id == leaveRequestId);

                if (leaveRequest == null)
                {
                    return new AutoApprovalEvaluationResult
                    {
                        Result = AutoApprovalResult.Error,
                        ResultReason = "Leave request not found",
                        ProcessingTime = stopwatch.Elapsed
                    };
                }

                // Get applicable rules sorted by priority
                var applicableRules = await GetApplicableRulesAsync(
                    leaveRequest.LeaveTypeId, 
                    null, // Employee level not implemented yet
                    leaveRequest.Employee.DepartmentId);

                if (!applicableRules.Any())
                {
                    return new AutoApprovalEvaluationResult
                    {
                        Result = AutoApprovalResult.RequiresManualApproval,
                        ResultReason = "No applicable auto-approval rules found",
                        ProcessingTime = stopwatch.Elapsed
                    };
                }

                // Evaluate rules in priority order
                foreach (var rule in applicableRules.OrderBy(r => r.Priority))
                {
                    var ruleResult = await EvaluateRuleAgainstRequest(rule, leaveRequest);
                    
                    if (ruleResult.Result == AutoApprovalResult.Approved)
                    {
                        // Log the auto-approval if enabled
                        if (rule.LogAutoApprovalAction)
                        {
                            await LogAutoApprovalActionAsync(rule.Id, leaveRequestId, ruleResult);
                        }
                        
                        ruleResult.AppliedRuleId = rule.Id;
                        ruleResult.AppliedRuleName = rule.RuleName;
                        ruleResult.ProcessingTime = stopwatch.Elapsed;
                        return ruleResult;
                    }
                    
                    if (ruleResult.Result == AutoApprovalResult.Rejected)
                    {
                        // Log the rejection if enabled
                        if (rule.LogAutoApprovalAction)
                        {
                            await LogAutoApprovalActionAsync(rule.Id, leaveRequestId, ruleResult);
                        }
                        
                        ruleResult.AppliedRuleId = rule.Id;
                        ruleResult.AppliedRuleName = rule.RuleName;
                        ruleResult.ProcessingTime = stopwatch.Elapsed;
                        return ruleResult;
                    }
                }

                return new AutoApprovalEvaluationResult
                {
                    Result = AutoApprovalResult.RequiresManualApproval,
                    ResultReason = "No rules resulted in auto-approval",
                    ProcessingTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating leave request {LeaveRequestId} for auto-approval", leaveRequestId);
                return new AutoApprovalEvaluationResult
                {
                    Result = AutoApprovalResult.Error,
                    ResultReason = $"Error during evaluation: {ex.Message}",
                    ProcessingTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<AutoApprovalEvaluationResult> EvaluateRuleAsync(int ruleId, int leaveRequestId)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var rule = await _context.AutoApprovalRules.FirstOrDefaultAsync(r => r.Id == ruleId);
                var leaveRequest = await _context.LeaveRequests
                    .Include(lr => lr.Employee)
                    .ThenInclude(e => e.Department)
                    .Include(lr => lr.LeaveType)
                    .FirstOrDefaultAsync(lr => lr.Id == leaveRequestId);

                if (rule == null)
                {
                    return new AutoApprovalEvaluationResult
                    {
                        Result = AutoApprovalResult.Error,
                        ResultReason = "Auto-approval rule not found",
                        ProcessingTime = stopwatch.Elapsed
                    };
                }

                if (leaveRequest == null)
                {
                    return new AutoApprovalEvaluationResult
                    {
                        Result = AutoApprovalResult.Error,
                        ResultReason = "Leave request not found",
                        ProcessingTime = stopwatch.Elapsed
                    };
                }

                var result = await EvaluateRuleAgainstRequest(rule, leaveRequest);
                result.ProcessingTime = stopwatch.Elapsed;
                result.AppliedRuleId = ruleId;
                result.AppliedRuleName = rule.RuleName;
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating rule {RuleId} against leave request {LeaveRequestId}", ruleId, leaveRequestId);
                return new AutoApprovalEvaluationResult
                {
                    Result = AutoApprovalResult.Error,
                    ResultReason = $"Error during rule evaluation: {ex.Message}",
                    ProcessingTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<bool> ProcessAutoApprovalAsync(int leaveRequestId)
        {
            var evaluationResult = await EvaluateLeaveRequestAsync(leaveRequestId);
            
            if (evaluationResult.IsAutoApproved)
            {
                var leaveRequest = await _context.LeaveRequests.FirstOrDefaultAsync(lr => lr.Id == leaveRequestId);
                if (leaveRequest != null)
                {
                    leaveRequest.Status = LeaveRequestStatus.Approved;
                    leaveRequest.UpdatedAt = DateTime.UtcNow;
                    
                    // Create approval record
                    var approval = new LeaveRequestApproval
                    {
                        LeaveRequestId = leaveRequestId,
                        ApprovalLevel = 1,
                        ApproverId = "system", // Auto-approval system user
                        ApproverName = "System Auto-Approval",
                        ApproverEmail = "system@autoapproval.local",
                        Status = ApprovalStatus.Approved,
                        Comments = $"Auto-approved by rule: {evaluationResult.AppliedRuleName}",
                        ApprovedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    _context.LeaveRequestApprovals.Add(approval);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Leave request {LeaveRequestId} was auto-approved by rule {RuleId}", 
                        leaveRequestId, evaluationResult.AppliedRuleId);
                    
                    return true;
                }
            }
            
            return false;
        }

        public async Task<List<AutoApprovalRule>> GetApplicableRulesAsync(int? leaveTypeId = null, AutoApprovalEmployeeLevel? employeeLevel = null, int? departmentId = null)
        {
            var query = _context.AutoApprovalRules.Where(r => r.IsActive);

            if (leaveTypeId.HasValue)
            {
                query = query.Where(r => 
                    r.ApplicableLeaveTypeIds == null || 
                    r.ApplicableLeaveTypeIds.Contains(leaveTypeId.Value.ToString()));
            }

            if (employeeLevel.HasValue)
            {
                query = query.Where(r => 
                    r.ApplicableEmployeeLevel == null || 
                    r.ApplicableEmployeeLevel == employeeLevel.Value);
            }

            if (departmentId.HasValue)
            {
                query = query.Where(r => 
                    r.ApplicableDepartmentIds == null || 
                    r.ApplicableDepartmentIds.Contains(departmentId.Value.ToString()));
            }

            return await query.OrderBy(r => r.Priority).ToListAsync();
        }

        public async Task<AutoApprovalRule> CreateRuleAsync(AutoApprovalRule rule)
        {
            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;
            
            _context.AutoApprovalRules.Add(rule);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created auto-approval rule {RuleId}: {RuleName}", rule.Id, rule.RuleName);
            return rule;
        }

        public async Task<AutoApprovalRule> UpdateRuleAsync(AutoApprovalRule rule)
        {
            var existingRule = await _context.AutoApprovalRules.FirstOrDefaultAsync(r => r.Id == rule.Id);
            if (existingRule == null)
            {
                throw new ArgumentException("Auto-approval rule not found", nameof(rule));
            }

            // Update properties
            existingRule.RuleName = rule.RuleName;
            existingRule.Description = rule.Description;
            existingRule.Priority = rule.Priority;
            existingRule.IsActive = rule.IsActive;
            existingRule.MaxDaysAllowed = rule.MaxDaysAllowed;
            existingRule.MinNoticeRequiredDays = rule.MinNoticeRequiredDays;
            existingRule.MaxConsecutiveDays = rule.MaxConsecutiveDays;
            existingRule.MinBalanceRequired = rule.MinBalanceRequired;
            existingRule.ApplicableLeaveTypeIds = rule.ApplicableLeaveTypeIds;
            existingRule.ApplicableEmployeeLevel = rule.ApplicableEmployeeLevel;
            existingRule.ApplicableDepartmentIds = rule.ApplicableDepartmentIds;
            existingRule.AllowedDaysOfWeek = rule.AllowedDaysOfWeek;
            existingRule.AllowedMonths = rule.AllowedMonths;
            existingRule.BlackoutStartDate = rule.BlackoutStartDate;
            existingRule.BlackoutEndDate = rule.BlackoutEndDate;
            existingRule.CheckTeamConflicts = rule.CheckTeamConflicts;
            existingRule.MaxTeamLeavePercentage = rule.MaxTeamLeavePercentage;
            existingRule.RequireSupportingDocuments = rule.RequireSupportingDocuments;
            existingRule.AllowHalfDayAutoApproval = rule.AllowHalfDayAutoApproval;
            existingRule.EmergencyBypassRule = rule.EmergencyBypassRule;
            existingRule.SendNotificationOnApproval = rule.SendNotificationOnApproval;
            existingRule.LogAutoApprovalAction = rule.LogAutoApprovalAction;
            existingRule.CustomConditions = rule.CustomConditions;
            existingRule.UpdatedAt = DateTime.UtcNow;
            existingRule.UpdatedBy = rule.UpdatedBy;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Updated auto-approval rule {RuleId}: {RuleName}", rule.Id, rule.RuleName);
            return existingRule;
        }

        public async Task<bool> DeleteRuleAsync(int ruleId)
        {
            var rule = await _context.AutoApprovalRules.FirstOrDefaultAsync(r => r.Id == ruleId);
            if (rule == null)
            {
                return false;
            }

            _context.AutoApprovalRules.Remove(rule);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Deleted auto-approval rule {RuleId}: {RuleName}", ruleId, rule.RuleName);
            return true;
        }

        public async Task<List<AutoApprovalRuleLog>> GetApprovalLogsAsync(int leaveRequestId)
        {
            return await _context.AutoApprovalRuleLogs
                .Include(arl => arl.AutoApprovalRule)
                .Include(arl => arl.Employee)
                .Where(arl => arl.LeaveRequestId == leaveRequestId)
                .OrderBy(arl => arl.ProcessedAt)
                .ToListAsync();
        }

        public async Task<AutoApprovalEvaluationResult> TestRuleAsync(AutoApprovalRule rule, RuleTestScenario testScenario)
        {
            // Create a temporary leave request for testing
            var testEmployee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == testScenario.EmployeeId);

            if (testEmployee == null)
            {
                return new AutoApprovalEvaluationResult
                {
                    Result = AutoApprovalResult.Error,
                    ResultReason = "Test employee not found"
                };
            }

            var testLeaveRequest = new LeaveRequest
            {
                EmployeeId = testScenario.EmployeeId,
                LeaveTypeId = testScenario.LeaveTypeId,
                StartDate = testScenario.StartDate,
                EndDate = testScenario.EndDate,
                DaysRequested = testScenario.DaysRequested,
                Reason = testScenario.Reason,
                HasSupportingDocuments = testScenario.HasSupportingDocuments,
                CreatedAt = DateTime.UtcNow.AddMinutes(-testScenario.MinutesNotice),
                Employee = testEmployee
            };

            return await EvaluateRuleAgainstRequest(rule, testLeaveRequest);
        }

        private async Task<AutoApprovalEvaluationResult> EvaluateRuleAgainstRequest(AutoApprovalRule rule, LeaveRequest leaveRequest)
        {
            var evaluationDetails = new List<string>();
            var result = new AutoApprovalEvaluationResult { EvaluationDetails = evaluationDetails };

            try
            {
                // Check if rule is active
                if (!rule.IsActive)
                {
                    result.Result = AutoApprovalResult.RuleNotApplicable;
                    result.ResultReason = "Rule is not active";
                    evaluationDetails.Add("Rule is inactive");
                    return result;
                }

                // Check maximum days allowed
                if (rule.MaxDaysAllowed.HasValue && leaveRequest.DaysRequested > rule.MaxDaysAllowed.Value)
                {
                    result.Result = AutoApprovalResult.RequiresManualApproval;
                    result.ResultReason = $"Requested days ({leaveRequest.DaysRequested}) exceeds maximum allowed ({rule.MaxDaysAllowed})";
                    evaluationDetails.Add(result.ResultReason);
                    return result;
                }
                evaluationDetails.Add($"Days requested ({leaveRequest.DaysRequested}) within limit ({rule.MaxDaysAllowed?.ToString() ?? "no limit"})");

                // Check minimum notice period
                if (rule.MinNoticeRequiredDays.HasValue)
                {
                    var noticeGiven = (leaveRequest.StartDate - leaveRequest.CreatedAt).Days;
                    if (noticeGiven < rule.MinNoticeRequiredDays.Value)
                    {
                        result.Result = AutoApprovalResult.RequiresManualApproval;
                        result.ResultReason = $"Insufficient notice: {noticeGiven} days, minimum required: {rule.MinNoticeRequiredDays}";
                        evaluationDetails.Add(result.ResultReason);
                        return result;
                    }
                    evaluationDetails.Add($"Notice period sufficient: {noticeGiven} days (minimum: {rule.MinNoticeRequiredDays})");
                }

                // Check maximum consecutive days
                if (rule.MaxConsecutiveDays.HasValue && leaveRequest.DaysRequested > rule.MaxConsecutiveDays.Value)
                {
                    result.Result = AutoApprovalResult.RequiresManualApproval;
                    result.ResultReason = $"Consecutive days ({leaveRequest.DaysRequested}) exceeds maximum ({rule.MaxConsecutiveDays})";
                    evaluationDetails.Add(result.ResultReason);
                    return result;
                }

                // Check employee level (currently not implemented in Employee entity)
                // TODO: Implement employee level property in Employee entity
                // if (rule.ApplicableEmployeeLevel.HasValue) 
                // {
                //     // Employee level checking logic would go here
                // }

                // Check department
                if (!string.IsNullOrEmpty(rule.ApplicableDepartmentIds))
                {
                    var applicableDepts = rule.ApplicableDepartmentIds.Split(',').Select(int.Parse);
                    if (!applicableDepts.Contains(leaveRequest.Employee.DepartmentId))
                    {
                        result.Result = AutoApprovalResult.RuleNotApplicable;
                        result.ResultReason = "Employee department not applicable for this rule";
                        evaluationDetails.Add($"Employee department {leaveRequest.Employee.DepartmentId} not in applicable list: {rule.ApplicableDepartmentIds}");
                        return result;
                    }
                }

                // Check allowed days of week
                if (!string.IsNullOrEmpty(rule.AllowedDaysOfWeek))
                {
                    var allowedDays = rule.AllowedDaysOfWeek.Split(',').Select(d => (DayOfWeek)int.Parse(d));
                    var requestDays = GetDaysOfWeekInRange(leaveRequest.StartDate, leaveRequest.EndDate);
                    
                    if (!requestDays.All(day => allowedDays.Contains(day)))
                    {
                        result.Result = AutoApprovalResult.RequiresManualApproval;
                        result.ResultReason = "Leave includes restricted days of the week";
                        evaluationDetails.Add($"Request includes disallowed days. Allowed: {string.Join(",", allowedDays)}, Requested: {string.Join(",", requestDays)}");
                        return result;
                    }
                }

                // Check allowed months
                if (!string.IsNullOrEmpty(rule.AllowedMonths))
                {
                    var allowedMonths = rule.AllowedMonths.Split(',').Select(int.Parse);
                    if (!allowedMonths.Contains(leaveRequest.StartDate.Month) || 
                        !allowedMonths.Contains(leaveRequest.EndDate.Month))
                    {
                        result.Result = AutoApprovalResult.RequiresManualApproval;
                        result.ResultReason = "Leave includes restricted months";
                        evaluationDetails.Add($"Request in disallowed months. Allowed: {rule.AllowedMonths}, Requested: {leaveRequest.StartDate.Month}-{leaveRequest.EndDate.Month}");
                        return result;
                    }
                }

                // Check blackout periods
                if (rule.BlackoutStartDate.HasValue && rule.BlackoutEndDate.HasValue)
                {
                    if (DoesOverlap(leaveRequest.StartDate, leaveRequest.EndDate, 
                                   rule.BlackoutStartDate.Value, rule.BlackoutEndDate.Value))
                    {
                        result.Result = AutoApprovalResult.RequiresManualApproval;
                        result.ResultReason = "Leave overlaps with blackout period";
                        evaluationDetails.Add($"Request overlaps blackout period: {rule.BlackoutStartDate} to {rule.BlackoutEndDate}");
                        return result;
                    }
                }

                // Check supporting documents requirement
                if (rule.RequireSupportingDocuments && !leaveRequest.HasSupportingDocuments)
                {
                    result.Result = AutoApprovalResult.RequiresManualApproval;
                    result.ResultReason = "Supporting documents required but not provided";
                    evaluationDetails.Add("Missing required supporting documents");
                    return result;
                }

                // Check team conflicts if enabled
                if (rule.CheckTeamConflicts && rule.MaxTeamLeavePercentage.HasValue)
                {
                    var teamConflict = await CheckTeamConflictsAsync(leaveRequest, rule.MaxTeamLeavePercentage.Value);
                    if (teamConflict.HasConflict)
                    {
                        result.Result = AutoApprovalResult.RequiresManualApproval;
                        result.ResultReason = $"Team leave percentage would exceed limit: {teamConflict.Percentage}% > {rule.MaxTeamLeavePercentage}%";
                        evaluationDetails.Add(result.ResultReason);
                        return result;
                    }
                }

                // Evaluate custom conditions if any
                if (!string.IsNullOrEmpty(rule.CustomConditions))
                {
                    var customResult = await EvaluateCustomConditionsAsync(rule.CustomConditions, leaveRequest);
                    if (!customResult.Success)
                    {
                        result.Result = AutoApprovalResult.RequiresManualApproval;
                        result.ResultReason = customResult.FailureReason;
                        evaluationDetails.Add($"Custom condition failed: {customResult.FailureReason}");
                        return result;
                    }
                }

                // All checks passed - approve
                result.Result = AutoApprovalResult.Approved;
                result.ResultReason = "All auto-approval conditions satisfied";
                evaluationDetails.Add("All rule conditions passed - approved for auto-approval");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating rule {RuleId} against leave request {LeaveRequestId}", 
                    rule.Id, leaveRequest.Id);
                
                result.Result = AutoApprovalResult.Error;
                result.ResultReason = $"Evaluation error: {ex.Message}";
                evaluationDetails.Add($"Error during evaluation: {ex.Message}");
                return result;
            }
        }

        private async Task LogAutoApprovalActionAsync(int ruleId, int leaveRequestId, AutoApprovalEvaluationResult result)
        {
            var leaveRequest = await _context.LeaveRequests.FirstOrDefaultAsync(lr => lr.Id == leaveRequestId);
            if (leaveRequest == null) return;

            var log = new AutoApprovalRuleLog
            {
                AutoApprovalRuleId = ruleId,
                LeaveRequestId = leaveRequestId,
                EmployeeId = leaveRequest.EmployeeId,
                Result = result.Result,
                ResultReason = result.ResultReason,
                EvaluationDetails = JsonSerializer.Serialize(result.EvaluationDetails),
                ProcessedAt = DateTime.UtcNow,
                ProcessingTimeMs = result.ProcessingTime.TotalMilliseconds.ToString("F2")
            };

            _context.AutoApprovalRuleLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        private List<DayOfWeek> GetDaysOfWeekInRange(DateTime startDate, DateTime endDate)
        {
            var days = new List<DayOfWeek>();
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (!days.Contains(date.DayOfWeek))
                {
                    days.Add(date.DayOfWeek);
                }
            }
            return days;
        }

        private bool DoesOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        {
            return start1 <= end2 && end1 >= start2;
        }

        private async Task<(bool HasConflict, decimal Percentage)> CheckTeamConflictsAsync(LeaveRequest leaveRequest, decimal maxPercentage)
        {
            // Get all team members in the same department
            var teamSize = await _context.Employees
                .Where(e => e.DepartmentId == leaveRequest.Employee.DepartmentId && e.Status == EmploymentStatus.Active)
                .CountAsync();

            if (teamSize <= 1)
            {
                return (false, 0);
            }

            // Count team members who will be on leave during the requested period
            var conflictingLeaves = await _context.LeaveRequests
                .Where(lr => lr.Employee.DepartmentId == leaveRequest.Employee.DepartmentId &&
                            lr.Status == LeaveRequestStatus.Approved &&
                            lr.Id != leaveRequest.Id &&
                            DoesOverlap(lr.StartDate, lr.EndDate, leaveRequest.StartDate, leaveRequest.EndDate))
                .CountAsync();

            var leavePercentage = ((decimal)(conflictingLeaves + 1) / teamSize) * 100;
            return (leavePercentage > maxPercentage, leavePercentage);
        }

        private async Task<(bool Success, string FailureReason)> EvaluateCustomConditionsAsync(string customConditions, LeaveRequest leaveRequest)
        {
            try
            {
                // This is a placeholder for custom condition evaluation
                // In a real implementation, you might use a rules engine or scripting engine
                // For now, we'll assume custom conditions are in JSON format with simple key-value pairs
                
                var conditions = JsonSerializer.Deserialize<Dictionary<string, object>>(customConditions);
                
                foreach (var condition in conditions)
                {
                    // Example custom condition evaluation
                    // This would be expanded based on business requirements
                    switch (condition.Key.ToLower())
                    {
                        case "min_tenure_months":
                            // Using CreatedAt as a proxy for hire date since HireDate property doesn't exist
                            var tenureMonths = (DateTime.Now - leaveRequest.Employee.CreatedAt).Days / 30;
                            var requiredTenure = Convert.ToInt32(condition.Value);
                            if (tenureMonths < requiredTenure)
                            {
                                return (false, $"Employee tenure ({tenureMonths} months) below required ({requiredTenure} months)");
                            }
                            break;
                            
                        case "max_requests_per_month":
                            var requestsThisMonth = await _context.LeaveRequests
                                .Where(lr => lr.EmployeeId == leaveRequest.EmployeeId &&
                                           lr.CreatedAt.Year == DateTime.Now.Year &&
                                           lr.CreatedAt.Month == DateTime.Now.Month &&
                                           lr.Status != LeaveRequestStatus.Cancelled)
                                .CountAsync();
                            
                            var maxRequests = Convert.ToInt32(condition.Value);
                            if (requestsThisMonth >= maxRequests)
                            {
                                return (false, $"Maximum requests per month ({maxRequests}) already reached");
                            }
                            break;
                    }
                }
                
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, $"Custom condition evaluation error: {ex.Message}");
            }
        }

        // CRUD Operations for Auto-Approval Rules Management

        public async Task<ServiceResponse<List<AutoApprovalRule>>> GetAutoApprovalRulesAsync()
        {
            try
            {
                var rules = await _context.AutoApprovalRules
                    .OrderByDescending(r => r.Priority)
                    .ThenBy(r => r.RuleName)
                    .ToListAsync();

                return ServiceResponse<List<AutoApprovalRule>>.Success(rules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving auto-approval rules");
                return ServiceResponse<List<AutoApprovalRule>>.Failure($"Error retrieving auto-approval rules: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<AutoApprovalRule>> GetAutoApprovalRuleAsync(int ruleId)
        {
            try
            {
                var rule = await _context.AutoApprovalRules
                    .FirstOrDefaultAsync(r => r.Id == ruleId);

                if (rule == null)
                {
                    return ServiceResponse<AutoApprovalRule>.Failure("Auto-approval rule not found");
                }

                return ServiceResponse<AutoApprovalRule>.Success(rule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving auto-approval rule {RuleId}", ruleId);
                return ServiceResponse<AutoApprovalRule>.Failure($"Error retrieving auto-approval rule: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<AutoApprovalRule>> CreateAutoApprovalRuleAsync(CreateAutoApprovalRuleDto createRuleDto)
        {
            try
            {
                // Validate rule name uniqueness
                var existingRule = await _context.AutoApprovalRules
                    .FirstOrDefaultAsync(r => r.RuleName == createRuleDto.RuleName);

                if (existingRule != null)
                {
                    return ServiceResponse<AutoApprovalRule>.Failure("A rule with this name already exists");
                }

                var rule = new AutoApprovalRule
                {
                    RuleName = createRuleDto.RuleName,
                    Description = createRuleDto.Description ?? string.Empty,
                    ApplicableDepartmentIds = createRuleDto.DepartmentId?.ToString(),
                    ApplicableLeaveTypeIds = createRuleDto.LeaveTypeId?.ToString(),
                    ApplicableEmployeeLevel = !string.IsNullOrEmpty(createRuleDto.EmployeeLevel) ? 
                        Enum.TryParse<AutoApprovalEmployeeLevel>(createRuleDto.EmployeeLevel, out var level) ? level : AutoApprovalEmployeeLevel.All : 
                        AutoApprovalEmployeeLevel.All,
                    MaxDaysAllowed = createRuleDto.MaxDaysAllowed,
                    MinNoticeRequiredDays = createRuleDto.MinNoticeRequiredDays,
                    IsActive = createRuleDto.IsActive,
                    Priority = createRuleDto.Priority,
                    CustomConditions = createRuleDto.CustomConditionsJson,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    UpdatedBy = "System"
                };

                _context.AutoApprovalRules.Add(rule);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created auto-approval rule {RuleName} with ID {RuleId}", rule.RuleName, rule.Id);
                return ServiceResponse<AutoApprovalRule>.Success(rule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating auto-approval rule {RuleName}", createRuleDto.RuleName);
                return ServiceResponse<AutoApprovalRule>.Failure($"Error creating auto-approval rule: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<AutoApprovalRule>> UpdateAutoApprovalRuleAsync(int ruleId, CreateAutoApprovalRuleDto updateRuleDto)
        {
            try
            {
                var rule = await _context.AutoApprovalRules
                    .FirstOrDefaultAsync(r => r.Id == ruleId);

                if (rule == null)
                {
                    return ServiceResponse<AutoApprovalRule>.Failure("Auto-approval rule not found");
                }

                // Validate rule name uniqueness (excluding current rule)
                var existingRule = await _context.AutoApprovalRules
                    .FirstOrDefaultAsync(r => r.RuleName == updateRuleDto.RuleName && r.Id != ruleId);

                if (existingRule != null)
                {
                    return ServiceResponse<AutoApprovalRule>.Failure("A rule with this name already exists");
                }

                // Update rule properties
                rule.RuleName = updateRuleDto.RuleName;
                rule.Description = updateRuleDto.Description ?? string.Empty;
                rule.ApplicableDepartmentIds = updateRuleDto.DepartmentId?.ToString();
                rule.ApplicableLeaveTypeIds = updateRuleDto.LeaveTypeId?.ToString();
                rule.ApplicableEmployeeLevel = !string.IsNullOrEmpty(updateRuleDto.EmployeeLevel) ? 
                    Enum.TryParse<AutoApprovalEmployeeLevel>(updateRuleDto.EmployeeLevel, out var level) ? level : AutoApprovalEmployeeLevel.All : 
                    AutoApprovalEmployeeLevel.All;
                rule.MaxDaysAllowed = updateRuleDto.MaxDaysAllowed;
                rule.MinNoticeRequiredDays = updateRuleDto.MinNoticeRequiredDays;
                rule.IsActive = updateRuleDto.IsActive;
                rule.Priority = updateRuleDto.Priority;
                rule.CustomConditions = updateRuleDto.CustomConditionsJson;
                rule.UpdatedAt = DateTime.UtcNow;
                rule.UpdatedBy = "System";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated auto-approval rule {RuleName} with ID {RuleId}", rule.RuleName, rule.Id);
                return ServiceResponse<AutoApprovalRule>.Success(rule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating auto-approval rule {RuleId}", ruleId);
                return ServiceResponse<AutoApprovalRule>.Failure($"Error updating auto-approval rule: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<bool>> DeleteAutoApprovalRuleAsync(int ruleId)
        {
            try
            {
                var rule = await _context.AutoApprovalRules
                    .FirstOrDefaultAsync(r => r.Id == ruleId);

                if (rule == null)
                {
                    return ServiceResponse<bool>.Failure("Auto-approval rule not found");
                }

                // Check if rule has any associated logs
                var hasLogs = await _context.AutoApprovalRuleLogs
                    .AnyAsync(l => l.AutoApprovalRuleId == ruleId);

                if (hasLogs)
                {
                    // Soft delete by deactivating instead of hard delete to preserve audit trail
                    rule.IsActive = false;
                    rule.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Deactivated auto-approval rule {RuleName} with ID {RuleId} (had associated logs)", rule.RuleName, rule.Id);
                }
                else
                {
                    // Hard delete if no logs exist
                    _context.AutoApprovalRules.Remove(rule);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Deleted auto-approval rule {RuleName} with ID {RuleId}", rule.RuleName, rule.Id);
                }

                return ServiceResponse<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting auto-approval rule {RuleId}", ruleId);
                return ServiceResponse<bool>.Failure($"Error deleting auto-approval rule: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<bool>> ToggleAutoApprovalRuleAsync(int ruleId)
        {
            try
            {
                var rule = await _context.AutoApprovalRules
                    .FirstOrDefaultAsync(r => r.Id == ruleId);

                if (rule == null)
                {
                    return ServiceResponse<bool>.Failure("Auto-approval rule not found");
                }

                rule.IsActive = !rule.IsActive;
                rule.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Toggled auto-approval rule {RuleName} to {Status}", rule.RuleName, rule.IsActive ? "Active" : "Inactive");
                return ServiceResponse<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling auto-approval rule {RuleId}", ruleId);
                return ServiceResponse<bool>.Failure($"Error toggling auto-approval rule: {ex.Message}");
            }
        }
    }
}