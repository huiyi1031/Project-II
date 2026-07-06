using PropertyManagement.API.Models.DTOs;

namespace PropertyManagement.API.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<bool> ChangePasswordAsync(ChangePasswordRequestDto request);
        Task<VerifyIcResponseDto> VerifyIcAsync(string identificationNo);
        Task UpdateEmailByIcAsync(string updateToken, string newEmail);
        Task<string> VerifyTempPasswordAsync(VerifyTempPasswordRequestDto request);
        Task<LoginResponseDto> SetPasswordAsync(SetPasswordRequestDto request);
    }
}