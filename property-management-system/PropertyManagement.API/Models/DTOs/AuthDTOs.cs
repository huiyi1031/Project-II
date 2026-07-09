using PropertyManagement.API.Models.Enums;

namespace PropertyManagement.API.Models.DTOs
{
    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public long UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AccountStatus { get; set; } = string.Empty;
        /// <summary>Only set for Occupant role: "Owner" | "Tenant" | "Resident"</summary>
        public string? OccupantType { get; set; }
        public bool RequiresPasswordChange { get; set; }
        public bool IsFirstLogin { get; set; }
        public string? UpdateToken { get; set; }
    }

    public class RegisterRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public RoleType RoleType { get; set; }
    }

    public class RegisterResponseDto
    {
        public long UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ChangePasswordRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class OwnerVerificationRequestDto
    {
        public string IdentificationNo { get; set; } = string.Empty;
    }

    public class VerifyIcResponseDto
    {
        public bool Found { get; set; }
        public string MaskedEmail { get; set; } = string.Empty;
        public string UpdateToken { get; set; } = string.Empty;
    }

    public class UpdateEmailRequestDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyTempPasswordRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string TemporaryPassword { get; set; } = string.Empty;
    }

    public class SetPasswordRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string? UpdateToken { get; set; }
    }
}