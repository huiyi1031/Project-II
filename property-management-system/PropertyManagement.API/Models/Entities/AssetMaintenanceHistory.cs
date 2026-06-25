using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PropertyManagement.API.Models.Enums;

namespace PropertyManagement.API.Models.Entities
{
    public class AssetMaintenanceHistory : BaseEntity
    {
        [ForeignKey("Asset")]
        public long AssetId { get; set; }
        
        [ForeignKey("WorkOrder")]
        public long? WorkOrderId { get; set; }
        
        public MaintenanceType MaintenanceType { get; set; }
        
        [MaxLength(50)]
        public string? FailureType { get; set; }
        
        public string? Description { get; set; }
        
        public int? DowntimeDuration { get; set; } // in hours
        
        public decimal? Cost { get; set; }
        
        public DateTime MaintenanceDate { get; set; } = DateTime.UtcNow;
        
        [MaxLength(50)]
        public string? ResultStatus { get; set; } = "Completed";
        
        // Navigation Properties
        public virtual Asset? Asset { get; set; }
        public virtual WorkOrder? WorkOrder { get; set; }
    }
}