using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PropertyManagement.API.Models.Enums;

namespace PropertyManagement.API.Models.Entities
{
    public class AssetMaintenanceHistory : BaseEntity
    {
        [ForeignKey("Asset")]
        public long AssetId { get; set; }

        /// <summary>Optional link to a work order if the maintenance was job-driven</summary>
        [ForeignKey("WorkOrder")]
        public long? WorkOrderId { get; set; }

        /// <summary>Preventive | Corrective | Inspection</summary>
        public MaintenanceType MaintenanceType { get; set; }

        public string? Description { get; set; }

        public decimal? Cost { get; set; }

        public DateTime MaintenanceDate { get; set; } = DateTime.UtcNow;

        /// <summary>Completed | Partial | Failed</summary>
        [MaxLength(50)]
        public string? ResultStatus { get; set; } = "Completed";

        /// <summary>Name of technician / service crew who performed the maintenance</summary>
        [MaxLength(100)]
        public string? PerformedBy { get; set; }

        // Navigation Properties
        public virtual Asset? Asset { get; set; }
        public virtual WorkOrder? WorkOrder { get; set; }
    }
}