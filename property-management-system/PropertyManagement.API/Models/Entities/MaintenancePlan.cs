using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models.Entities
{
    public class MaintenancePlan : BaseEntity
    {
        [ForeignKey("Asset")]
        public long AssetId { get; set; }
        
        public int IntervalDays { get; set; }
        
        public DateTime LastServiceDate { get; set; }
        
        public DateTime NextDueDate { get; set; }
        
        [MaxLength(50)]
        public string? Status { get; set; } = "Active";
        
        [MaxLength(50)]
        public string? ScheduledBy { get; set; }
        
        // Navigation Properties
        public virtual Asset? Asset { get; set; }
        public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
    }
}