using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models.Entities
{
    public class WorkAssignment : BaseEntity
    {
        [ForeignKey("WorkOrder")]
        public long WorkOrderId { get; set; }
        
        [ForeignKey("Technician")]
        public long TechnicianId { get; set; }
        
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        
        [MaxLength(50)]
        public string? Status { get; set; } = "Assigned";
        
        // Navigation Properties
        public virtual WorkOrder? WorkOrder { get; set; }
        public virtual Technician? Technician { get; set; }
    }
}