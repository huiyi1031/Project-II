using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models.Entities;
using System.Security.Claims;

namespace PropertyManagement.API.Controllers
{
    // --- DTOs ---
    public record CreateProactiveWorkOrderDto(
        long AssetId,
        string Description,
        DateTime ScheduleDate
    );

    public record AssignTechnicianDto(
        long TechnicianId
    );

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WorkOrdersController : ControllerBase
    {
        private readonly AppDbContext _ctx;

        public WorkOrdersController(AppDbContext ctx) => _ctx = ctx;

        private async Task<long?> GetManagerPropertyIdAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var roleClaim = User.FindFirst(ClaimTypes.Role);

            if (userIdClaim != null && roleClaim != null && roleClaim.Value == "PropertyManager")
            {
                var userId = long.Parse(userIdClaim.Value);
                var pm = await _ctx.PropertyManagers.FirstOrDefaultAsync(m => m.UserAccountId == userId);
                return pm?.PropertyId;
            }
            return null;
        }

        // GET: api/WorkOrders
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var propertyId = await GetManagerPropertyIdAsync();
            var q = _ctx.WorkOrders
                .Include(w => w.MaintenancePlan).ThenInclude(p => p.Asset)
                .Include(w => w.MaintenanceRequest).ThenInclude(r => r.PropertyUnit)
                .Include(w => w.WorkAssignments).ThenInclude(a => a.Technician)
                .Where(w => !w.IsDeleted);

            if (propertyId.HasValue)
            {
                q = q.Where(w => (w.MaintenancePlan != null && w.MaintenancePlan.Asset != null && w.MaintenancePlan.Asset.PropertyId == propertyId) ||
                                 (w.MaintenanceRequest != null && w.MaintenanceRequest.PropertyUnit != null && w.MaintenanceRequest.PropertyUnit.PropertyId == propertyId));
            }

            var orders = await q
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => new
                {
                    id = w.Id,
                    workType = w.WorkType,
                    description = w.Description,
                    status = w.Status,
                    scheduleDate = w.ScheduleDate,
                    assetName = w.MaintenancePlan != null && w.MaintenancePlan.Asset != null ? w.MaintenancePlan.Asset.AssetName : null,
                    assetId = w.MaintenancePlan != null ? w.MaintenancePlan.AssetId : (long?)null,
                    unitNumber = w.MaintenanceRequest != null && w.MaintenanceRequest.PropertyUnit != null ? w.MaintenanceRequest.PropertyUnit.UnitNumber : null,
                    assignments = w.WorkAssignments.Where(a => !a.IsDeleted).Select(a => new
                    {
                        assignmentId = a.Id,
                        technicianId = a.TechnicianId,
                        technicianName = a.Technician != null ? a.Technician.FullName : null,
                        status = a.Status
                    })
                })
                .ToListAsync();

            return Ok(orders);
        }

        // POST: api/WorkOrders/proactive
        [HttpPost("proactive")]
        public async Task<IActionResult> CreateProactiveWorkOrder([FromBody] CreateProactiveWorkOrderDto dto)
        {
            var propertyId = await GetManagerPropertyIdAsync();
            var asset = await _ctx.Assets.FirstOrDefaultAsync(a => a.Id == dto.AssetId && !a.IsDeleted);
            if (asset == null || (propertyId.HasValue && asset.PropertyId != propertyId.Value))
            {
                return NotFound(new { message = "Asset not found or unauthorized." });
            }

            // Find or create Maintenance Plan
            var plan = await _ctx.MaintenancePlans.FirstOrDefaultAsync(p => p.AssetId == dto.AssetId && !p.IsDeleted);
            if (plan == null)
            {
                plan = new MaintenancePlan
                {
                    AssetId = dto.AssetId,
                    IntervalDays = asset.MaintenanceIntervalDays,
                    NextDueDate = asset.NextMaintenanceDueDate ?? DateTime.UtcNow,
                    Status = "Active"
                };
                _ctx.MaintenancePlans.Add(plan);
                await _ctx.SaveChangesAsync();
            }

            var workOrder = new WorkOrder
            {
                PlanId = plan.Id,
                WorkType = "Preventive Maintenance",
                Description = dto.Description,
                ScheduleDate = dto.ScheduleDate.ToUniversalTime(),
                Status = "Pending"
            };

            _ctx.WorkOrders.Add(workOrder);
            await _ctx.SaveChangesAsync();

            return Ok(new { message = "Work Order created successfully.", workOrderId = workOrder.Id });
        }

        // GET: api/WorkOrders/technicians
        [HttpGet("technicians")]
        public async Task<IActionResult> GetAvailableTechnicians([FromQuery] long? assetId)
        {
            var q = _ctx.Technicians.Include(t => t.ServiceType).Where(t => !t.IsDeleted && t.AvailabilityStatus == "Available");

            if (assetId.HasValue)
            {
                var asset = await _ctx.Assets.FindAsync(assetId.Value);
                if (asset != null && !string.IsNullOrEmpty(asset.AssetType))
                {
                    // Naive matching: if ServiceType matches AssetType name
                    var lowerAssetType = asset.AssetType.ToLower();
                    // We will return all, but flag the 'recommended' ones on the frontend, or sort them.
                    // For backend, let's just return them all with their ServiceType.
                }
            }

            var techs = await q.Select(t => new
            {
                technicianId = t.Id,
                fullName = t.FullName,
                serviceType = t.ServiceType != null ? t.ServiceType.Name : "General",
                contactNumber = t.ContactNumber,
                ranking = t.Ranking
            }).ToListAsync();

            return Ok(techs);
        }

        // POST: api/WorkOrders/{id}/assign
        [HttpPost("{id:long}/assign")]
        public async Task<IActionResult> AssignTechnician(long id, [FromBody] AssignTechnicianDto dto)
        {
            var workOrder = await _ctx.WorkOrders
                .Include(w => w.MaintenanceRequest)
                .FirstOrDefaultAsync(w => w.Id == id);
            if (workOrder == null || workOrder.IsDeleted) return NotFound("Work order not found.");

            var tech = await _ctx.Technicians.FindAsync(dto.TechnicianId);
            if (tech == null || tech.IsDeleted) return NotFound("Technician not found.");

            var assignment = new WorkAssignment
            {
                WorkOrderId = id,
                TechnicianId = dto.TechnicianId,
                Status = "Assigned",
                AssignedDate = DateTime.UtcNow
            };

            workOrder.Status = "Assigned";
            
            if (workOrder.MaintenanceRequest != null)
            {
                workOrder.MaintenanceRequest.Status = PropertyManagement.API.Models.Enums.RequestStatus.InProgress;
            }
            
            _ctx.WorkAssignments.Add(assignment);
            await _ctx.SaveChangesAsync();

            return Ok(new { message = "Technician assigned successfully." });
        }
    }
}
