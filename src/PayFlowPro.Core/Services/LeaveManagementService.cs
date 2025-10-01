using Microsoft.EntityFrameworkCore;
using PayFlowPro.Data.Context;
using PayFlowPro.Models.DTOs.Leave;
using PayFlowPro.Models.Entities;
using PayFlowPro.Models.Enums;
using PayFlowPro.Shared.DTOs;
using PayFlowPro.Core.Interfaces;

namespace PayFlowPro.Core.Services
{
    public interface ILeaveManagementService
    {
        // Leave Request Operations
        Task<ServiceResponse<LeaveRequestDto>> CreateLeaveRequestAsync(CreateLeaveRequestDto request);
        Task<ServiceResponse<LeaveRequestDto>> UpdateLeaveRequestAsync(UpdateLeaveRequestDto request);
        Task<ServiceResponse<bool>> CancelLeaveRequestAsync(int leaveRequestId, string reason);
        Task<ServiceResponse<bool>> SubmitLeaveRequestAsync(int leaveRequestId);
        Task<ServiceResponse<LeaveRequestDto>> GetLeaveRequestByIdAsync(int id);
        Task<ServiceResponse<List<LeaveRequestDto>>> GetEmployeeLeaveRequestsAsync(int employeeId, int? financialYear = null);
        Task<ServiceResponse<List<LeaveRequestDto>>> GetPendingLeaveRequestsAsync();
        Task<ServiceResponse<List<LeaveRequestDto>>> GetPendingApprovalsAsync(string approverId);
        
        // Leave Approval Operations
        Task<ServiceResponse<bool>> ApproveLeaveRequestAsync(int leaveRequestId, int approverId, string comments);
        Task<ServiceResponse<bool>> RejectLeaveRequestAsync(int leaveRequestId, int approverId, string comments);
        Task<ServiceResponse<bool>> DelegateApprovalAsync(int leaveRequestId, string fromApproverId, string toApproverId);
        
        // Leave Balance Operations
        Task<ServiceResponse<LeaveSummaryDto>> GetEmployeeLeaveSummaryAsync(int employeeId, int? financialYear = null);
        Task<ServiceResponse<List<LeaveTypeBalanceDto>>> GetEmployeeLeaveBalancesAsync(int employeeId, int? financialYear = null);
        Task<ServiceResponse<bool>> UpdateLeaveBalanceAsync(UpdateLeaveBalanceDto balance);
        Task<ServiceResponse<bool>> InitializeLeaveBalancesAsync(int employeeId, int financialYear);
        
        // Leave Type Operations
        Task<ServiceResponse<List<LeaveTypeDto>>> GetLeaveTypesAsync();
        Task<ServiceResponse<List<LeaveTypeDto>>> GetActiveLeaveTypesAsync();
        Task<ServiceResponse<LeaveTypeDto>> CreateLeaveTypeAsync(CreateLeaveTypeDto leaveType);
        Task<ServiceResponse<bool>> UpdateLeaveTypeAsync(int id, CreateLeaveTypeDto leaveType);
        
        // Leave Analytics and Reporting
        Task<ServiceResponse<LeaveAnalyticsDto>> GetEmployeeLeaveAnalyticsAsync(int employeeId, int financialYear);
        Task<ServiceResponse<DepartmentLeaveAnalyticsDto>> GetDepartmentLeaveAnalyticsAsync(int departmentId, int financialYear);
        Task<ServiceResponse<LeaveReportDto>> GenerateLeaveReportAsync(LeaveReportFilterDto filters);
        
        // Leave Accrual Operations
        Task<ServiceResponse<bool>> ProcessLeaveAccrualAsync(ProcessLeaveAccrualDto accrual);
        Task<ServiceResponse<List<LeaveAccrualDto>>> GetLeaveAccrualStatusAsync(int? employeeId = null);
        
        // Validation and Business Logic
        Task<ServiceResponse<LeaveValidationResult>> ValidateLeaveRequestAsync(int employeeId, CreateLeaveRequestDto request);
        Task<ServiceResponse<decimal>> CalculateLeaveDaysAsync(DateTime startDate, DateTime endDate, bool isHalfDay);
        Task<ServiceResponse<List<CalendarLeaveRequestDto>>> GetLeaveCalendarAsync(DateTime fromDate, DateTime toDate, int? departmentId = null);
    }

    public class LeaveManagementService : ILeaveManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IAutoApprovalService _autoApprovalService;

        public LeaveManagementService(ApplicationDbContext context, IEmailService emailService, IAutoApprovalService autoApprovalService)
        {
            _context = context;
            _emailService = emailService;
            _autoApprovalService = autoApprovalService;
        }

        public async Task<ServiceResponse<LeaveRequestDto>> CreateLeaveRequestAsync(CreateLeaveRequestDto request)
        {
            try
            {
                // Calculate leave days first
                var daysResult = await CalculateLeaveDaysAsync(request.StartDate, request.EndDate, request.IsHalfDay);
                if (!daysResult.IsSuccess)
                {
                    return ServiceResponse<LeaveRequestDto>.Failure("Failed to calculate leave days");
                }

                // Basic validation
                if (request.EmployeeId <= 0 || request.LeaveTypeId <= 0)
                {
                    return ServiceResponse<LeaveRequestDto>.Failure("Invalid employee or leave type");
                }

                // Check for overlapping leave requests
                var hasOverlapResult = await CheckForOverlappingLeaveRequestsAsync(request.EmployeeId, request.StartDate, request.EndDate);
                if (!hasOverlapResult.IsSuccess)
                {
                    return ServiceResponse<LeaveRequestDto>.Failure(hasOverlapResult.Message);
                }

                // Generate request number
                var requestNumber = await GenerateRequestNumberAsync();
                var currentYear = GetFinancialYear(request.StartDate);

                // Create leave request entity
                var leaveRequest = new LeaveRequest
                {
                    EmployeeId = request.EmployeeId,
                    LeaveTypeId = request.LeaveTypeId,
                    RequestNumber = requestNumber,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    DaysRequested = daysResult.Data,
                    Reason = request.Reason,
                    EmergencyContact = request.EmergencyContact,
                    AddressDuringLeave = request.AddressDuringLeave,
                    PhoneDuringLeave = request.PhoneDuringLeave,
                    Priority = Enum.Parse<LeaveRequestPriority>(request.Priority),
                    IsHalfDay = request.IsHalfDay,
                    HalfDaySession = request.IsHalfDay ? Enum.Parse<HalfDaySession>(request.HalfDaySession) : null,
                    HasSupportingDocuments = request.HasSupportingDocuments,
                    FinancialYear = currentYear,
                    Status = LeaveRequestStatus.Pending,
                    SubmittedAt = DateTime.UtcNow,
                    SubmittedBy = request.EmployeeId.ToString() // This should be the current user ID
                };

                _context.LeaveRequests.Add(leaveRequest);
                await _context.SaveChangesAsync();

                // Check for auto-approval eligibility
                var autoApprovalResult = await _autoApprovalService.EvaluateLeaveRequestAsync(leaveRequest.Id);
                
                if (autoApprovalResult.IsApproved)
                {
                    // Auto-approve the leave request
                    leaveRequest.Status = LeaveRequestStatus.Approved;
                    leaveRequest.ReviewedAt = DateTime.UtcNow;
                    leaveRequest.ReviewedBy = "Auto-Approval System";
                    leaveRequest.ApproverComments = autoApprovalResult.ApprovalComments;
                    
                    // Process the auto-approval (update balances, send notifications)
                    await _autoApprovalService.ProcessAutoApprovalAsync(leaveRequest.Id);
                    
                    // Update leave balance - deduct from available and add to used
                    await ApproveLeaveBalanceUpdateAsync(request.EmployeeId, request.LeaveTypeId, currentYear, daysResult.Data);
                }
                else
                {
                    // Create manual approval workflow
                    await CreateApprovalWorkflowAsync(leaveRequest.Id, request.EmployeeId);
                    
                    // Update leave balance pending days
                    await UpdatePendingDaysAsync(request.EmployeeId, request.LeaveTypeId, currentYear, daysResult.Data);
                }

                // Convert to DTO and return
                var dto = await GetLeaveRequestDtoAsync(leaveRequest.Id);
                return ServiceResponse<LeaveRequestDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return ServiceResponse<LeaveRequestDto>.Failure($"Error creating leave request: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<LeaveRequestDto>> UpdateLeaveRequestAsync(UpdateLeaveRequestDto request)
        {
            try
            {
                var leaveRequest = await _context.LeaveRequests
                    .FirstOrDefaultAsync(lr => lr.Id == request.Id);

                if (leaveRequest == null)
                {
                    return ServiceResponse<LeaveRequestDto>.Failure("Leave request not found");
                }

                if (leaveRequest.Status != LeaveRequestStatus.Draft && leaveRequest.Status != LeaveRequestStatus.Pending)
                {
                    return ServiceResponse<LeaveRequestDto>.Failure("Cannot update approved or completed leave requests");
                }

                // Check for overlapping leave requests (exclude current request)
                var hasOverlapResult = await CheckForOverlappingLeaveRequestsAsync(leaveRequest.EmployeeId, request.StartDate, request.EndDate, request.Id);
                if (!hasOverlapResult.IsSuccess)
                {
                    return ServiceResponse<LeaveRequestDto>.Failure(hasOverlapResult.Message);
                }

                // Calculate new leave days
                var daysResult = await CalculateLeaveDaysAsync(request.StartDate, request.EndDate, request.IsHalfDay);
                if (!daysResult.IsSuccess)
                {
                    return ServiceResponse<LeaveRequestDto>.Failure("Failed to calculate leave days");
                }

                // Update pending days if days changed
                var daysDifference = daysResult.Data - leaveRequest.DaysRequested;
                if (daysDifference != 0)
                {
                    await UpdatePendingDaysAsync(leaveRequest.EmployeeId, leaveRequest.LeaveTypeId, 
                        leaveRequest.FinancialYear, daysDifference);
                }

                // Update leave request
                leaveRequest.StartDate = request.StartDate;
                leaveRequest.EndDate = request.EndDate;
                leaveRequest.DaysRequested = daysResult.Data;
                leaveRequest.Reason = request.Reason;
                leaveRequest.EmergencyContact = request.EmergencyContact;
                leaveRequest.AddressDuringLeave = request.AddressDuringLeave;
                leaveRequest.PhoneDuringLeave = request.PhoneDuringLeave;
                leaveRequest.Priority = Enum.Parse<LeaveRequestPriority>(request.Priority);
                leaveRequest.IsHalfDay = request.IsHalfDay;
                leaveRequest.HalfDaySession = request.IsHalfDay ? Enum.Parse<HalfDaySession>(request.HalfDaySession) : null;
                leaveRequest.HasSupportingDocuments = request.HasSupportingDocuments;
                leaveRequest.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var dto = await GetLeaveRequestDtoAsync(leaveRequest.Id);
                return ServiceResponse<LeaveRequestDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return ServiceResponse<LeaveRequestDto>.Failure($"Error updating leave request: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<bool>> CancelLeaveRequestAsync(int leaveRequestId, string reason)
        {
            try
            {
                var leaveRequest = await _context.LeaveRequests
                    .FirstOrDefaultAsync(lr => lr.Id == leaveRequestId);

                if (leaveRequest == null)
                {
                    return ServiceResponse<bool>.Failure("Leave request not found");
                }

                if (leaveRequest.Status == LeaveRequestStatus.Completed)
                {
                    return ServiceResponse<bool>.Failure("Cannot cancel completed leave requests");
                }

                // Update status
                leaveRequest.Status = LeaveRequestStatus.Cancelled;
                leaveRequest.ApproverComments = reason;
                leaveRequest.ReviewedAt = DateTime.UtcNow;
                leaveRequest.UpdatedAt = DateTime.UtcNow;

                // Restore pending days to available balance
                await UpdatePendingDaysAsync(leaveRequest.EmployeeId, leaveRequest.LeaveTypeId, 
                    leaveRequest.FinancialYear, -leaveRequest.DaysRequested);

                await _context.SaveChangesAsync();

                return ServiceResponse<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.Failure($"Error cancelling leave request: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<LeaveRequestDto>> GetLeaveRequestByIdAsync(int id)
        {
            try
            {
                var dto = await GetLeaveRequestDtoAsync(id);
                if (dto == null)
                {
                    return ServiceResponse<LeaveRequestDto>.Failure("Leave request not found");
                }

                return ServiceResponse<LeaveRequestDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return ServiceResponse<LeaveRequestDto>.Failure($"Error retrieving leave request: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<List<LeaveRequestDto>>> GetEmployeeLeaveRequestsAsync(int employeeId, int? financialYear = null)
        {
            try
            {
                var currentYear = financialYear ?? GetFinancialYear(DateTime.Now);
                
                var leaveRequests = await _context.LeaveRequests
                    .Include(lr => lr.Employee)
                    .Include(lr => lr.LeaveType)
                    .Include(lr => lr.Approvals)
                    .Where(lr => lr.EmployeeId == employeeId && lr.FinancialYear == currentYear)
                    .OrderByDescending(lr => lr.SubmittedAt)
                    .Select(lr => MapToLeaveRequestDto(lr))
                    .ToListAsync();

                return ServiceResponse<List<LeaveRequestDto>>.Success(leaveRequests);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<LeaveRequestDto>>.Failure($"Error retrieving leave requests: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<List<LeaveRequestDto>>> GetPendingApprovalsAsync(string approverId)
        {
            try
            {
                var pendingApprovals = await _context.LeaveRequestApprovals
                    .Include(lra => lra.LeaveRequest)
                        .ThenInclude(lr => lr.Employee)
                    .Include(lra => lra.LeaveRequest)
                        .ThenInclude(lr => lr.LeaveType)
                    .Where(lra => lra.ApproverId == approverId && lra.Status == ApprovalStatus.Pending)
                    .Select(lra => MapToLeaveRequestDto(lra.LeaveRequest))
                    .ToListAsync();

                return ServiceResponse<List<LeaveRequestDto>>.Success(pendingApprovals);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<LeaveRequestDto>>.Failure($"Error retrieving pending approvals: {ex.Message}");
            }
        }

        // Additional service methods would continue here...
        // Due to length constraints, I'll continue with key methods

        private async Task<string> GenerateRequestNumberAsync()
        {
            var year = DateTime.Now.Year.ToString();
            var count = await _context.LeaveRequests.CountAsync(lr => lr.CreatedAt.Year == DateTime.Now.Year);
            return $"LR{year}{(count + 1):D4}";
        }

        private int GetFinancialYear(DateTime date)
        {
            return date.Month >= 4 ? date.Year : date.Year - 1;
        }

        private async Task CreateApprovalWorkflowAsync(int leaveRequestId, int employeeId)
        {
            // Simple approval workflow - this can be enhanced based on business requirements
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee?.Department?.ManagerEmployeeId != null)
            {
                var approval = new LeaveRequestApproval
                {
                    LeaveRequestId = leaveRequestId,
                    ApprovalLevel = 1,
                    ApproverId = employee.Department.ManagerEmployeeId.Value.ToString(),
                    ApproverName = "Department Manager", // This should be populated with actual manager name
                    ApproverEmail = "manager@company.com", // This should be populated with actual email
                    Status = ApprovalStatus.Pending,
                    IsRequired = true,
                    IsNotified = false
                };

                _context.LeaveRequestApprovals.Add(approval);
            }
        }

        private async Task UpdatePendingDaysAsync(int employeeId, int leaveTypeId, int financialYear, decimal daysChange)
        {
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.EmployeeId == employeeId && 
                                          lb.LeaveTypeId == leaveTypeId && 
                                          lb.FinancialYear == financialYear);

            if (leaveBalance != null)
            {
                // Recalculate pending days from all pending requests instead of using daysChange
                var totalPendingDays = await _context.LeaveRequests
                    .Where(lr => lr.EmployeeId == employeeId && 
                               lr.LeaveTypeId == leaveTypeId && 
                               lr.FinancialYear == financialYear && 
                               lr.Status == LeaveRequestStatus.Pending)
                    .SumAsync(lr => lr.DaysRequested);
                
                leaveBalance.PendingDays = totalPendingDays;
                leaveBalance.UpdatedAt = DateTime.UtcNow;
            }
        }

        private async Task ApproveLeaveBalanceUpdateAsync(int employeeId, int leaveTypeId, int financialYear, decimal daysUsed)
        {
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.EmployeeId == employeeId && 
                                          lb.LeaveTypeId == leaveTypeId && 
                                          lb.FinancialYear == financialYear);

            if (leaveBalance != null)
            {
                // Add to used days (cumulative)
                leaveBalance.UsedDays += daysUsed;
                
                // Recalculate pending days from all pending requests
                var totalPendingDays = await _context.LeaveRequests
                    .Where(lr => lr.EmployeeId == employeeId && 
                               lr.LeaveTypeId == leaveTypeId && 
                               lr.FinancialYear == financialYear && 
                               lr.Status == LeaveRequestStatus.Pending)
                    .SumAsync(lr => lr.DaysRequested);
                
                leaveBalance.PendingDays = totalPendingDays;
                leaveBalance.UpdatedAt = DateTime.UtcNow;
            }
        }

        private async Task<LeaveRequestDto> GetLeaveRequestDtoAsync(int id)
        {
            return await _context.LeaveRequests
                .Include(lr => lr.Employee)
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.Approvals)
                .Where(lr => lr.Id == id)
                .Select(lr => MapToLeaveRequestDto(lr))
                .FirstOrDefaultAsync();
        }

        private static LeaveRequestDto MapToLeaveRequestDto(LeaveRequest leaveRequest)
        {
            return new LeaveRequestDto
            {
                Id = leaveRequest.Id,
                EmployeeId = leaveRequest.EmployeeId,
                LeaveTypeId = leaveRequest.LeaveTypeId,
                RequestNumber = leaveRequest.RequestNumber,
                StartDate = leaveRequest.StartDate,
                EndDate = leaveRequest.EndDate,
                DaysRequested = leaveRequest.DaysRequested,
                Reason = leaveRequest.Reason,
                EmergencyContact = leaveRequest.EmergencyContact,
                AddressDuringLeave = leaveRequest.AddressDuringLeave,
                PhoneDuringLeave = leaveRequest.PhoneDuringLeave,
                Status = leaveRequest.Status.ToString(),
                SubmittedAt = leaveRequest.SubmittedAt,
                SubmittedBy = leaveRequest.SubmittedBy,
                ApproverComments = leaveRequest.ApproverComments,
                ReviewedAt = leaveRequest.ReviewedAt,
                ReviewedBy = leaveRequest.ReviewedBy,
                Priority = leaveRequest.Priority.ToString(),
                IsHalfDay = leaveRequest.IsHalfDay,
                HalfDaySession = leaveRequest.HalfDaySession?.ToString() ?? string.Empty,
                HasSupportingDocuments = leaveRequest.HasSupportingDocuments,
                FinancialYear = leaveRequest.FinancialYear,
                EmployeeName = leaveRequest.Employee?.FullName ?? string.Empty,
                LeaveTypeName = leaveRequest.LeaveType?.Name ?? string.Empty,
                LeaveTypeColor = leaveRequest.LeaveType?.ColorCode ?? "#007bff",
                Employee = leaveRequest.Employee != null ? new EmployeeBasicDto
                {
                    Id = leaveRequest.Employee.Id,
                    FirstName = leaveRequest.Employee.FirstName,
                    LastName = leaveRequest.Employee.LastName,
                    Email = leaveRequest.Employee.Email,
                    EmployeeCode = leaveRequest.Employee.EmployeeCode
                } : null,
                LeaveType = leaveRequest.LeaveType != null ? new LeaveTypeDto
                {
                    Id = leaveRequest.LeaveType.Id,
                    Name = leaveRequest.LeaveType.Name,
                    Code = leaveRequest.LeaveType.Code,
                    ColorCode = leaveRequest.LeaveType.ColorCode
                } : null,
                Approvals = leaveRequest.Approvals?.Select(a => new LeaveRequestApprovalDto
                {
                    Id = a.Id,
                    LeaveRequestId = a.LeaveRequestId,
                    ApprovalLevel = a.ApprovalLevel,
                    ApproverId = a.ApproverId,
                    ApproverName = a.ApproverName,
                    ApproverEmail = a.ApproverEmail,
                    Status = a.Status.ToString(),
                    Comments = a.Comments,
                    ApprovedAt = a.ApprovedAt,
                    IsRequired = a.IsRequired,
                    IsNotified = a.IsNotified,
                    NotifiedAt = a.NotifiedAt,
                    DeadlineAt = a.DeadlineAt,
                    CanDelegate = a.CanDelegate,
                    DelegatedTo = a.DelegatedTo
                }).ToList() ?? new List<LeaveRequestApprovalDto>()
            };
        }

        // Leave Approval Implementation
        public async Task<ServiceResponse<bool>> ApproveLeaveRequestAsync(int leaveRequestId, int approverId, string comments)
        {
            try
            {
                var leaveRequest = await _context.LeaveRequests
                    .Include(lr => lr.Employee)
                    .Include(lr => lr.LeaveType)
                    .FirstOrDefaultAsync(lr => lr.Id == leaveRequestId);
                
                if (leaveRequest == null)
                    return ServiceResponse<bool>.Failure("Leave request not found");

                // Get approver details
                var approver = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Id == approverId);

                leaveRequest.Status = LeaveRequestStatus.Approved;
                leaveRequest.ReviewedBy = approverId.ToString();
                leaveRequest.ReviewedAt = DateTime.UtcNow;
                leaveRequest.ApproverComments = comments;

                // Update leave balance - move from pending to used days
                var daysResult = await CalculateLeaveDaysAsync(leaveRequest.StartDate, leaveRequest.EndDate, leaveRequest.IsHalfDay);
                if (daysResult.IsSuccess)
                {
                    await ApproveLeaveBalanceUpdateAsync(leaveRequest.EmployeeId, leaveRequest.LeaveTypeId, leaveRequest.FinancialYear, daysResult.Data);
                }

                await _context.SaveChangesAsync();

                // Send email notification
                try
                {
                    var leaveRequestDto = MapToLeaveRequestDto(leaveRequest);
                    var approverName = approver != null ? $"{approver.FirstName} {approver.LastName}" : "Admin";
                    await _emailService.SendLeaveApprovalNotificationAsync(leaveRequestDto, approverName, comments);
                }
                catch (Exception emailEx)
                {
                    // Log email error but don't fail the approval
                    Console.WriteLine($"Failed to send approval email: {emailEx.Message}");
                }

                return ServiceResponse<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.Failure($"Error approving leave request: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<bool>> RejectLeaveRequestAsync(int leaveRequestId, int approverId, string comments)
        {
            try
            {
                var leaveRequest = await _context.LeaveRequests
                    .Include(lr => lr.Employee)
                    .Include(lr => lr.LeaveType)
                    .FirstOrDefaultAsync(lr => lr.Id == leaveRequestId);
                
                if (leaveRequest == null)
                    return ServiceResponse<bool>.Failure("Leave request not found");

                // Get approver details
                var approver = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Id == approverId);

                leaveRequest.Status = LeaveRequestStatus.Rejected;
                leaveRequest.ReviewedBy = approverId.ToString();
                leaveRequest.ReviewedAt = DateTime.UtcNow;
                leaveRequest.ApproverComments = comments;

                // Update leave balance - remove pending days since request is rejected
                var daysResult = await CalculateLeaveDaysAsync(leaveRequest.StartDate, leaveRequest.EndDate, leaveRequest.IsHalfDay);
                if (daysResult.IsSuccess)
                {
                    // Subtract pending days (negative value to remove them)
                    await UpdatePendingDaysAsync(leaveRequest.EmployeeId, leaveRequest.LeaveTypeId, leaveRequest.FinancialYear, -daysResult.Data);
                }

                await _context.SaveChangesAsync();

                // Send email notification
                try
                {
                    var leaveRequestDto = MapToLeaveRequestDto(leaveRequest);
                    var approverName = approver != null ? $"{approver.FirstName} {approver.LastName}" : "Admin";
                    await _emailService.SendLeaveRejectionNotificationAsync(leaveRequestDto, approverName, comments);
                }
                catch (Exception emailEx)
                {
                    // Log email error but don't fail the rejection
                    Console.WriteLine($"Failed to send rejection email: {emailEx.Message}");
                }

                return ServiceResponse<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.Failure($"Error rejecting leave request: {ex.Message}");
            }
        }
        public Task<ServiceResponse<bool>> DelegateApprovalAsync(int leaveRequestId, string fromApproverId, string toApproverId) => throw new NotImplementedException();
        
        public async Task<ServiceResponse<LeaveSummaryDto>> GetEmployeeLeaveSummaryAsync(int employeeId, int? financialYear = null)
        {
            try
            {
                var year = financialYear ?? GetFinancialYear(DateTime.Now);
                
                // Get employee gender for filtering leave types
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Id == employeeId);
                    
                if (employee == null)
                {
                    return ServiceResponse<LeaveSummaryDto>.Failure("Employee not found");
                }
                
                var leaveBalances = await _context.LeaveBalances
                    .Include(lb => lb.LeaveType)
                    .Where(lb => lb.EmployeeId == employeeId && lb.FinancialYear == year)
                    .ToListAsync();

                // Filter leave balances based on employee gender and leave type gender restrictions
                // Also exclude Public Holiday from leave balance calculations
                var filteredLeaveBalances = leaveBalances.Where(lb => 
                    lb.LeaveType.Code != "PH" && // Exclude Public Holiday from calculations
                    (lb.LeaveType.GenderRestriction == null || // No restriction (applies to all)
                    (employee.Gender == Gender.Male && lb.LeaveType.GenderRestriction == LeaveGenderRestriction.Male) || // Male-only leave for males
                    (employee.Gender == Gender.Female && lb.LeaveType.GenderRestriction == LeaveGenderRestriction.Female)) // Female-only leave for females
                ).ToList();

                var summary = new LeaveSummaryDto
                {
                    EmployeeId = employeeId,
                    FinancialYear = year,
                    TotalAllocated = filteredLeaveBalances.Sum(lb => lb.AllocatedDays),
                    TotalUsed = filteredLeaveBalances.Sum(lb => lb.UsedDays),
                    TotalAvailable = filteredLeaveBalances.Sum(lb => lb.AvailableDays),
                    TotalPending = filteredLeaveBalances.Sum(lb => lb.PendingDays),
                    LeaveTypeBalances = filteredLeaveBalances.Select(lb => new LeaveTypeBalanceDto
                    {
                        Id = lb.LeaveType.Id,
                        LeaveTypeId = lb.LeaveTypeId,
                        Name = lb.LeaveType.Name,
                        LeaveTypeName = lb.LeaveType.Name,
                        Code = lb.LeaveType.Code,
                        LeaveTypeCode = lb.LeaveType.Code,
                        ColorCode = lb.LeaveType.ColorCode,
                        AllocatedDays = lb.AllocatedDays,
                        UsedDays = lb.UsedDays,
                        AvailableDays = lb.AllocatedDays + lb.CarriedOverDays - lb.UsedDays - lb.PendingDays,
                        PendingDays = lb.PendingDays,
                        CarriedOverDays = lb.CarriedOverDays,
                        ExpiringDays = lb.ExpiringDays,
                        ExpiryDate = lb.ExpiryDate,
                        RequiresDocuments = false // Default value for now
                    }).ToList()
                };

                return ServiceResponse<LeaveSummaryDto>.Success(summary);
            }
            catch (Exception ex)
            {
                return ServiceResponse<LeaveSummaryDto>.Failure($"Error getting leave summary: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<List<LeaveTypeBalanceDto>>> GetEmployeeLeaveBalancesAsync(int employeeId, int? financialYear = null)
        {
            try
            {
                var year = financialYear ?? GetFinancialYear(DateTime.Now);
                
                // Get employee gender for filtering leave types
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Id == employeeId);
                    
                if (employee == null)
                {
                    return ServiceResponse<List<LeaveTypeBalanceDto>>.Failure("Employee not found");
                }
                
                var leaveBalances = await _context.LeaveBalances
                    .Include(lb => lb.LeaveType)
                    .Where(lb => lb.EmployeeId == employeeId && lb.FinancialYear == year && lb.LeaveType.IsActive &&
                        lb.LeaveType.Code != "PH" && // Exclude Public Holiday from calculations
                        (lb.LeaveType.GenderRestriction == null || // No restriction (applies to all)
                         (employee.Gender == Gender.Male && lb.LeaveType.GenderRestriction == LeaveGenderRestriction.Male) || // Male-only leave for males
                         (employee.Gender == Gender.Female && lb.LeaveType.GenderRestriction == LeaveGenderRestriction.Female))) // Female-only leave for females
                    .Select(lb => new LeaveTypeBalanceDto
                    {
                        Id = lb.LeaveType.Id,
                        LeaveTypeId = lb.LeaveTypeId,
                        Name = lb.LeaveType.Name,
                        LeaveTypeName = lb.LeaveType.Name,
                        Code = lb.LeaveType.Code,
                        LeaveTypeCode = lb.LeaveType.Code,
                        ColorCode = lb.LeaveType.ColorCode,
                        AllocatedDays = lb.AllocatedDays,
                        UsedDays = lb.UsedDays,
                        AvailableDays = lb.AllocatedDays + lb.CarriedOverDays - lb.UsedDays - lb.PendingDays,
                        PendingDays = lb.PendingDays,
                        CarriedOverDays = lb.CarriedOverDays,
                        ExpiringDays = lb.ExpiringDays,
                        ExpiryDate = lb.ExpiryDate,
                        RequiresDocuments = false // Default value for now
                    })
                    .ToListAsync();

                return ServiceResponse<List<LeaveTypeBalanceDto>>.Success(leaveBalances);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<LeaveTypeBalanceDto>>.Failure($"Error getting leave balances: {ex.Message}");
            }
        }
        public Task<ServiceResponse<bool>> UpdateLeaveBalanceAsync(UpdateLeaveBalanceDto balance) => throw new NotImplementedException();
        public Task<ServiceResponse<bool>> InitializeLeaveBalancesAsync(int employeeId, int financialYear) => throw new NotImplementedException();
        
        public async Task<ServiceResponse<List<LeaveTypeDto>>> GetLeaveTypesAsync()
        {
            try
            {
                var leaveTypes = await _context.LeaveTypes
                    .Select(lt => new LeaveTypeDto
                    {
                        Id = lt.Id,
                        Name = lt.Name,
                        Code = lt.Code,
                        Description = lt.Description,
                        ColorCode = lt.ColorCode,
                        IsActive = lt.IsActive,
                        AnnualAllocation = lt.AnnualAllocation,
                        CanCarryOver = lt.CanCarryOver,
                        MaxCarryOverDays = lt.MaxCarryOverDays,
                        RequiresApproval = lt.RequiresApproval,
                        MinimumNoticeRequiredDays = lt.MinimumNoticeRequiredDays,
                        MaxConsecutiveDays = lt.MaxConsecutiveDays,
                        IsPaid = lt.IsPaid,
                        AccrualFrequency = lt.AccrualFrequency.ToString(),
                        GenderRestriction = lt.GenderRestriction.HasValue ? lt.GenderRestriction.Value.ToString() : ""
                    })
                    .ToListAsync();

                return ServiceResponse<List<LeaveTypeDto>>.Success(leaveTypes);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<LeaveTypeDto>>.Failure($"Error getting leave types: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<List<LeaveTypeDto>>> GetActiveLeaveTypesAsync()
        {
            try
            {
                var leaveTypes = await _context.LeaveTypes
                    .Where(lt => lt.IsActive)
                    .Select(lt => new LeaveTypeDto
                    {
                        Id = lt.Id,
                        Name = lt.Name,
                        Code = lt.Code,
                        Description = lt.Description,
                        ColorCode = lt.ColorCode,
                        IsActive = lt.IsActive,
                        AnnualAllocation = lt.AnnualAllocation,
                        CanCarryOver = lt.CanCarryOver,
                        MaxCarryOverDays = lt.MaxCarryOverDays,
                        RequiresApproval = lt.RequiresApproval,
                        MinimumNoticeRequiredDays = lt.MinimumNoticeRequiredDays,
                        MaxConsecutiveDays = lt.MaxConsecutiveDays,
                        IsPaid = lt.IsPaid,
                        AccrualFrequency = lt.AccrualFrequency.ToString(),
                        GenderRestriction = lt.GenderRestriction.HasValue ? lt.GenderRestriction.Value.ToString() : ""
                    })
                    .ToListAsync();

                return ServiceResponse<List<LeaveTypeDto>>.Success(leaveTypes);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<LeaveTypeDto>>.Failure($"Error getting active leave types: {ex.Message}");
            }
        }
        public Task<ServiceResponse<LeaveTypeDto>> CreateLeaveTypeAsync(CreateLeaveTypeDto leaveType) => throw new NotImplementedException();
        public Task<ServiceResponse<bool>> UpdateLeaveTypeAsync(int id, CreateLeaveTypeDto leaveType) => throw new NotImplementedException();
        public Task<ServiceResponse<LeaveAnalyticsDto>> GetEmployeeLeaveAnalyticsAsync(int employeeId, int financialYear) => throw new NotImplementedException();
        public Task<ServiceResponse<DepartmentLeaveAnalyticsDto>> GetDepartmentLeaveAnalyticsAsync(int departmentId, int financialYear) => throw new NotImplementedException();
        public Task<ServiceResponse<LeaveReportDto>> GenerateLeaveReportAsync(LeaveReportFilterDto filters) => throw new NotImplementedException();
        public Task<ServiceResponse<bool>> ProcessLeaveAccrualAsync(ProcessLeaveAccrualDto accrual) => throw new NotImplementedException();
        public Task<ServiceResponse<List<LeaveAccrualDto>>> GetLeaveAccrualStatusAsync(int? employeeId = null) => throw new NotImplementedException();
        public async Task<ServiceResponse<List<LeaveRequestDto>>> GetPendingLeaveRequestsAsync()
        {
            try
            {
                var pendingRequests = await _context.LeaveRequests
                    .Include(lr => lr.Employee)
                    .Include(lr => lr.LeaveType)
                    .Where(lr => lr.Status == LeaveRequestStatus.Pending)
                    .OrderBy(lr => lr.StartDate)
                    .Select(lr => new LeaveRequestDto
                    {
                        Id = lr.Id,
                        EmployeeId = lr.EmployeeId,
                        EmployeeName = lr.Employee.FirstName + " " + lr.Employee.LastName,
                        DepartmentName = lr.Employee.Department != null ? lr.Employee.Department.Name : "",
                        DepartmentId = lr.Employee.DepartmentId,
                        LeaveTypeId = lr.LeaveTypeId,
                        LeaveTypeName = lr.LeaveType.Name,
                        LeaveTypeColor = lr.LeaveType.ColorCode,
                        RequestNumber = lr.RequestNumber,
                        StartDate = lr.StartDate,
                        EndDate = lr.EndDate,
                        DaysRequested = lr.DaysRequested,
                        Reason = lr.Reason,
                        Status = lr.Status.ToString(),
                        IsHalfDay = lr.IsHalfDay,
                        HalfDaySession = lr.HalfDaySession.ToString(),
                        SubmittedAt = lr.SubmittedAt,
                        ApprovedAt = lr.ReviewedAt,
                        ApproverName = lr.ReviewedBy ?? "",
                        Comments = lr.ApproverComments ?? "",
                        RejectionReason = lr.ApproverComments ?? ""
                    })
                    .ToListAsync();

                return ServiceResponse<List<LeaveRequestDto>>.Success(pendingRequests);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<LeaveRequestDto>>.Failure($"Error getting pending leave requests: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<bool>> SubmitLeaveRequestAsync(int leaveRequestId)
        {
            try
            {
                var leaveRequest = await _context.LeaveRequests
                    .FirstOrDefaultAsync(lr => lr.Id == leaveRequestId);
                
                if (leaveRequest == null)
                    return ServiceResponse<bool>.Failure("Leave request not found");

                if (leaveRequest.Status != LeaveRequestStatus.Draft)
                    return ServiceResponse<bool>.Failure("Only draft requests can be submitted");

                leaveRequest.Status = LeaveRequestStatus.Pending;
                leaveRequest.SubmittedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return ServiceResponse<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.Failure($"Error submitting leave request: {ex.Message}");
            }
        }

        public Task<ServiceResponse<LeaveValidationResult>> ValidateLeaveRequestAsync(int employeeId, CreateLeaveRequestDto request) => throw new NotImplementedException();
        
        public async Task<ServiceResponse<decimal>> CalculateLeaveDaysAsync(DateTime startDate, DateTime endDate, bool isHalfDay)
        {
            try
            {
                if (isHalfDay)
                {
                    return ServiceResponse<decimal>.Success(0.5m);
                }

                var businessDays = 0;
                var currentDate = startDate;
                
                while (currentDate <= endDate)
                {
                    if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                    {
                        businessDays++;
                    }
                    currentDate = currentDate.AddDays(1);
                }
                
                return ServiceResponse<decimal>.Success(businessDays);
            }
            catch (Exception ex)
            {
                return ServiceResponse<decimal>.Failure($"Error calculating leave days: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<List<CalendarLeaveRequestDto>>> GetLeaveCalendarAsync(DateTime fromDate, DateTime toDate, int? departmentId = null)
        {
            try
            {
                var query = _context.LeaveRequests
                    .Include(lr => lr.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(lr => lr.LeaveType)
                    .Where(lr => 
                        (lr.StartDate <= toDate && lr.EndDate >= fromDate) && // Overlapping date range
                        lr.Status != LeaveRequestStatus.Draft); // Exclude draft requests

                // Apply department filter if specified
                if (departmentId.HasValue)
                {
                    query = query.Where(lr => lr.Employee.DepartmentId == departmentId.Value);
                }

                var leaveRequests = await query
                    .OrderBy(lr => lr.StartDate)
                    .ThenBy(lr => lr.Employee.FirstName)
                    .ToListAsync();

                var calendarData = leaveRequests.Select(lr => new CalendarLeaveRequestDto
                {
                    Id = lr.Id,
                    RequestNumber = lr.RequestNumber,
                    EmployeeId = lr.EmployeeId,
                    EmployeeName = $"{lr.Employee.FirstName} {lr.Employee.LastName}",
                    DepartmentId = lr.Employee.DepartmentId,
                    DepartmentName = lr.Employee.Department?.Name ?? "N/A",
                    LeaveTypeId = lr.LeaveTypeId,
                    LeaveTypeName = lr.LeaveType.Name,
                    LeaveTypeCode = lr.LeaveType.Code,
                    LeaveTypeColor = lr.LeaveType.ColorCode ?? "#007bff",
                    StartDate = lr.StartDate,
                    EndDate = lr.EndDate,
                    DaysRequested = (int)lr.DaysRequested,
                    IsHalfDay = lr.IsHalfDay,
                    Status = lr.Status.ToString(),
                    Reason = lr.Reason ?? string.Empty,
                    ApproverComments = lr.ApproverComments,
                    SubmittedAt = lr.SubmittedAt,
                    ReviewedAt = lr.ReviewedAt,
                    ReviewedBy = !string.IsNullOrEmpty(lr.ReviewedBy) ? int.TryParse(lr.ReviewedBy, out var reviewerId) ? reviewerId : null : null,
                    ReviewerName = !string.IsNullOrEmpty(lr.ReviewedBy) ? "Admin" : null // TODO: Proper reviewer lookup needed
                }).ToList();

                return new ServiceResponse<List<CalendarLeaveRequestDto>>
                {
                    IsSuccess = true,
                    Data = calendarData,
                    Message = $"Retrieved {calendarData.Count} leave requests for calendar view"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<List<CalendarLeaveRequestDto>>
                {
                    IsSuccess = false,
                    Message = $"Error retrieving calendar data: {ex.Message}",
                    Data = new List<CalendarLeaveRequestDto>()
                };
            }
        }

        private async Task<ServiceResponse<bool>> CheckForOverlappingLeaveRequestsAsync(int employeeId, DateTime startDate, DateTime endDate, int? excludeRequestId = null)
        {
            try
            {
                var overlappingRequests = await _context.LeaveRequests
                    .Where(lr => 
                        lr.EmployeeId == employeeId &&
                        lr.Status != LeaveRequestStatus.Cancelled &&
                        lr.Status != LeaveRequestStatus.Rejected &&
                        lr.Status != LeaveRequestStatus.Draft &&
                        (excludeRequestId == null || lr.Id != excludeRequestId) &&
                        // Check for date overlap: (StartA <= EndB) AND (EndA >= StartB)
                        lr.StartDate <= endDate &&
                        lr.EndDate >= startDate)
                    .Include(lr => lr.LeaveType)
                    .ToListAsync();

                if (overlappingRequests.Any())
                {
                    var conflictDetails = overlappingRequests
                        .Select(lr => $"'{lr.LeaveType.Name}' from {lr.StartDate:dd/MM/yyyy} to {lr.EndDate:dd/MM/yyyy} (Status: {lr.Status})")
                        .ToList();
                    
                    var message = $"Cannot create leave request due to overlapping dates. Existing leave request(s): {string.Join(", ", conflictDetails)}";
                    
                    return ServiceResponse<bool>.Failure(message);
                }

                return ServiceResponse<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.Failure($"Error checking for overlapping leave requests: {ex.Message}");
            }
        }
    }
}