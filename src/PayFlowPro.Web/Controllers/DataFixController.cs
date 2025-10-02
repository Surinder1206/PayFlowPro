using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayFlowPro.Web.Services;

namespace PayFlowPro.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // Only admins can run data fixes
public class DataFixController : ControllerBase
{
    private readonly DataFixService _dataFixService;
    private readonly ILogger<DataFixController> _logger;

    public DataFixController(DataFixService dataFixService, ILogger<DataFixController> logger)
    {
        _dataFixService = dataFixService;
        _logger = logger;
    }

    [HttpPost("fix-accrued-days")]
    public async Task<IActionResult> FixAccruedDays()
    {
        try
        {
            _logger.LogInformation("API call to fix accrued days initiated");

            var updatedCount = await _dataFixService.FixAccruedDaysAsync();

            await _dataFixService.LogLeaveBalancesAsync();

            return Ok(new {
                message = $"Successfully fixed {updatedCount} leave balance records",
                updatedCount = updatedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fixing accrued days");
            return StatusCode(500, new { message = "An error occurred while fixing the data", error = ex.Message });
        }
    }

    [HttpGet("leave-balances")]
    public async Task<IActionResult> GetLeaveBalances()
    {
        try
        {
            await _dataFixService.LogLeaveBalancesAsync();
            return Ok(new { message = "Leave balances logged to console" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting leave balances");
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }
}