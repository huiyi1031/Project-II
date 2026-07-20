using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models.Entities;

namespace PropertyManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PropertyUnitsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PropertyUnitsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/PropertyUnits
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var units = await _context.PropertyUnits
                .Where(u => !u.IsDeleted)
                .OrderBy(u => u.Block)
                .ThenBy(u => u.UnitNumber)
                .Select(u => new
                {
                    unitID   = u.Id,
                    unitNumber = u.UnitNumber,
                    block    = u.Block,
                    floor    = u.FloorLevel,
                    unitType = u.UnitType,
                    status   = u.Status
                })
                .ToListAsync();

            return Ok(units);
        }

        // GET: api/PropertyUnits/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var unit = await _context.PropertyUnits.FindAsync(id);
            if (unit == null || unit.IsDeleted) return NotFound();
            return Ok(unit);
        }

        // GET: api/PropertyUnits/my/headcount
        [HttpGet("my/headcount")]
        public async Task<IActionResult> GetMyHeadcount()
        {
            // Placeholder – returns a simple summary
            var units = await _context.PropertyUnits
                .Where(u => !u.IsDeleted)
                .Select(u => new { u.UnitNumber, u.CurrentOccupants, u.MaxOccupants })
                .ToListAsync();
            return Ok(units);
        }

        // POST: api/PropertyUnits
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PropertyUnit unit)
        {
            _context.PropertyUnits.Add(unit);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = unit.Id }, unit);
        }

        // PUT: api/PropertyUnits/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] PropertyUnit updated)
        {
            var unit = await _context.PropertyUnits.FindAsync(id);
            if (unit == null || unit.IsDeleted) return NotFound();

            unit.UnitNumber     = updated.UnitNumber;
            unit.Block          = updated.Block;
            unit.FloorLevel     = updated.FloorLevel;
            unit.UnitType       = updated.UnitType;
            unit.Status         = updated.Status;
            unit.AreaSqft       = updated.AreaSqft;
            unit.MaxOccupants   = updated.MaxOccupants;
            unit.CurrentOccupants = updated.CurrentOccupants;
            unit.UpdatedAt      = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(unit);
        }
    }
}
