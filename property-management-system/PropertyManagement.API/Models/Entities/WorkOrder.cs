using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models.Entities
{
    public class WorkOrder : BaseEntity
    {
        [ForeignKey("MaintenanceRequest")]
        public long? RequestId { get; set; }
        
        [ForeignKey("MaintenancePlan")]
        public long? PlanId { get; set; }
        
        [MaxLength(50)]
        public string WorkType { get; set; } = string.Empty; // Repair, Inspection, Replacement
        
        public string? Description { get; set; }
        
        public DateTime ScheduleDate { get; set; }
        
        public DateTime? CompletedDate { get; set; }
        
        [MaxLength(50)]
        public string? Status { get; set; } = "Pending";
        
        // Navigation Properties
        public virtual MaintenanceRequest? MaintenanceRequest { get; set; }
        public virtual MaintenancePlan? MaintenancePlan { get; set; }
        public virtual ICollection<WorkAssignment> WorkAssignments { get; set; } = new List<WorkAssignment>();
        public virtual ICollection<AssetMaintenanceHistory> MaintenanceHistories { get; set; } = new List<AssetMaintenanceHistory>();
    }
}