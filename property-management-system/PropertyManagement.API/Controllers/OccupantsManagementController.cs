using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models.Entities;
using PropertyManagement.API.Models.Enums;
using System.Text.RegularExpressions;

namespace PropertyManagement.API.Controllers
{
    [Route("api/Occupants")]
    [ApiController]
    [Authorize]
    public class OccupantsManagementController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OccupantsManagementController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetOccupants(
            [FromQuery] string? roleType,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 50);

            var query = _context.Occupants
                .AsNoTracking()
                .Include(o => o.UserAccount)
                .Include(o => o.Contracts)
                    .ThenInclude(c => c.PropertyUnit)
                .Where(o => !o.IsDeleted && (o.UserAccount == null || !o.UserAccount.IsDeleted));

            if (Enum.TryParse<OccupantType>(roleType, true, out var parsedRole))
            {
                query = query.Where(o => o.OccupantType == parsedRole);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                query = query.Where(o =>
                    o.FullName.ToLower().Contains(keyword) ||
                    (o.IdentificationNo != null && o.IdentificationNo.ToLower().Contains(keyword)) ||
                    (o.ContactNumber != null && o.ContactNumber.ToLower().Contains(keyword)) ||
                    (o.UserAccount != null && o.UserAccount.Email.ToLower().Contains(keyword)) ||
                    o.Contracts.Any(c => c.PropertyUnit != null && c.PropertyUnit.UnitNumber.ToLower().Contains(keyword)));
            }

            var totalItems = await query.CountAsync();
            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page > totalPages) page = totalPages;

            var occupants = await query
                .OrderBy(o => o.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    occupantID = o.Id,
                    accountID = o.UserAccountId,
                    fullName = o.FullName,
                    identificationNo = o.IdentificationNo,
                    contactNumber = o.ContactNumber,
                    gender = o.Gender,
                    age = o.Age,
                    occupantType = o.OccupantType.ToString(),
                    occupantStatus = o.OccupantStatus,
                    email = o.UserAccount != null ? o.UserAccount.Email : null,
                    unitID = o.Contracts.Where(c => c.Status == "Active").Select(c => (long?)c.UnitId).FirstOrDefault(),
                    unitNumber = o.Contracts.Where(c => c.Status == "Active").Select(c => c.PropertyUnit != null ? c.PropertyUnit.UnitNumber : null).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(new
            {
                items = occupants,
                page,
                pageSize,
                totalItems,
                totalPages
            });
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetOccupant(long id)
        {
            var occupant = await _context.Occupants
                .AsNoTracking()
                .Include(o => o.UserAccount)
                .Include(o => o.Contracts)
                    .ThenInclude(c => c.PropertyUnit)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

            if (occupant is null) return NotFound(new { message = "Occupant was not found." });

            var contract = occupant.Contracts.FirstOrDefault(c => c.Status == "Active");
            return Ok(ToResponse(occupant, contract));
        }

        [HttpPost]
        public async Task<IActionResult> CreateOccupant([FromBody] ManagerOccupantRequest request)
        {
            var validation = ValidateRequest(request);
            if (validation.Count > 0) return BadRequest(new ValidationProblemDetails(validation));

            if (await HasDuplicateAsync(request, null))
                return Conflict(new { message = "Identification No, contact number, or email already exists." });

            var now = DateTime.UtcNow;
            var user = new UserAccount
            {
                Email = string.IsNullOrWhiteSpace(request.Email) ? GenerateLocalEmail(request.FullName) : request.Email.Trim().ToLowerInvariant(),
                PasswordHash = "AutoCreatedOccupant",
                RoleType = RoleType.Occupant,
                AccountStatus = AccountStatus.Active,
                CreatedAt = now
            };
            _context.UserAccounts.Add(user);
            await _context.SaveChangesAsync();

            var occupant = new Occupant
            {
                UserAccountId = user.Id,
                FullName = request.FullName.Trim(),
                IdentificationNo = request.IdentificationNo.Trim(),
                ContactNumber = request.ContactNumber.Trim(),
                OccupantType = Enum.Parse<OccupantType>(request.OccupantType, true),
                OccupantStatus = "Active",
                CreatedAt = now
            };
            _context.Occupants.Add(occupant);
            await _context.SaveChangesAsync();

            var unit = await GetOrCreateUnitAsync(request.UnitNumber, now);
            var contract = await UpsertContractAsync(occupant.Id, unit.Id, occupant.OccupantType, now);
            await _context.SaveChangesAsync();

            return Ok(ToResponse(occupant, contract, user, unit));
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateOccupant(long id, [FromBody] ManagerOccupantRequest request)
        {
            var occupant = await _context.Occupants.Include(o => o.UserAccount).FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
            if (occupant is null) return NotFound(new { message = "Occupant was not found." });

            var validation = ValidateRequest(request);
            if (validation.Count > 0) return BadRequest(new ValidationProblemDetails(validation));

            if (await HasDuplicateAsync(request, id))
                return Conflict(new { message = "Identification No, contact number, or email already exists." });

            var now = DateTime.UtcNow;
            occupant.FullName = request.FullName.Trim();
            occupant.IdentificationNo = request.IdentificationNo.Trim();
            occupant.ContactNumber = request.ContactNumber.Trim();
            occupant.OccupantType = Enum.Parse<OccupantType>(request.OccupantType, true);
            occupant.UpdatedAt = now;

            if (occupant.UserAccount != null && !string.IsNullOrWhiteSpace(request.Email))
            {
                occupant.UserAccount.Email = request.Email.Trim().ToLowerInvariant();
                occupant.UserAccount.UpdatedAt = now;
            }

            var unit = await GetOrCreateUnitAsync(request.UnitNumber, now);
            var contract = await UpsertContractAsync(occupant.Id, unit.Id, occupant.OccupantType, now);
            await _context.SaveChangesAsync();

            return Ok(ToResponse(occupant, contract, occupant.UserAccount, unit));
        }

        [HttpPatch("{id:long}/deactivate")]
        public async Task<IActionResult> DeactivateOccupant(long id)
        {
            var occupant = await _context.Occupants.Include(o => o.UserAccount).FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
            if (occupant is null) return NotFound(new { message = "Occupant was not found." });

            occupant.OccupantStatus = "Inactive";
            occupant.UpdatedAt = DateTime.UtcNow;
            if (occupant.UserAccount != null) occupant.UserAccount.AccountStatus = AccountStatus.Suspended;
            await _context.SaveChangesAsync();

            return Ok(new { occupantID = occupant.Id, occupantStatus = occupant.OccupantStatus });
        }

        [HttpPatch("{id:long}/activate")]
        public async Task<IActionResult> ActivateOccupant(long id)
        {
            var occupant = await _context.Occupants.Include(o => o.UserAccount).FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
            if (occupant is null) return NotFound(new { message = "Occupant was not found." });

            occupant.OccupantStatus = "Active";
            occupant.UpdatedAt = DateTime.UtcNow;
            if (occupant.UserAccount != null) occupant.UserAccount.AccountStatus = AccountStatus.Active;
            await _context.SaveChangesAsync();

            return Ok(new { occupantID = occupant.Id, occupantStatus = occupant.OccupantStatus });
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteOccupant(long id)
        {
            var occupant = await _context.Occupants
                .Include(o => o.UserAccount)
                .Include(o => o.Contracts)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

            if (occupant is null) return NotFound(new { message = "Occupant was not found." });

            var now = DateTime.UtcNow;
            occupant.IsDeleted = true;
            occupant.OccupantStatus = "Deleted";
            occupant.UpdatedAt = now;

            if (occupant.UserAccount != null)
            {
                occupant.UserAccount.IsDeleted = true;
                occupant.UserAccount.AccountStatus = AccountStatus.Suspended;
                occupant.UserAccount.UpdatedAt = now;
            }

            foreach (var contract in occupant.Contracts.Where(c => !c.IsDeleted))
            {
                contract.IsDeleted = true;
                contract.Status = "Deleted";
                contract.EndDate ??= now;
                contract.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();

            return Ok(new { occupantID = occupant.Id, message = "Occupant record deleted successfully." });
        }

        private static Dictionary<string, string[]> ValidateRequest(ManagerOccupantRequest request)
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(request.FullName))
                errors["fullName"] = new[] { "Name is required." };
            else if (!Regex.IsMatch(request.FullName.Trim(), "^[A-Za-z ]+$"))
                errors["fullName"] = new[] { "Name can contain alphabets and spaces only." };

            if (string.IsNullOrWhiteSpace(request.IdentificationNo) || !Regex.IsMatch(request.IdentificationNo.Trim(), @"^\d{6}-\d{2}-\d{4}$"))
                errors["identificationNo"] = new[] { "Identification No format must be xxxxxx-xx-xxxx using numbers only." };

            if (string.IsNullOrWhiteSpace(request.ContactNumber) || !Regex.IsMatch(request.ContactNumber.Trim(), @"^01\d-\d{7}$"))
                errors["contactNumber"] = new[] { "Contact No format must be 01x-xxxxxxx using numbers only." };

            if (!string.IsNullOrWhiteSpace(request.Email) && !Regex.IsMatch(request.Email.Trim(), @"^[^@\s]+@[^@\s]+\.com$"))
                errors["email"] = new[] { "Email must be in valid @xxx.com format." };

            if (string.IsNullOrWhiteSpace(request.UnitNumber) || !Regex.IsMatch(request.UnitNumber.Trim(), "^[A-C]-(0[1-9]|1[0-9]|20)-0[1-9]$", RegexOptions.IgnoreCase))
                errors["unitNumber"] = new[] { "Property Unit format must be A-C, 01-20, 01-09. Example: A-10-09." };

            if (!Enum.TryParse<OccupantType>(request.OccupantType, true, out _))
                errors["occupantType"] = new[] { "Role Type is invalid." };

            return errors;
        }

        private async Task<bool> HasDuplicateAsync(ManagerOccupantRequest request, long? currentOccupantId)
        {
            var identificationNo = request.IdentificationNo.Trim();
            var contactNumber = request.ContactNumber.Trim();
            var email = request.Email?.Trim().ToLowerInvariant();

            var duplicateOccupant = await _context.Occupants.AnyAsync(o =>
                !o.IsDeleted &&
                (!currentOccupantId.HasValue || o.Id != currentOccupantId.Value) &&
                (o.IdentificationNo == identificationNo || o.ContactNumber == contactNumber));

            if (duplicateOccupant) return true;
            if (string.IsNullOrWhiteSpace(email)) return false;

            return await _context.UserAccounts.AnyAsync(u =>
                !u.IsDeleted &&
                u.Email.ToLower() == email &&
                (!currentOccupantId.HasValue || u.Occupant == null || u.Occupant.Id != currentOccupantId.Value));
        }

        private async Task<PropertyUnit> GetOrCreateUnitAsync(string unitNumber, DateTime now)
        {
            var normalized = unitNumber.Trim().ToUpperInvariant();
            var existing = await _context.PropertyUnits.FirstOrDefaultAsync(u => !u.IsDeleted && u.UnitNumber.ToUpper() == normalized);
            if (existing is not null) return existing;

            var propertyId = await _context.Properties.Where(p => !p.IsDeleted).OrderBy(p => p.Id).Select(p => p.Id).FirstOrDefaultAsync();
            if (propertyId <= 0)
            {
                var property = new Property { PropertyName = "Default Property", PropertyType = "Residential", CreatedAt = now };
                _context.Properties.Add(property);
                await _context.SaveChangesAsync();
                propertyId = property.Id;
            }

            var parts = normalized.Split('-');
            var unit = new PropertyUnit
            {
                PropertyId = propertyId,
                UnitNumber = normalized,
                Block = parts[0],
                FloorLevel = parts[1],
                Status = "Occupied",
                UnitType = "Residential",
                CurrentOccupants = 0,
                MaxOccupants = 4,
                CreatedAt = now
            };
            _context.PropertyUnits.Add(unit);
            await _context.SaveChangesAsync();
            return unit;
        }

        private async Task<Contract> UpsertContractAsync(long occupantId, long unitId, OccupantType occupantType, DateTime now)
        {
            var existing = await _context.Contracts.FirstOrDefaultAsync(c => c.OccupantId == occupantId && c.Status == "Active");
            if (existing is not null)
            {
                existing.UnitId = unitId;
                existing.ContractType = occupantType == OccupantType.Owner ? "Ownership" : "Tenancy";
                existing.UpdatedAt = now;
                return existing;
            }

            var contract = new Contract
            {
                OccupantId = occupantId,
                UnitId = unitId,
                ContractType = occupantType == OccupantType.Owner ? "Ownership" : "Tenancy",
                StartDate = now,
                IsPrimaryOccupant = true,
                Status = "Active",
                CreatedAt = now
            };
            _context.Contracts.Add(contract);
            return contract;
        }

        private static object ToResponse(Occupant occupant, Contract? contract, UserAccount? user = null, PropertyUnit? unit = null)
        {
            return new
            {
                occupantID = occupant.Id,
                accountID = occupant.UserAccountId,
                fullName = occupant.FullName,
                identificationNo = occupant.IdentificationNo,
                contactNumber = occupant.ContactNumber,
                gender = occupant.Gender,
                age = occupant.Age,
                occupantType = occupant.OccupantType.ToString(),
                occupantStatus = occupant.OccupantStatus,
                email = user?.Email ?? occupant.UserAccount?.Email,
                unitID = contract?.UnitId,
                unitNumber = unit?.UnitNumber ?? contract?.PropertyUnit?.UnitNumber
            };
        }

        private static string GenerateLocalEmail(string fullName)
        {
            var prefix = new string(fullName.ToLowerInvariant().Where(char.IsLetter).ToArray());
            if (string.IsNullOrWhiteSpace(prefix)) prefix = "occupant";
            return $"{prefix}.{DateTime.UtcNow:yyyyMMddHHmmssfff}@local.pms";
        }
    }

    public class ManagerOccupantRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string IdentificationNo { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string UnitNumber { get; set; } = string.Empty;
        public string OccupantType { get; set; } = "Tenant";
    }
}

