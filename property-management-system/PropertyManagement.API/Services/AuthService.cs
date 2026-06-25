using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models.DTOs;
using PropertyManagement.API.Models.Entities;
using PropertyManagement.API.Models.Enums;

namespace PropertyManagement.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            // Find user by email
            var user = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                throw new Exception("User not found");
            }

            // Verify password (using BCrypt)
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new Exception("Invalid password");
            }

            // Check account status
            if (user.AccountStatus != AccountStatus.Active)
            {
                throw new Exception("Account is not active");
            }

            // Update last login
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Get full name based on role
            string fullName = await GetUserFullName(user);

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return new LoginResponseDto
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                Role = user.RoleType.ToString(),
                FullName = fullName,
                AccountStatus = user.AccountStatus.ToString()
            };
        }

        public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            // Check if email already exists
            var existingUser = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                throw new Exception("Email already registered");
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create user account
            var user = new UserAccount
            {
                Email = request.Email,
                PasswordHash = passwordHash,
                RoleType = request.RoleType,
                AccountStatus = AccountStatus.Pending
            };

            _context.UserAccounts.Add(user);
            await _context.SaveChangesAsync();

            // Create role-specific record
            if (request.RoleType == RoleType.Occupant)
            {
                var occupant = new Occupant
                {
                    UserAccountId = user.Id,
                    FullName = request.FullName,
                    OccupantType = OccupantType.Tenant,
                    OccupantStatus = "Pending"
                };
                _context.Occupants.Add(occupant);
            }
            else if (request.RoleType == RoleType.Technician)
            {
                var technician = new Technician
                {
                    UserAccountId = user.Id,
                    FullName = request.FullName,
                    AvailabilityStatus = "Available"
                };
                _context.Technicians.Add(technician);
            }
            else if (request.RoleType == RoleType.PropertyManager)
            {
                var manager = new PropertyManager
                {
                    UserAccountId = user.Id,
                    FullName = request.FullName
                };
                _context.PropertyManagers.Add(manager);
            }

            await _context.SaveChangesAsync();

            return new RegisterResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Message = "Registration successful. Please wait for account activation."
            };
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordRequestDto request)
        {
            var user = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return false;
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return false;
            }

            // Update to new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> VerifyOwnerAsync(OwnerVerificationRequestDto request)
        {
            var occupant = await _context.Occupants
                .FirstOrDefaultAsync(o => o.IdentificationNo == request.IdentificationNo 
                    && o.OccupantType == OccupantType.Owner);

            if (occupant == null)
            {
                return false;
            }

            var user = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.Id == occupant.UserAccountId);

            if (user != null)
            {
                user.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return true;
        }

        private string GenerateJwtToken(UserAccount user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured"));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.RoleType.ToString())
            };

            var key = new SymmetricSecurityKey(secretKey);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryMinutes"] ?? "60"));

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<string> GetUserFullName(UserAccount user)
        {
            if (user.RoleType == RoleType.Occupant)
            {
                var occupant = await _context.Occupants
                    .FirstOrDefaultAsync(o => o.UserAccountId == user.Id);
                return occupant?.FullName ?? "Occupant";
            }
            else if (user.RoleType == RoleType.Technician)
            {
                var technician = await _context.Technicians
                    .FirstOrDefaultAsync(t => t.UserAccountId == user.Id);
                return technician?.FullName ?? "Technician";
            }
            else if (user.RoleType == RoleType.PropertyManager)
            {
                var manager = await _context.PropertyManagers
                    .FirstOrDefaultAsync(pm => pm.UserAccountId == user.Id);
                return manager?.FullName ?? "Property Manager";
            }
            return "User";
        }
    }
}