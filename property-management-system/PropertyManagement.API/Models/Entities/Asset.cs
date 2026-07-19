using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models.Entities
{
    public class Asset : BaseEntity
    {
        [ForeignKey("Property")]
        public long PropertyId { get; set; }

        [Required]
        [MaxLength(100)]
        public string AssetName { get; set; } = string.Empty;

        /// <summary>e.g. Elevator, HVAC, Water Pump, Fire Suppression, Generator</summary>
        [MaxLength(50)]
        public string? AssetType { get; set; }

        /// <summary>Physical location within the building (e.g. "Lobby B1", "Rooftop")</summary>
        [MaxLength(100)]
        public string? Location { get; set; }

        public DateTime InstallationDate { get; set; }

        [MaxLength(100)]
        public string? Manufacturer { get; set; }

        [MaxLength(50)]
        public string? ModelNumber { get; set; }

        /// <summary>Expected lifespan in years (from supplier documentation)</summary>
        public int ExpLifespanYears { get; set; } = 10;

        /// <summary>Recommended interval in days between maintenance (from supplier)</summary>
        public int MaintenanceIntervalDays { get; set; } = 90;

        /// <summary>Active | Inactive</summary>
        [MaxLength(20)]
        public string? Status { get; set; } = "Active";

        // ── Supplier Info ──────────────────────────────────────
        [MaxLength(100)]
        public string? SupplierName { get; set; }

        public DateTime? WarrantyExpiryDate { get; set; }

        // ── Maintenance Tracking ───────────────────────────────
        /// <summary>Calculated: LastServiceDate + MaintenanceIntervalDays</summary>
        public DateTime? NextMaintenanceDueDate { get; set; }

        /// <summary>Unique QR code string, auto-generated on creation</summary>
        [MaxLength(255)]
        public string? QrCode { get; set; }

        // Navigation Properties
        public virtual Property? Property { get; set; }
        public virtual ICollection<MaintenancePlan> MaintenancePlans { get; set; } = new List<MaintenancePlan>();
        public virtual ICollection<AssetMaintenanceHistory> MaintenanceHistories { get; set; } = new List<AssetMaintenanceHistory>();
    }
}