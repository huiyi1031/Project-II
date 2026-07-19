using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models.Entities;

namespace PropertyManagement.API.Controllers
{
    // ─── DTOs ────────────────────────────────────────────────────────────────────

    public record CreatePropertyUnitDto(
        long PropertyId,
        string UnitNumber,
        string? FloorLevel,
        string? Block,
        string UnitType,
        decimal? AreaSqft,
        int? Bedrooms,
        int? Bathrooms,
        int MaxOccupants,
        string Status
    );

    public record UpdatePropertyUnitDto(
        string UnitNumber,
        string? FloorLevel,
        string? Block,
        string? UnitType,
        decimal? AreaSqft,
        int? Bedrooms,
        int? Bathrooms,
        int MaxOccupants,
        string? Status
    );

    // ─── Controller ──────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [ApiController]
    public class PropertyUnitsController : ControllerBase
    {
        private readonly AppDbContext _ctx;

        public PropertyUnitsController(AppDbContext ctx) => _ctx = ctx;

        // ── GET /api/PropertyUnits ───────────────────────────────────────────────
        // Supports: search, block, floorLevel, unitType, minSqft, maxSqft, status, propertyId
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? block,
            [FromQuery] string? floorLevel,
            [FromQuery] string? unitType,
            [FromQuery] decimal? minSqft,
            [FromQuery] decimal? maxSqft,
            [FromQuery] string? status,
            [FromQuery] long? propertyId)
        {
            var q = _ctx.PropertyUnits
                .Include(u => u.Property)
                .Where(u => !u.IsDeleted);

            if (propertyId.HasValue)
                q = q.Where(u => u.PropertyId == propertyId);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(u => u.UnitNumber.Contains(search) ||
                                 (u.Block != null && u.Block.Contains(search)));

            if (!string.IsNullOrWhiteSpace(block))
                q = q.Where(u => u.Block == block);

            if (!string.IsNullOrWhiteSpace(floorLevel))
                q = q.Where(u => u.FloorLevel == floorLevel);

            if (!string.IsNullOrWhiteSpace(unitType))
                q = q.Where(u => u.UnitType == unitType);

            if (minSqft.HasValue)
                q = q.Where(u => u.AreaSqft >= minSqft);

            if (maxSqft.HasValue)
                q = q.Where(u => u.AreaSqft <= maxSqft);

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
                q = q.Where(u => u.Status == status);

            var units = await q
                .OrderBy(u => u.Block)
                .ThenBy(u => u.FloorLevel)
                .ThenBy(u => u.UnitNumber)
                .Select(u => new
                {
                    unitId       = u.Id,
                    propertyId   = u.PropertyId,
                    propertyName = u.Property != null ? u.Property.PropertyName : null,
                    unitNumber   = u.UnitNumber,
                    floorLevel   = u.FloorLevel,
                    block        = u.Block,
                    unitType     = u.UnitType,
                    areaSqft     = u.AreaSqft,
                    bedrooms     = u.Bedrooms,
                    bathrooms    = u.Bathrooms,
                    maxOccupants     = u.MaxOccupants,
                    currentOccupants = u.CurrentOccupants,
                    status       = u.Status,
                    createdAt    = u.CreatedAt
                })
                .ToListAsync();

            return Ok(units);
        }

        // ── GET /api/PropertyUnits/{id} ──────────────────────────────────────────
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var u = await _ctx.PropertyUnits
                .Include(u => u.Property)
                .Include(u => u.Contracts.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.Occupant)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (u == null) return NotFound(new { message = "Unit not found." });

            var activeContracts = u.Contracts
                .Where(c => c.Status == "Active")
                .Select(c => new
                {
                    contractId   = c.Id,
                    occupantName = c.Occupant?.FullName,
                    occupantType = c.Occupant?.OccupantType.ToString(),
                    startDate    = c.StartDate,
                    endDate      = c.EndDate,
                    status       = c.Status
                });

            return Ok(new
            {
                unitId           = u.Id,
                propertyId       = u.PropertyId,
                propertyName     = u.Property?.PropertyName,
                unitNumber       = u.UnitNumber,
                floorLevel       = u.FloorLevel,
                block            = u.Block,
                unitType         = u.UnitType,
                areaSqft         = u.AreaSqft,
                bedrooms         = u.Bedrooms,
                bathrooms        = u.Bathrooms,
                maxOccupants     = u.MaxOccupants,
                currentOccupants = u.CurrentOccupants,
                status           = u.Status,
                createdAt        = u.CreatedAt,
                activeContracts
            });
        }

        // ── POST /api/PropertyUnits ──────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePropertyUnitDto dto)
        {
            // Check property exists
            var propertyExists = await _ctx.Properties.AnyAsync(p => p.Id == dto.PropertyId && !p.IsDeleted);
            if (!propertyExists)
                return BadRequest(new { message = "Property not found." });

            // Check for duplicate unit number within the same property
            var duplicate = await _ctx.PropertyUnits.AnyAsync(u =>
                u.PropertyId == dto.PropertyId &&
                u.UnitNumber == dto.UnitNumber &&
                !u.IsDeleted);

            if (duplicate)
                return Conflict(new { message = $"Unit '{dto.UnitNumber}' already exists in this property." });

            var unit = new PropertyUnit
            {
                PropertyId   = dto.PropertyId,
                UnitNumber   = dto.UnitNumber,
                FloorLevel   = dto.FloorLevel,
                Block        = dto.Block,
                UnitType     = dto.UnitType,
                AreaSqft     = dto.AreaSqft,
                Bedrooms     = dto.Bedrooms,
                Bathrooms    = dto.Bathrooms,
                MaxOccupants = dto.MaxOccupants,
                Status       = dto.Status ?? "Vacant",
                CurrentOccupants = 0
            };

            _ctx.PropertyUnits.Add(unit);
            await _ctx.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = unit.Id }, new
            {
                message    = $"Unit '{unit.UnitNumber}' created successfully.",
                unitId     = unit.Id,
                unitNumber = unit.UnitNumber,
                status     = unit.Status
            });
        }

        // ── PUT /api/PropertyUnits/{id} ──────────────────────────────────────────
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePropertyUnitDto dto)
        {
            var unit = await _ctx.PropertyUnits.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (unit == null) return NotFound(new { message = "Unit not found." });

            // Check for duplicate unit number (exclude self)
            if (unit.UnitNumber != dto.UnitNumber)
            {
                var duplicate = await _ctx.PropertyUnits.AnyAsync(u =>
                    u.PropertyId == unit.PropertyId &&
                    u.UnitNumber == dto.UnitNumber &&
                    u.Id != id &&
                    !u.IsDeleted);

                if (duplicate)
                    return Conflict(new { message = $"Unit '{dto.UnitNumber}' already exists in this property." });
            }

            unit.UnitNumber   = dto.UnitNumber;
            unit.FloorLevel   = dto.FloorLevel;
            unit.Block        = dto.Block;
            unit.UnitType     = dto.UnitType;
            unit.AreaSqft     = dto.AreaSqft;
            unit.Bedrooms     = dto.Bedrooms;
            unit.Bathrooms    = dto.Bathrooms;
            unit.MaxOccupants = dto.MaxOccupants;
            unit.Status       = dto.Status ?? unit.Status;
            unit.UpdatedAt    = DateTime.UtcNow;

            await _ctx.SaveChangesAsync();

            return Ok(new { message = $"Unit '{unit.UnitNumber}' updated successfully.", unitId = unit.Id });
        }

        // ── DELETE /api/PropertyUnits/{id} ───────────────────────────────────────
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var unit = await _ctx.PropertyUnits
                .Include(u => u.Contracts.Where(c => c.Status == "Active" && !c.IsDeleted))
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (unit == null) return NotFound(new { message = "Unit not found." });

            if (unit.Contracts.Any())
                return BadRequest(new { message = "Cannot delete a unit with active occupants. Please terminate all contracts first." });

            if (unit.Status == "Occupied")
                return BadRequest(new { message = "Cannot delete an occupied unit." });

            unit.IsDeleted = true;
            unit.UpdatedAt = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();

            return Ok(new { message = $"Unit '{unit.UnitNumber}' deleted successfully." });
        }

        // ── GET /api/PropertyUnits/filter-options ───────────────────────────────
        // Returns distinct blocks, floors for a given property (for dropdowns)
        [HttpGet("filter-options")]
        public async Task<IActionResult> GetFilterOptions([FromQuery] long? propertyId)
        {
            var q = _ctx.PropertyUnits.Where(u => !u.IsDeleted);
            if (propertyId.HasValue) q = q.Where(u => u.PropertyId == propertyId);

            var blocks  = await q.Where(u => u.Block != null).Select(u => u.Block!).Distinct().OrderBy(b => b).ToListAsync();
            var floors  = await q.Where(u => u.FloorLevel != null).Select(u => u.FloorLevel!).Distinct()
                                 .ToListAsync();

            // Natural sort floors: 1, 2, 3 ... 10, 11 ...
            var sortedFloors = floors.OrderBy(f => { int.TryParse(f, out int n); return n; }).ToList();

            return Ok(new { blocks, floors = sortedFloors });
        }

        // ── GET /api/PropertyUnits/my/headcount ─────────────────────────────────
        // Used by tenant (Occupant/Owner) to check their unit capacity
        [HttpGet("my/headcount")]
        public IActionResult GetMyHeadcount()
        {
            // This matches the existing endpoint used by the tenant side
            return Ok(new { message = "Use occupant service for tenant headcount." });
        }
    }
}
