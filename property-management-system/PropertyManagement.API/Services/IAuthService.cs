using PropertyManagement.API.Models.DTOs;

namespace PropertyManagement.API.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<bool> ChangePasswordAsync(ChangePasswordRequestDto request);
        Task<bool> VerifyOwnerAsync(OwnerVerificationRequestDto request);
    }
}