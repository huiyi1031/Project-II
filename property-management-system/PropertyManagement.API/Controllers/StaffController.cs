using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models.Entities;
using PropertyManagement.API.Models.Enums;
using PropertyManagement.API.Models.DTOs;
using PropertyManagement.API.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;

namespace PropertyManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StaffController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public StaffController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
                return userId;
            return 0;
        }

        [HttpGet("service-types")]
        public async Task<IActionResult> GetServiceTypes()
        {
            var list = await _context.ServiceTypes
                .Select(st => new { id = st.Id, name = st.Name, description = st.Description })
                .ToListAsync();
            return Ok(list);
        }

        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { exists = false });
            var exists = await _context.UserAccounts
                .AnyAsync(u => u.Email == email.Trim().ToLower() && !u.IsDeleted);
            return Ok(new { exists });
        }

        private static string? NormalizeGender(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var g = input.Trim().ToUpper();
            if (g.StartsWith("M")) return "M";
            if (g.StartsWith("F")) return "F";
            return g.Length > 0 ? g.Substring(0, 1) : null;
        }

        private static string? FormatGender(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var g = input.Trim().ToUpper();
            if (g == "M") return "Male";
            if (g == "F") return "Female";
            return input;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStaff()
        {
            var currentUserId = GetCurrentUserId();
            long? myPropertyId = null;
            if (currentUserId > 0)
            {
                var myPm = await _context.PropertyManagers.AsNoTracking().FirstOrDefaultAsync(pm => pm.UserAccountId == currentUserId);
                if (myPm != null)
                {
                    myPropertyId = myPm.PropertyId;
                }
            }

            var query = _context.UserAccounts
                .Include(u => u.Technician).ThenInclude(t => t!.ServiceType)
                .Include(u => u.PropertyManager)
                .Where(u => u.RoleType == RoleType.Technician || u.RoleType == RoleType.PropertyManager);

            if (myPropertyId.HasValue)
            {
                query = query.Where(u => u.RoleType == RoleType.Technician || (u.RoleType == RoleType.PropertyManager && (u.PropertyManager == null || u.PropertyManager.PropertyId == myPropertyId.Value || u.Id == currentUserId)));
            }

            var users = await query.ToListAsync();

            var result = users.Select(u => new
            {
                accountID = u.Id,
                fullName = u.Technician?.FullName ?? u.PropertyManager?.FullName ?? u.Email,
                email = u.Email,
                contactNumber = u.Technician?.ContactNumber ?? u.PropertyManager?.ContactNumber,
                roleType = u.RoleType.ToString(),
                accountStatus = u.AccountStatus.ToString(),
                lastLogin = u.LastLogin,
                technicianID = u.Technician?.Id,
                serviceTypeName = u.Technician?.ServiceType?.Name,
                experienceLevel = u.Technician?.ExperienceLevel,
                availabilityStatus = u.Technician?.AvailabilityStatus ?? "Available",
                ranking = u.Technician?.Ranking,
                managerID = u.PropertyManager?.Id,
                position = u.PropertyManager?.Position,
                propertyId = u.PropertyManager?.PropertyId,
                gender = FormatGender(u.Technician?.Gender ?? u.PropertyManager?.Gender),
                dateOfBirth = u.Technician?.DateOfBirth ?? u.PropertyManager?.DateOfBirth,
                age = u.Technician?.Age ?? u.PropertyManager?.Age
            });

            return Ok(result);
        }

        [HttpGet("technicians")]
        public async Task<IActionResult> GetTechnicians()
        {
            var techs = await _context.Technicians
                .Include(t => t.ServiceType)
                .Include(t => t.UserAccount)
                .ToListAsync();

            var result = techs.Select(t => new
            {
                technicianID = t.Id,
                fullName = t.FullName,
                email = t.UserAccount?.Email ?? string.Empty,
                serviceTypeName = t.ServiceType?.Name ?? string.Empty,
                experienceLevel = t.ExperienceLevel,
                availabilityStatus = t.AvailabilityStatus,
                ranking = t.Ranking
            });

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStaffById(long id)
        {
            var u = await _context.UserAccounts
                .Include(a => a.Technician).ThenInclude(t => t!.ServiceType)
                .Include(a => a.PropertyManager)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (u == null) return NotFound("Staff account not found.");

            return Ok(new
            {
                accountID = u.Id,
                fullName = u.Technician?.FullName ?? u.PropertyManager?.FullName ?? u.Email,
                email = u.Email,
                contactNumber = u.Technician?.ContactNumber ?? u.PropertyManager?.ContactNumber,
                roleType = u.RoleType.ToString(),
                accountStatus = u.AccountStatus.ToString(),
                lastLogin = u.LastLogin,
                technicianID = u.Technician?.Id,
                serviceTypeName = u.Technician?.ServiceType?.Name,
                experienceLevel = u.Technician?.ExperienceLevel,
                availabilityStatus = u.Technician?.AvailabilityStatus ?? "Available",
                ranking = u.Technician?.Ranking,
                managerID = u.PropertyManager?.Id,
                position = u.PropertyManager?.Position,
                propertyId = u.PropertyManager?.PropertyId,
                gender = FormatGender(u.Technician?.Gender ?? u.PropertyManager?.Gender),
                dateOfBirth = u.Technician?.DateOfBirth ?? u.PropertyManager?.DateOfBirth,
                age = u.Technician?.Age ?? u.PropertyManager?.Age
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffDto dto)
        {
            if (await _context.UserAccounts.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
            {
                return BadRequest(new { message = "Email is already registered." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var tempPassword = $"TEMP-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
                var role = dto.RoleType.Equals("Technician", StringComparison.OrdinalIgnoreCase) ? RoleType.Technician : RoleType.PropertyManager;

                var user = new UserAccount
                {
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                    RoleType = role,
                    AccountStatus = AccountStatus.Active
                };

                _context.UserAccounts.Add(user);
                await _context.SaveChangesAsync();

                int? calculatedAge = dto.Age;
                if (dto.DateOfBirth.HasValue)
                {
                    var today = DateTime.Today;
                    var ageVal = today.Year - dto.DateOfBirth.Value.Year;
                    if (dto.DateOfBirth.Value.Date > today.AddYears(-ageVal)) ageVal--;
                    calculatedAge = ageVal;
                }

                long? assignedPropertyId = dto.PropertyId;
                var currentUserId = GetCurrentUserId();
                if (currentUserId > 0)
                {
                    var creatorPm = await _context.PropertyManagers.AsNoTracking().FirstOrDefaultAsync(pm => pm.UserAccountId == currentUserId);
                    if (creatorPm != null && creatorPm.PropertyId.HasValue)
                    {
                        assignedPropertyId = creatorPm.PropertyId.Value;
                    }
                }

                var normGender = NormalizeGender(dto.Gender);

                if (role == RoleType.Technician)
                {
                    var tech = new Technician
                    {
                        UserAccountId = user.Id,
                        FullName = dto.FullName,
                        ContactNumber = dto.ContactNumber,
                        Gender = normGender,
                        DateOfBirth = dto.DateOfBirth.HasValue ? DateTime.SpecifyKind(dto.DateOfBirth.Value, DateTimeKind.Utc) : null,
                        Age = calculatedAge,
                        ServiceTypeId = dto.ServiceTypeID,
                        ExperienceLevel = dto.ExperienceLevel ?? "Junior",
                        AvailabilityStatus = dto.AvailabilityStatus ?? "Available",
                        Ranking = dto.PriorityRank ?? 1
                    };
                    _context.Technicians.Add(tech);
                }
                else
                {
                    var pm = new PropertyManager
                    {
                        UserAccountId = user.Id,
                        FullName = dto.FullName,
                        ContactNumber = dto.ContactNumber,
                        Gender = normGender,
                        DateOfBirth = dto.DateOfBirth.HasValue ? DateTime.SpecifyKind(dto.DateOfBirth.Value, DateTimeKind.Utc) : null,
                        Age = calculatedAge,
                        PropertyId = assignedPropertyId,
                        Position = dto.Position ?? "Property Manager"
                    };
                    _context.PropertyManagers.Add(pm);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                try
                {
                    await _emailService.SendActivationEmailAsync(
                        dto.Email,
                        dto.FullName,
                        tempPassword,
                        role == RoleType.Technician ? "Technician" : "Property Manager"
                    );
                }
                catch { }

                return Ok(new
                {
                    message = "Staff account created successfully",
                    temporaryPassword = tempPassword,
                    tempPassword = tempPassword,
                    accountID = user.Id
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Failed to save staff record to database: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStaff(long id, [FromBody] UpdateStaffDto dto)
        {
            try
            {
                var u = await _context.UserAccounts
                    .Include(a => a.Technician)
                    .Include(a => a.PropertyManager)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (u == null) return NotFound("Staff account not found.");

                if (!string.IsNullOrWhiteSpace(dto.Email) && !string.Equals(u.Email, dto.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    u.Email = dto.Email.Trim();
                }

                int? calculatedAge = dto.Age;
                if (dto.DateOfBirth.HasValue)
                {
                    var today = DateTime.Today;
                    var ageVal = today.Year - dto.DateOfBirth.Value.Year;
                    if (dto.DateOfBirth.Value.Date > today.AddYears(-ageVal)) ageVal--;
                    calculatedAge = ageVal;
                }

                var normGender = NormalizeGender(dto.Gender);

                if (u.Technician == null && u.RoleType == RoleType.Technician)
                {
                    u.Technician = new Technician { UserAccountId = u.Id };
                    _context.Technicians.Add(u.Technician);
                }
                else if (u.PropertyManager == null && u.RoleType == RoleType.PropertyManager)
                {
                    u.PropertyManager = new PropertyManager { UserAccountId = u.Id };
                    _context.PropertyManagers.Add(u.PropertyManager);
                }

                if (u.Technician != null)
                {
                    if (dto.FullName != null) u.Technician.FullName = dto.FullName;
                    if (dto.ContactNumber != null) u.Technician.ContactNumber = dto.ContactNumber;
                    if (dto.Gender != null) u.Technician.Gender = normGender;
                    if (dto.DateOfBirth.HasValue) { u.Technician.DateOfBirth = DateTime.SpecifyKind(dto.DateOfBirth.Value, DateTimeKind.Utc); u.Technician.Age = calculatedAge; }
                    else if (dto.Age.HasValue) u.Technician.Age = dto.Age;
                    if (dto.ServiceTypeID.HasValue) u.Technician.ServiceTypeId = dto.ServiceTypeID.Value;
                    if (dto.ExperienceLevel != null) u.Technician.ExperienceLevel = dto.ExperienceLevel;
                    if (dto.AvailabilityStatus != null) u.Technician.AvailabilityStatus = dto.AvailabilityStatus;
                    if (dto.PriorityRank.HasValue) u.Technician.Ranking = dto.PriorityRank.Value;
                }
                else if (u.PropertyManager != null)
                {
                    if (dto.FullName != null) u.PropertyManager.FullName = dto.FullName;
                    if (dto.ContactNumber != null) u.PropertyManager.ContactNumber = dto.ContactNumber;
                    if (dto.Gender != null) u.PropertyManager.Gender = normGender;
                    if (dto.DateOfBirth.HasValue) { u.PropertyManager.DateOfBirth = DateTime.SpecifyKind(dto.DateOfBirth.Value, DateTimeKind.Utc); u.PropertyManager.Age = calculatedAge; }
                    else if (dto.Age.HasValue) u.PropertyManager.Age = dto.Age;
                    if (dto.PropertyId.HasValue) u.PropertyManager.PropertyId = dto.PropertyId;
                    if (dto.Position != null) u.PropertyManager.Position = dto.Position;
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Staff updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update staff record in database: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpPatch("{id}/deactivate")]
        public async Task<IActionResult> DeactivateStaff(long id, [FromBody] DeactivateStaffDto dto)
        {
            var u = await _context.UserAccounts.FirstOrDefaultAsync(a => a.Id == id);
            if (u == null) return NotFound("Staff account not found.");

            u.AccountStatus = AccountStatus.Suspended;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Staff account deactivated successfully." });
        }

        [HttpPatch("{id}/reactivate")]
        public async Task<IActionResult> ReactivateStaff(long id)
        {
            var u = await _context.UserAccounts.FirstOrDefaultAsync(a => a.Id == id);
            if (u == null) return NotFound("Staff account not found.");

            u.AccountStatus = AccountStatus.Active;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Staff account reactivated successfully." });
        }
    }
}
