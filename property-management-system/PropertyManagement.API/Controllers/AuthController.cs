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

        [HttpPost("verify-ic")]
        public async Task<IActionResult> VerifyOwner([FromBody] OwnerVerificationRequestDto request)
        {
            try
            {
                var result = await _authService.VerifyIcAsync(request.IdentificationNo);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("update-email")]
        public async Task<IActionResult> UpdateEmail([FromHeader(Name = "X-Update-Token")] string updateToken, [FromBody] UpdateEmailRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(updateToken)) return Unauthorized(new { message = "Missing update token." });
                await _authService.UpdateEmailByIcAsync(updateToken, request.Email);
                return Ok(new { message = "Email updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("verify-temp-password")]
        public async Task<IActionResult> VerifyTempPassword([FromBody] VerifyTempPasswordRequestDto request)
        {
            try
            {
                var token = await _authService.VerifyTempPasswordAsync(request);
                return Ok(new { tempVerifiedToken = token });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("set-password")]
        public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequestDto request)
        {
            try
            {
                var result = await _authService.SetPasswordAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            try
            {
                await _authService.ForgotPasswordAsync(request.Email);
                return Ok(new { message = "If the email is registered, a temporary password has been sent." });
            }
            catch (Exception ex)
            {
                // We shouldn't really expose internal errors here unless necessary, but keeping it consistent with the rest
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}