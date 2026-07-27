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
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = GetCurrentUserId();
            var occupant = await _context.Occupants
                .Include(o => o.UserAccount)
                .FirstOrDefaultAsync(o => o.UserAccountId == userId);
            if (occupant == null) return NotFound();

            occupant.FullName = request.FullName;
            occupant.ContactNumber = request.ContactNumber;
            occupant.Gender = request.Gender != null ? (request.Gender.Trim().ToUpper().StartsWith("M") ? "M" : (request.Gender.Trim().ToUpper().StartsWith("F") ? "F" : (request.Gender.Length > 0 ? request.Gender.Substring(0, 1) : null))) : null;

            // Update email on UserAccount if provided and different
            if (!string.IsNullOrWhiteSpace(request.Email) && occupant.UserAccount != null
                && !string.Equals(occupant.UserAccount.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var emailTaken = await _context.UserAccounts
                    .AnyAsync(u => u.Email == request.Email.Trim().ToLower() && u.Id != userId && !u.IsDeleted);
                if (emailTaken) return BadRequest(new { message = "This email is already in use by another account." });
                occupant.UserAccount.Email = request.Email.Trim().ToLower();
            }

            await _context.SaveChangesAsync();
            return Ok(new {
                fullName      = occupant.FullName,
                contactNumber = occupant.ContactNumber,
                gender        = occupant.Gender,
                email         = occupant.UserAccount?.Email
            });
        }

        // --- My Contracts (self-service) ---
        [HttpGet("me/contracts")]
        public async Task<IActionResult> GetMyContracts()
        {
            var userId = GetCurrentUserId();
            var myOccupant = await _context.Occupants.FirstOrDefaultAsync(o => o.UserAccountId == userId);
            if (myOccupant == null) return NotFound("Occupant not found");

            var contracts = await _context.Contracts
                .Include(c => c.PropertyUnit)
                    .ThenInclude(u => u!.Property)
                .Where(c => c.OccupantId == myOccupant.Id && c.Status == "Active")
                .ToListAsync();

            var result = contracts.Select(c => new {
                contractID    = c.Id,
                contractType  = c.ContractType,
                status        = c.Status,
                startDate     = c.StartDate.ToString("yyyy-MM-dd"),
                endDate       = c.EndDate?.ToString("yyyy-MM-dd"),
                isPrimary     = c.IsPrimaryOccupant,
                unitID        = c.UnitId,
                unitNumber    = c.PropertyUnit?.UnitNumber,
                block         = c.PropertyUnit?.Block,
                floorLevel    = c.PropertyUnit?.FloorLevel,
                unitType      = c.PropertyUnit?.UnitType,
                areaSqft      = c.PropertyUnit?.AreaSqft,
                bedrooms      = c.PropertyUnit?.Bedrooms,
                bathrooms     = c.PropertyUnit?.Bathrooms,
                unitStatus    = c.PropertyUnit?.Status,
                propertyName  = c.PropertyUnit?.Property?.PropertyName ?? "BlueBay Residence"
            });

            return Ok(result);
        }

        // --- Owner's Units (for tenant dropdown when adding new tenant) ---
        [HttpGet("me/owner-units")]
        public async Task<IActionResult> GetMyOwnerUnits()
        {
            var userId = GetCurrentUserId();
            var myOccupant = await _context.Occupants.FirstOrDefaultAsync(o => o.UserAccountId == userId);
            if (myOccupant == null) return NotFound("Occupant not found");

            // Owner's own contracts (Ownership type)
            var ownerContracts = await _context.Contracts
                .Include(c => c.PropertyUnit)
                .Where(c => c.OccupantId == myOccupant.Id && c.ContractType == "Ownership" && c.Status == "Active")
                .ToListAsync();

            var result = ownerContracts.Select(c => new {
                unitId     = c.UnitId,
                unitNumber = c.PropertyUnit?.UnitNumber ?? $"Unit {c.UnitId}"
            });

            return Ok(result);
        }

        // --- Get My Owner (for tenant/family member to see their property owner) ---
        [HttpGet("me/owner")]
        public async Task<IActionResult> GetMyOwner()
        {
            var userId = GetCurrentUserId();
            var myOccupant = await _context.Occupants
                .Include(o => o.UserAccount)
                .FirstOrDefaultAsync(o => o.UserAccountId == userId);
            if (myOccupant == null) return NotFound("Occupant not found");

            // Find the owner: ParentOccupantId points to the owner's occupant record
            if (myOccupant.ParentOccupantId == null)
                return NotFound("No owner linked to this account");

            var owner = await _context.Occupants
                .Include(o => o.UserAccount)
                .FirstOrDefaultAsync(o => o.Id == myOccupant.ParentOccupantId);

            if (owner == null) return NotFound("Owner record not found");

            return Ok(new {
                fullName      = owner.FullName,
                email         = owner.UserAccount?.Email,
                contactNumber = owner.ContactNumber
            });
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
                Gender = request.Gender != null ? (request.Gender.Trim().ToUpper().StartsWith("M") ? "M" : (request.Gender.Trim().ToUpper().StartsWith("F") ? "F" : (request.Gender.Length > 0 ? request.Gender.Substring(0, 1) : null))) : null,
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
                Gender = request.Gender != null ? (request.Gender.Trim().ToUpper().StartsWith("M") ? "M" : (request.Gender.Trim().ToUpper().StartsWith("F") ? "F" : (request.Gender.Length > 0 ? request.Gender.Substring(0, 1) : null))) : null,
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

    public class UpdateProfileRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
