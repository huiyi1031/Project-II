using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models.DTOs;
using PropertyManagement.API.Models.Entities;
using PropertyManagement.API.Models.Enums;
using System.Security.Claims;
using System.IO;

namespace PropertyManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MaintenanceRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public MaintenanceRequestsController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
                return userId;
            return 0;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromForm] CreateMaintenanceRequestDto requestDto)
        {
            var userId = GetCurrentUserId();
            var occupant = await _context.Occupants.FirstOrDefaultAsync(o => o.UserAccountId == userId);
            
            if (occupant == null)
            {
                // Auto-create occupant if it's missing (e.g. from a previous bug)
                var user = await _context.UserAccounts.FindAsync(userId);
                if (user != null)
                {
                    occupant = new Occupant
                    {
                        UserAccountId = userId,
                        FullName = "Missing Profile",
                        OccupantType = OccupantType.Resident,
                        OccupantStatus = "Active"
                    };
                    _context.Occupants.Add(occupant);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    return BadRequest(new { message = $"Only occupants can create maintenance requests. (User ID {userId} not found)" });
                }
            }

            // Handle Image Upload
            string? imagePath = null;
            if (requestDto.Image != null && requestDto.Image.Length > 0)
            {
                var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsFolder = Path.Combine(webRoot, "uploads", "maintenance");
                
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + requestDto.Image.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await requestDto.Image.CopyToAsync(fileStream);
                }
                
                imagePath = $"/uploads/maintenance/{uniqueFileName}";
            }

            var request = new MaintenanceRequest
            {
                Title = requestDto.Title,
                AssetType = requestDto.IssueCategory, // We store the category in AssetType here
                Description = requestDto.Description,
                UnitId = requestDto.UnitId,
                OccupantId = occupant.Id,
                ImagePath = imagePath,
                PriorityLevel = PriorityLevel.Medium,
                RequestDate = DateTime.UtcNow,
                Status = RequestStatus.Pending
            };

            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();

            // Auto-assign Technician logic
            var availableTechnician = await _context.Technicians
                .Include(t => t.ServiceType)
                .Where(t => t.AvailabilityStatus == "Available" && 
                            t.ServiceType != null && 
                            t.ServiceType.Name == requestDto.IssueCategory && 
                            !t.IsDeleted)
                .FirstOrDefaultAsync();

            WorkOrder? workOrder = null;
            if (availableTechnician != null)
            {
                // Create Work Order
                workOrder = new WorkOrder
                {
                    RequestId = request.Id,
                    WorkType = requestDto.IssueCategory,
                    Description = requestDto.Description,
                    ScheduleDate = DateTime.UtcNow.AddDays(1), // default schedule for next day
                    Status = "Assigned"
                };
                _context.WorkOrders.Add(workOrder);
                await _context.SaveChangesAsync();

                // Create Work Assignment
                var assignment = new WorkAssignment
                {
                    WorkOrderId = workOrder.Id,
                    TechnicianId = availableTechnician.Id,
                    AssignedDate = DateTime.UtcNow,
                    Status = "Assigned"
                };
                _context.WorkAssignments.Add(assignment);
                
                // Update Request Status
                request.Status = RequestStatus.InProgress;
                
                await _context.SaveChangesAsync();
            }

            // Create Chat Room
            var chat = new Chat
            {
                RequestId = request.Id
            };
            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            // Add Participants to Chat
            // 1. Tenant/Occupant
            _context.ChatParticipants.Add(new ChatParticipant
            {
                ChatId = chat.Id,
                UserAccountId = userId
            });

            // 2. Property Manager
            // Find a property manager to assign (get the first one for simplicity)
            var propertyManager = await _context.PropertyManagers.FirstOrDefaultAsync(pm => !pm.IsDeleted);
            if (propertyManager != null)
            {
                _context.ChatParticipants.Add(new ChatParticipant
                {
                    ChatId = chat.Id,
                    UserAccountId = propertyManager.UserAccountId
                });
            }

            // 3. Technician (if assigned)
            if (availableTechnician != null)
            {
                _context.ChatParticipants.Add(new ChatParticipant
                {
                    ChatId = chat.Id,
                    UserAccountId = availableTechnician.UserAccountId
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                requestId    = request.Id,
                title        = request.Title,
                status       = request.Status.ToString(),
                requestDate  = request.RequestDate
            });
        }

        // GET api/MaintenanceRequests/my  — returns the current occupant's requests
        [HttpGet("my")]
        public async Task<IActionResult> GetMyRequests([FromQuery] string? status)
        {
            var userId = GetCurrentUserId();
            var occupant = await _context.Occupants.FirstOrDefaultAsync(o => o.UserAccountId == userId);
            if (occupant == null)
                return Ok(Array.Empty<object>());

            var query = _context.MaintenanceRequests
                .Include(r => r.PropertyUnit)
                .Include(r => r.WorkOrder)
                .Where(r => r.OccupantId == occupant.Id);

            if (!string.IsNullOrEmpty(status) && status != "All" &&
                Enum.TryParse<RequestStatus>(status, true, out var parsedStatus))
                query = query.Where(r => r.Status == parsedStatus);

            var requests = await query
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            // Fetch technician names separately to avoid deep include chain
            var workOrderIds = requests
                .Where(r => r.WorkOrder != null)
                .Select(r => r.WorkOrder!.Id)
                .ToList();

            var technicianNames = await _context.WorkAssignments
                .Where(wa => workOrderIds.Contains(wa.WorkOrderId))
                .Join(_context.Technicians,
                      wa => wa.TechnicianId,
                      t  => t.Id,
                      (wa, t) => new { wa.WorkOrderId, t.FullName })
                .ToListAsync();

            var result = requests.Select(r =>
            {
                var techName = r.WorkOrder != null
                    ? technicianNames.FirstOrDefault(x => x.WorkOrderId == r.WorkOrder.Id)?.FullName
                    : null;

                return new
                {
                    requestID            = r.Id,
                    requestTitle         = r.Title,
                    issueCategory        = r.AssetType ?? "",
                    description          = r.Description ?? "",
                    priorityLevel        = r.PriorityLevel.ToString(),
                    status               = r.Status.ToString(),
                    submissionDate       = r.RequestDate,
                    attachmentPath       = r.ImagePath,
                    unitID               = r.UnitId,
                    occupantID           = r.OccupantId,
                    unitNumber           = r.PropertyUnit != null ? r.PropertyUnit.UnitNumber : "",
                    assignedTechnicianName = techName,
                    scheduledDate        = r.WorkOrder?.ScheduleDate,
                    workOrderID          = r.WorkOrder?.Id
                };
            });

            return Ok(result);
        }

        [HttpGet("debug-occupant/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> DebugOccupant(long userId)
        {
            var user = await _context.UserAccounts.FindAsync(userId);
            var occupant = await _context.Occupants.FirstOrDefaultAsync(o => o.UserAccountId == userId);
            
            return Ok(new {
                UserExists = user != null,
                UserRole = user?.RoleType.ToString(),
                OccupantExists = occupant != null,
                OccupantType = occupant?.OccupantType.ToString(),
                OccupantId = occupant?.Id,
                AllOccupants = await _context.Occupants.Select(o => new { o.Id, o.UserAccountId, Type = o.OccupantType.ToString() }).ToListAsync()
            });
        }
    }
}
