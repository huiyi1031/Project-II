using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models.Entities;
using PropertyManagement.API.Models.Enums;
using PropertyManagement.API.Services;
using System.Security.Claims;

namespace PropertyManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OccupantsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public OccupantsController(AppDbContext context, IEmailService emailService)
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

        // --- My Profile ---
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            var occupant = await _context.Occupants.FirstOrDefaultAsync(o => o.UserAccountId == userId);
            if (occupant == null) return NotFound("Occupant profile not found");
            return Ok(occupant);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] Occupant request)
        {
            var userId = GetCurrentUserId();
            var occupant = await _context.Occupants.FirstOrDefaultAsync(o => o.UserAccountId == userId);
            if (occupant == null) return NotFound();

            occupant.FullName = request.FullName;
            occupant.ContactNumber = request.ContactNumber;
            occupant.Gender = request.Gender;
            
            await _context.SaveChangesAsync();
            return Ok(occupant);
        }

        // --- Family Members ---
        [HttpGet("me/family")]
        public async Task<IActionResult> GetMyFamilyMembers()
        {
            var userId = GetCurrentUserId();
            var myOccupant = await _context.Occupants.FirstOrDefaultAsync(o => o.UserAccountId == userId);
            if (myOccupant == null) return NotFound();

            // Filter by ParentOccupantId to ensure owners only see their own family members
            var family = await _context.Occupants
                .Include(o => o.UserAccount)
                .Where(o => o.OccupantType == OccupantType.Resident && o.ParentOccupantId == myOccupant.Id && !o.IsDeleted && (o.UserAccount == null || !o.UserAccount.IsDeleted))
                .ToListAsync();

            var result = family.Select(f => new {
                occupantID = f.Id,
                fullName = f.FullName,
                email = f.UserAccount?.Email,
                contactNumber = f.ContactNumber,
                gender = f.Gender,
                occupantStatus = f.OccupantStatus,
                dateOfBirth = "1990-01-01", // Mock DOB as it's not in DB
                relationship = "Family Member"
            });

            return Ok(result);
        }

        [HttpPost("me/family")]
        public async Task<IActionResult> AddFamilyMember([FromBody] AddFamilyMemberDto request)
        {
            var existingUser = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);
            if (existingUser != null) return BadRequest(new { message = "Email already registered." });

            var tempPassword = $"TEMP-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
            
            var userAccount = new UserAccount
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                RoleType = RoleType.Occupant,
                AccountStatus = AccountStatus.Pending
            };
            
            _context.UserAccounts.Add(userAccount);
            await _context.SaveChangesAsync();

            var occupant = new Occupant
            {
                UserAccountId = userAccount.Id,
                FullName = request.FullName,
                ContactNumber = request.ContactNumber,
                Gender = request.Gender,
                DateOfBirth = DateTime.TryParse(request.DateOfBirth, out var dob) ? DateTime.SpecifyKind(dob, DateTimeKind.Utc) : null,
                OccupantType = OccupantType.Resident,
                OccupantStatus = "Active",
                ParentOccupantId = GetCurrentUserId() // temporarily use UserAccountId to lookup the owner occupant ID
            };
            
            var owner = await _context.Occupants.FirstOrDefaultAsync(o => o.UserAccountId == occupant.ParentOccupantId);
            if (owner != null) occupant.ParentOccupantId = owner.Id;
            
            _context.Occupants.Add(occupant);
            await _context.SaveChangesAsync();

            // Send real email via SMTP
            await _emailService.SendActivationEmailAsync(
                request.Email,
                request.FullName,
                tempPassword,
                "Family Member"
            );

            return Ok(new { message = "Family member added successfully", tempPassword = tempPassword });
        }

        [HttpDelete("me/family/{id}")]
        public async Task<IActionResult> RemoveFamilyMember(long id)
        {
            var occupant = await _context.Occupants.Include(o => o.UserAccount).FirstOrDefaultAsync(o => o.Id == id && o.OccupantType == OccupantType.Resident);
            if (occupant == null) return NotFound("Family member not found");

            occupant.IsDeleted = true;
            occupant.UpdatedAt = DateTime.UtcNow;

            if (occupant.UserAccount != null)
            {
                occupant.UserAccount.IsDeleted = true;
                occupant.UserAccount.UpdatedAt = DateTime.UtcNow;
                occupant.UserAccount.AccountStatus = AccountStatus.Suspended;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Family member removed successfully" });
        }

        // --- Tenants ---
        [HttpGet("me/tenants")]
        public async Task<IActionResult> GetMyTenants()
        {
            var userId = GetCurrentUserId();
            var myOccupant = await _context.Occupants.FirstOrDefaultAsync(o => o.UserAccountId == userId);
            if (myOccupant == null) return NotFound();

            var tenants = await _context.Occupants
                .Include(o => o.UserAccount)
                .Include(o => o.Contracts)
                    .ThenInclude(c => c.PropertyUnit)
                .Where(o => o.OccupantType == OccupantType.Tenant && o.ParentOccupantId == myOccupant.Id && !o.IsDeleted && (o.UserAccount == null || !o.UserAccount.IsDeleted))
                .ToListAsync();

            var result = tenants.Select(t => {
                var contract = t.Contracts.FirstOrDefault(c => c.ContractType == "Tenancy");
                return new {
                    occupantID = t.Id,
                    fullName = t.FullName,
                    email = t.UserAccount?.Email,
                    contactNumber = t.ContactNumber,
                    status = t.OccupantStatus,
                    unitNumber = contract?.PropertyUnit?.UnitNumber ?? "Unknown Unit",
                    startDate = contract?.StartDate.ToString("yyyy-MM-dd"),
                    endDate = contract?.EndDate?.ToString("yyyy-MM-dd")
                };
            });

            return Ok(result);
        }

        [HttpPost("me/tenants")]
        public async Task<IActionResult> AddTenant([FromBody] AddTenantDto request)
        {
            var existingUser = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);
            if (existingUser != null) return BadRequest(new { message = "Email already registered." });

            var tempPassword = $"TEMP-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
            
            var userAccount = new UserAccount
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                RoleType = RoleType.Occupant,
                AccountStatus = AccountStatus.Pending
            };
            
            _context.UserAccounts.Add(userAccount);
            await _context.SaveChangesAsync();

            var occupant = new Occupant
            {
                UserAccountId = userAccount.Id,
                FullName = request.FullName,
                ContactNumber = request.ContactNumber,
                Gender = request.Gender,
                DateOfBirth = DateTime.TryParse(request.DateOfBirth, out var tdob) ? DateTime.SpecifyKind(tdob, DateTimeKind.Utc) : null,
                OccupantType = OccupantType.Tenant,
                OccupantStatus = "Active",
                ParentOccupantId = GetCurrentUserId()
            };
            
            var owner = await _context.Occupants.FirstOrDefaultAsync(o => o.UserAccountId == occupant.ParentOccupantId);
            if (owner != null) occupant.ParentOccupantId = owner.Id;
            
            _context.Occupants.Add(occupant);
            await _context.SaveChangesAsync();

            var contract = new Contract
            {
                OccupantId = occupant.Id,
                UnitId = request.UnitId,
                ContractType = "Tenancy",
                StartDate = DateTime.TryParse(request.StartDate, out var sDate) ? DateTime.SpecifyKind(sDate, DateTimeKind.Utc) : DateTime.UtcNow,
                EndDate = DateTime.TryParse(request.EndDate, out var eDate) ? DateTime.SpecifyKind(eDate, DateTimeKind.Utc) : null,
                IsPrimaryOccupant = true,
                Status = "Active"
            };
            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();

            // Send real email via SMTP
            await _emailService.SendActivationEmailAsync(
                request.Email,
                request.FullName,
                tempPassword,
                "Tenant"
            );

            return Ok(new { message = "Tenant added successfully", tempPassword = tempPassword });
        }

        [HttpDelete("me/tenants/{id}")]
        public async Task<IActionResult> RemoveTenant(long id)
        {
            var occupant = await _context.Occupants.Include(o => o.UserAccount).FirstOrDefaultAsync(o => o.Id == id && o.OccupantType == OccupantType.Tenant);
            if (occupant == null) return NotFound("Tenant not found");

            occupant.IsDeleted = true;
            occupant.UpdatedAt = DateTime.UtcNow;

            if (occupant.UserAccount != null)
            {
                occupant.UserAccount.IsDeleted = true;
                occupant.UserAccount.UpdatedAt = DateTime.UtcNow;
                occupant.UserAccount.AccountStatus = AccountStatus.Suspended;
            }

            var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.OccupantId == id && c.Status == "Active");
            if (contract != null)
            {
                contract.Status = "Terminated";
                contract.EndDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Tenant removed successfully" });
        }
    }

    public class AddFamilyMemberDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
    }

    public class AddTenantDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public long UnitId { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
    }
}
