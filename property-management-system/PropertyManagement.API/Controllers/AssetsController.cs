using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models.Entities;
using PropertyManagement.API.Models.Enums;
using Microsoft.AspNetCore.Authorization;
namespace PropertyManagement.API.Controllers
{
    // ─── DTOs ────────────────────────────────────────────────────────────────────

    public record CreateAssetDto(
        long PropertyId,
        string AssetName,
        string? AssetType,
        string? Location,
        DateTime InstallationDate,
        string? Manufacturer,
        string? ModelNumber,
        int ExpLifespanYears,
        int MaintenanceIntervalDays,
        string? SupplierName,
        DateTime? WarrantyExpiryDate
    );

    public record UpdateAssetDto(
        string AssetName,
        string? AssetType,
        string? Location,
        DateTime InstallationDate,
        string? Manufacturer,
        string? ModelNumber,
        int ExpLifespanYears,
        int MaintenanceIntervalDays,
        string? SupplierName,
        DateTime? WarrantyExpiryDate,
        DateTime? NextMaintenanceDueDate,
        string? Status
    );

    public record AddMaintenanceHistoryDto(
        int MaintenanceType,        // 0=Corrective, 1=Preventive, 2=Inspection
        string? Description,
        decimal? Cost,
        DateTime MaintenanceDate,
        string? ResultStatus,
        string? PerformedBy
    );

    // ─── Controller ──────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AssetsController : ControllerBase
    {
        private readonly AppDbContext _ctx;

        public AssetsController(AppDbContext ctx) => _ctx = ctx;

        private async Task<long?> GetManagerPropertyIdAsync()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);

            if (userIdClaim != null && roleClaim != null && roleClaim.Value == "PropertyManager")
            {
                var userId = long.Parse(userIdClaim.Value);
                var pm = await _ctx.PropertyManagers.FirstOrDefaultAsync(m => m.UserAccountId == userId);
                if (pm != null && pm.PropertyId.HasValue)
                {
                    return pm.PropertyId.Value;
                }
            }
            return null;
        }

        // ── GET /api/Assets ──────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? assetType,
            [FromQuery] string? status,
            [FromQuery] long? propertyId)
        {
            var q = _ctx.Assets
                .Include(a => a.Property)
                .Where(a => !a.IsDeleted);

            var managerPropertyId = await GetManagerPropertyIdAsync();
            if (managerPropertyId.HasValue)
            {
                q = q.Where(a => a.PropertyId == managerPropertyId.Value);
            }
            else if (propertyId.HasValue)
            {
                q = q.Where(a => a.PropertyId == propertyId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                q = q.Where(a => a.AssetName.ToLower().Contains(lowerSearch) ||
                                 (a.Location != null && a.Location.ToLower().Contains(lowerSearch)) ||
                                 (a.AssetType != null && a.AssetType.ToLower().Contains(lowerSearch)));
            }

            if (!string.IsNullOrWhiteSpace(assetType) && assetType != "All")
                q = q.Where(a => a.AssetType == assetType);

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
                q = q.Where(a => a.Status == status);

            var assets = await q
                .OrderBy(a => a.AssetType)
                .ThenBy(a => a.AssetName)
                .Select(a => new
                {
                    assetId                = a.Id,
                    propertyId             = a.PropertyId,
                    propertyName           = a.Property != null ? a.Property.PropertyName : null,
                    assetName              = a.AssetName,
                    assetType              = a.AssetType,
                    location               = a.Location,
                    installationDate       = a.InstallationDate,
                    manufacturer           = a.Manufacturer,
                    modelNumber            = a.ModelNumber,
                    expLifespanYears       = a.ExpLifespanYears,
                    maintenanceIntervalDays = a.MaintenanceIntervalDays,
                    supplierName           = a.SupplierName,
                    warrantyExpiryDate     = a.WarrantyExpiryDate,
                    nextMaintenanceDueDate = a.NextMaintenanceDueDate,
                    status                 = a.Status,
                    qrCode                 = a.QrCode,
                    createdAt              = a.CreatedAt
                })
                .ToListAsync();

            return Ok(assets);
        }

        // ── GET /api/Assets/{id} ─────────────────────────────────────────────────
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var q = _ctx.Assets
                .Include(a => a.Property)
                .Include(a => a.MaintenanceHistories.Where(h => !h.IsDeleted))
                .Where(a => a.Id == id && !a.IsDeleted);

            var managerPropertyId = await GetManagerPropertyIdAsync();
            if (managerPropertyId.HasValue)
            {
                q = q.Where(a => a.PropertyId == managerPropertyId.Value);
            }

            var asset = await q.FirstOrDefaultAsync();

            if (asset == null) return NotFound(new { message = "Asset not found." });

            var history = asset.MaintenanceHistories
                .OrderByDescending(h => h.MaintenanceDate)
                .Select(h => new
                {
                    historyId       = h.Id,
                    maintenanceType = h.MaintenanceType.ToString(),
                    description     = h.Description,
                    cost            = h.Cost,
                    maintenanceDate = h.MaintenanceDate,
                    resultStatus    = h.ResultStatus,
                    performedBy     = h.PerformedBy,
                    workOrderId     = h.WorkOrderId
                });

            return Ok(new
            {
                assetId                = asset.Id,
                propertyId             = asset.PropertyId,
                propertyName           = asset.Property?.PropertyName,
                assetName              = asset.AssetName,
                assetType              = asset.AssetType,
                location               = asset.Location,
                installationDate       = asset.InstallationDate,
                manufacturer           = asset.Manufacturer,
                modelNumber            = asset.ModelNumber,
                expLifespanYears       = asset.ExpLifespanYears,
                maintenanceIntervalDays = asset.MaintenanceIntervalDays,
                supplierName           = asset.SupplierName,
                warrantyExpiryDate     = asset.WarrantyExpiryDate,
                nextMaintenanceDueDate = asset.NextMaintenanceDueDate,
                status                 = asset.Status,
                qrCode                 = asset.QrCode,
                createdAt              = asset.CreatedAt,
                maintenanceHistory     = history
            });
        }

        // ── POST /api/Assets ─────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAssetDto dto)
        {
            var managerPropertyId = await GetManagerPropertyIdAsync();
            long propertyIdToUse = managerPropertyId ?? dto.PropertyId;

            var propertyExists = await _ctx.Properties.AnyAsync(p => p.Id == propertyIdToUse && !p.IsDeleted);
            if (!propertyExists)
                return BadRequest(new { message = "Property not found." });

            // Generate unique QR code string: PMS-ASSET-{TYPE_PREFIX}-{TIMESTAMP}-{RANDOM}
            var typePrefix = (dto.AssetType ?? "GEN").Length >= 3
                ? dto.AssetType!.Substring(0, 3).ToUpper()
                : "GEN";
            var qrCode = $"PMS-{typePrefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";

            // Ensure QR is unique (extremely unlikely collision but safe)
            while (await _ctx.Assets.AnyAsync(a => a.QrCode == qrCode))
                qrCode = $"PMS-{typePrefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";

            // Calculate initial NextMaintenanceDueDate: InstallationDate + interval
            var nextDue = dto.InstallationDate.AddDays(dto.MaintenanceIntervalDays);

            var asset = new Asset
            {
                PropertyId              = propertyIdToUse,
                AssetName               = dto.AssetName,
                AssetType               = dto.AssetType,
                Location                = dto.Location,
                InstallationDate        = dto.InstallationDate.ToUniversalTime(),
                Manufacturer            = dto.Manufacturer,
                ModelNumber             = dto.ModelNumber,
                ExpLifespanYears        = dto.ExpLifespanYears,
                MaintenanceIntervalDays = dto.MaintenanceIntervalDays,
                SupplierName            = dto.SupplierName,
                WarrantyExpiryDate      = dto.WarrantyExpiryDate?.ToUniversalTime(),
                NextMaintenanceDueDate  = nextDue.ToUniversalTime(),
                Status                  = "Active",
                QrCode                  = qrCode
            };

            _ctx.Assets.Add(asset);
            await _ctx.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = asset.Id }, new
            {
                message   = $"Asset '{asset.AssetName}' registered successfully.",
                assetId   = asset.Id,
                assetName = asset.AssetName,
                qrCode    = asset.QrCode,
                nextMaintenanceDueDate = asset.NextMaintenanceDueDate
            });
        }

        // ── PUT /api/Assets/{id} ─────────────────────────────────────────────────
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateAssetDto dto)
        {
            var q = _ctx.Assets.Where(a => a.Id == id && !a.IsDeleted);
            var managerPropertyId = await GetManagerPropertyIdAsync();
            if (managerPropertyId.HasValue)
            {
                q = q.Where(a => a.PropertyId == managerPropertyId.Value);
            }

            var asset = await q.FirstOrDefaultAsync();
            if (asset == null) return NotFound(new { message = "Asset not found or unauthorized." });

            asset.AssetName               = dto.AssetName;
            asset.AssetType               = dto.AssetType;
            asset.Location                = dto.Location;
            asset.InstallationDate        = dto.InstallationDate.ToUniversalTime();
            asset.Manufacturer            = dto.Manufacturer;
            asset.ModelNumber             = dto.ModelNumber;
            asset.ExpLifespanYears        = dto.ExpLifespanYears;
            asset.MaintenanceIntervalDays = dto.MaintenanceIntervalDays;
            asset.SupplierName            = dto.SupplierName;
            asset.WarrantyExpiryDate      = dto.WarrantyExpiryDate?.ToUniversalTime();
            asset.NextMaintenanceDueDate  = dto.NextMaintenanceDueDate?.ToUniversalTime();
            asset.Status                  = dto.Status ?? asset.Status;
            asset.UpdatedAt               = DateTime.UtcNow;

            await _ctx.SaveChangesAsync();

            return Ok(new { message = $"Asset '{asset.AssetName}' updated successfully.", assetId = asset.Id });
        }

        // ── PATCH /api/Assets/{id}/deactivate ────────────────────────────────────
        [HttpPatch("{id:long}/deactivate")]
        public async Task<IActionResult> Deactivate(long id)
        {
            var q = _ctx.Assets.Where(a => a.Id == id && !a.IsDeleted);
            var managerPropertyId = await GetManagerPropertyIdAsync();
            if (managerPropertyId.HasValue)
            {
                q = q.Where(a => a.PropertyId == managerPropertyId.Value);
            }

            var asset = await q.FirstOrDefaultAsync();
            if (asset == null) return NotFound(new { message = "Asset not found or unauthorized." });
            if (asset.Status == "Inactive")
                return BadRequest(new { message = "Asset is already inactive." });

            asset.Status    = "Inactive";
            asset.UpdatedAt = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();

            return Ok(new { message = $"Asset '{asset.AssetName}' has been deactivated. Historical records are preserved." });
        }

        // ── GET /api/Assets/{id}/history ─────────────────────────────────────────
        [HttpGet("{id:long}/history")]
        public async Task<IActionResult> GetHistory(long id)
        {
            var q = _ctx.Assets.Where(a => a.Id == id && !a.IsDeleted);
            var managerPropertyId = await GetManagerPropertyIdAsync();
            if (managerPropertyId.HasValue)
            {
                q = q.Where(a => a.PropertyId == managerPropertyId.Value);
            }

            var assetExists = await q.AnyAsync();
            if (!assetExists) return NotFound(new { message = "Asset not found or unauthorized." });

            var history = await _ctx.AssetMaintenanceHistories
                .Where(h => h.AssetId == id && !h.IsDeleted)
                .OrderByDescending(h => h.MaintenanceDate)
                .Select(h => new
                {
                    historyId       = h.Id,
                    assetId         = h.AssetId,
                    maintenanceType = h.MaintenanceType.ToString(),
                    description     = h.Description,
                    cost            = h.Cost,
                    maintenanceDate = h.MaintenanceDate,
                    resultStatus    = h.ResultStatus,
                    performedBy     = h.PerformedBy,
                    workOrderId     = h.WorkOrderId
                })
                .ToListAsync();

            return Ok(history);
        }

        // ── GET /api/Assets/histories ─────────────────────────────────────────
        [HttpGet("histories")]
        public async Task<IActionResult> GetAllHistories()
        {
            var managerPropertyId = await GetManagerPropertyIdAsync();
            var q = _ctx.AssetMaintenanceHistories
                .Include(h => h.Asset)
                .Where(h => !h.IsDeleted);

            if (managerPropertyId.HasValue)
            {
                q = q.Where(h => h.Asset.PropertyId == managerPropertyId.Value);
            }

            var history = await q
                .OrderByDescending(h => h.MaintenanceDate)
                .Select(h => new
                {
                    historyId       = h.Id,
                    assetId         = h.AssetId,
                    assetName       = h.Asset.AssetName,
                    maintenanceType = h.MaintenanceType.ToString(),
                    description     = h.Description,
                    cost            = h.Cost,
                    maintenanceDate = h.MaintenanceDate,
                    resultStatus    = h.ResultStatus,
                    performedBy     = h.PerformedBy,
                    workOrderId     = h.WorkOrderId
                })
                .ToListAsync();

            return Ok(history);
        }

        // ── POST /api/Assets/{id}/history ────────────────────────────────────────
        [HttpPost("{id:long}/history")]
        public async Task<IActionResult> AddHistory(long id, [FromBody] AddMaintenanceHistoryDto dto)
        {
            var q = _ctx.Assets.Where(a => a.Id == id && !a.IsDeleted);
            var managerPropertyId = await GetManagerPropertyIdAsync();
            if (managerPropertyId.HasValue)
            {
                q = q.Where(a => a.PropertyId == managerPropertyId.Value);
            }

            var asset = await q.FirstOrDefaultAsync();
            if (asset == null) return NotFound(new { message = "Asset not found or unauthorized." });

            var record = new AssetMaintenanceHistory
            {
                AssetId         = id,
                MaintenanceType = (MaintenanceType)dto.MaintenanceType,
                Description     = dto.Description,
                Cost            = dto.Cost,
                MaintenanceDate = dto.MaintenanceDate.ToUniversalTime(),
                ResultStatus    = dto.ResultStatus ?? "Completed",
                PerformedBy     = dto.PerformedBy
            };

            _ctx.AssetMaintenanceHistories.Add(record);

            // Update next due date on the asset when a completed maintenance is recorded
            if (record.ResultStatus == "Completed")
            {
                asset.NextMaintenanceDueDate = dto.MaintenanceDate.AddDays(asset.MaintenanceIntervalDays).ToUniversalTime();
                asset.UpdatedAt = DateTime.UtcNow;
            }

            await _ctx.SaveChangesAsync();

            return Ok(new
            {
                message   = "Maintenance record added.",
                historyId = record.Id,
                nextMaintenanceDueDate = asset.NextMaintenanceDueDate
            });
        }
    }
}
