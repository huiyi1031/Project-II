using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
        private readonly IEmailService _emailService;

        public AuthService(AppDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            // Generic error for both not found and wrong password (security: never reveal which)
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new Exception("Incorrect email or password. Please try again.");
            }

            if (user.AccountStatus == AccountStatus.Suspended)
            {
                throw new Exception("Account is suspended. Please contact the property management office.");
            }

            // Password is correct. Check if first-time login (Pending = must set permanent password)
            if (user.AccountStatus == AccountStatus.Pending)
            {
                var updateToken = GenerateUpdateToken(user);
                return new LoginResponseDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Role = user.RoleType.ToString(),
                    RequiresPasswordChange = true,
                    IsFirstLogin = (user.LastLogin == null),
                    UpdateToken = updateToken
                };
            }

            // Normal active login
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            string fullName = await GetUserFullName(user);
            string? occupantType = null;
            if (user.RoleType == RoleType.Occupant)
            {
                var occ = await _context.Occupants.FirstOrDefaultAsync(o => o.UserAccountId == user.Id);
                occupantType = occ?.OccupantType.ToString();
            }

            var token = GenerateJwtToken(user);

            return new LoginResponseDto
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                Role = user.RoleType.ToString(),
                FullName = fullName,
                AccountStatus = user.AccountStatus.ToString(),
                OccupantType = occupantType,
                RequiresPasswordChange = false,
                IsFirstLogin = false
            };
        }

        public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            var existingUser = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                throw new Exception("Email already registered");
            }

            // Generate a temporary password (e.g. TEMP-A8B9)
            var tempPassword = GenerateTemporaryPassword();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

            var user = new UserAccount
            {
                Email = request.Email,
                PasswordHash = passwordHash,
                RoleType = request.RoleType,
                AccountStatus = AccountStatus.Pending
            };

            _context.UserAccounts.Add(user);
            await _context.SaveChangesAsync();

            if (request.RoleType == RoleType.Occupant)
            {
                _context.Occupants.Add(new Occupant
                {
                    UserAccountId = user.Id,
                    FullName = request.FullName,
                    OccupantType = OccupantType.Tenant,
                    OccupantStatus = "Pending"
                });
            }
            else if (request.RoleType == RoleType.Technician)
            {
                _context.Technicians.Add(new Technician
                {
                    UserAccountId = user.Id,
                    FullName = request.FullName,
                    AvailabilityStatus = "Available"
                });
            }
            else if (request.RoleType == RoleType.PropertyManager)
            {
                _context.PropertyManagers.Add(new PropertyManager
                {
                    UserAccountId = user.Id,
                    FullName = request.FullName
                });
            }

            await _context.SaveChangesAsync();

            // Send Email
            var subject = "Welcome to Property Management System - Activation Required";
            var body = $@"
                <h3>Welcome {request.FullName}!</h3>
                <p>Your account has been created.</p>
                <p>Your temporary password is: <strong>{tempPassword}</strong></p>
                <p>Please log in using this temporary password. You will be required to set a new permanent password immediately.</p>";
            
            await _emailService.SendEmailAsync(user.Email, subject, body);

            return new RegisterResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Message = "Registration successful. Temporary password sent via email."
            };
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordRequestDto request)
        {
            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<VerifyIcResponseDto> VerifyIcAsync(string identificationNo)
        {
            var occupant = await _context.Occupants
                .Include(o => o.UserAccount)
                .FirstOrDefaultAsync(o => o.IdentificationNo == identificationNo && o.OccupantType == OccupantType.Owner);

            if (occupant == null || occupant.UserAccount == null)
            {
                return new VerifyIcResponseDto { Found = false };
            }

            // Generate an update token (short lived, specific to email updates)
            var updateToken = GenerateUpdateToken(occupant.UserAccount);

            return new VerifyIcResponseDto
            {
                Found = true,
                MaskedEmail = MaskEmail(occupant.UserAccount.Email),
                UpdateToken = updateToken
            };
        }

        public async Task UpdateEmailByIcAsync(string updateToken, string newEmail)
        {
            var userId = ValidateUpdateToken(updateToken);
            var user = await _context.UserAccounts.FindAsync(userId);
            if (user == null) throw new Exception("Invalid token or user not found.");

            // Check if new email is taken
            var existing = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == newEmail);
            if (existing != null && existing.Id != user.Id) throw new Exception("Email is already in use.");

            user.Email = newEmail;
            await _context.SaveChangesAsync();
        }

        public async Task<string> VerifyTempPasswordAsync(VerifyTempPasswordRequestDto request)
        {
            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.TemporaryPassword, user.PasswordHash))
            {
                throw new Exception("Invalid temporary password.");
            }
            if (user.AccountStatus != AccountStatus.Pending)
            {
                throw new Exception("Account is already active.");
            }

            return GenerateUpdateToken(user);
        }

        public async Task<LoginResponseDto> SetPasswordAsync(SetPasswordRequestDto request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                throw new Exception("Passwords do not match.");

            UserAccount? user = null;

            if (!string.IsNullOrEmpty(request.UpdateToken))
            {
                var userId = ValidateUpdateToken(request.UpdateToken);
                user = await _context.UserAccounts.FindAsync(userId);
            }
            else
            {
                user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == request.Email);
            }

            if (user == null) throw new Exception("User not found. Please try logging in again.");

            try 
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.AccountStatus = AccountStatus.Active;
                user.UpdatedAt = DateTime.UtcNow;
                user.LastLogin = DateTime.UtcNow;

                var saved = await _context.SaveChangesAsync();
                if (saved == 0)
                {
                    // This shouldn't happen unless EF core decides no fields were modified, but let's be safe.
                    // Or if there was a concurrency issue.
                    Console.WriteLine($"[SetPasswordAsync] Warning: SaveChangesAsync returned 0 for user {user.Email}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SetPasswordAsync] Database Save Error: {ex.Message}");
                throw new Exception("A database error occurred while saving your new password. Please try again.");
            }

            var token = GenerateJwtToken(user);
            string fullName = await GetUserFullName(user);
            string? occupantType = null;
            if (user.RoleType == RoleType.Occupant)
            {
                var occ = await _context.Occupants.FirstOrDefaultAsync(o => o.UserAccountId == user.Id);
                occupantType = occ?.OccupantType.ToString();
            }

            return new LoginResponseDto
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                Role = user.RoleType.ToString(),
                FullName = fullName,
                AccountStatus = user.AccountStatus.ToString(),
                OccupantType = occupantType,
                RequiresPasswordChange = false
            };
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                // Security: don't reveal if email exists, just return silently
                return;
            }

            var tempPassword = GenerateTemporaryPassword();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            user.AccountStatus = AccountStatus.Pending; // Force password change on next login
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _emailService.SendPasswordResetEmailAsync(user.Email, tempPassword);
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

        private string GenerateUpdateToken(UserAccount user)
        {
            // Reusing JWT mechanism but with a different claim/scope
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "fallback-secret-key-too-short");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("scope", "password-update")
            };

            var key = new SymmetricSecurityKey(secretKey);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddMinutes(15),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private long ValidateUpdateToken(string token)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "");
            var handler = new JwtSecurityTokenHandler();
            
            try
            {
                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var scope = jwtToken.Claims.FirstOrDefault(x => x.Type == "scope")?.Value;
                if (scope != "password-update") throw new Exception("Invalid token scope.");

                return long.Parse(jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
            }
            catch
            {
                throw new Exception("Invalid or expired update token.");
            }
        }

        private string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@")) return email;
            var parts = email.Split('@');
            if (parts[0].Length <= 1) return email;
            return parts[0][0] + new string('*', parts[0].Length - 1) + "@" + parts[1];
        }

        private string GenerateTemporaryPassword()
        {
            var bytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            var hex = BitConverter.ToString(bytes).Replace("-", "");
            return $"TEMP-{hex}";
        }

        private async Task<string> GetUserFullName(UserAccount user)
        {
            if (user.RoleType == RoleType.Occupant)
            {
                var occupant = await _context.Occupants.FirstOrDefaultAsync(o => o.UserAccountId == user.Id);
                return occupant?.FullName ?? "Occupant";
            }
            else if (user.RoleType == RoleType.Technician)
            {
                var technician = await _context.Technicians.FirstOrDefaultAsync(t => t.UserAccountId == user.Id);
                return technician?.FullName ?? "Technician";
            }
            else if (user.RoleType == RoleType.PropertyManager)
            {
                var manager = await _context.PropertyManagers.FirstOrDefaultAsync(pm => pm.UserAccountId == user.Id);
                return manager?.FullName ?? "Property Manager";
            }
            return "User";
        }
    }
}