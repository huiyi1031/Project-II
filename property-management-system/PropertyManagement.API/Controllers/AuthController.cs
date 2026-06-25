using Microsoft.AspNetCore.Mvc;
using PropertyManagement.API.Models.DTOs;
using PropertyManagement.API.Services;

namespace PropertyManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            try
            {
                var result = await _authService.ChangePasswordAsync(request);
                if (result)
                {
                    return Ok(new { message = "Password changed successfully" });
                }
                return BadRequest(new { message = "Invalid current password" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("verify-owner")]
        public async Task<IActionResult> VerifyOwner([FromBody] OwnerVerificationRequestDto request)
        {
            try
            {
                var result = await _authService.VerifyOwnerAsync(request);
                if (result)
                {
                    return Ok(new { message = "Owner verified successfully" });
                }
                return BadRequest(new { message = "Invalid identification number" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}