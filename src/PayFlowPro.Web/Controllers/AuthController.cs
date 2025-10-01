using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PayFlowPro.Models.Entities;

namespace PayFlowPro.Blazor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(SignInManager<ApplicationUser> signInManager, ILogger<AuthController> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                _logger.LogInformation("User logged out successfully via API");
                return Ok(new { success = true, redirectUrl = "/" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("logout")]
        public async Task<IActionResult> LogoutGet()
        {
            try
            {
                await _signInManager.SignOutAsync();
                _logger.LogInformation("User logged out successfully via GET");
                return Redirect("/");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return BadRequest("Logout failed");
            }
        }
    }
}