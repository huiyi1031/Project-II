using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PropertyManagement.API.Models.Enums;

namespace PropertyManagement.API.Models.Entities
{
    public class MaintenanceRequest : BaseEntity
    {
        [ForeignKey("Occupant")]
        public long OccupantId { get; set; }
        
        [ForeignKey("PropertyUnit")]
        public long UnitId { get; set; }
        
        [MaxLength(100)]
        public string? AssetType { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [MaxLength(255)]
        public string? ImagePath { get; set; }
        
        public PriorityLevel PriorityLevel { get; set; } = PriorityLevel.Medium;
        
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        
        public RequestStatus Status { get; set; } = RequestStatus.Pending;
        
        [MaxLength(500)]
        public string? ResolutionNotes { get; set; }
        
        public DateTime? ResolvedDate { get; set; }
        
        // Navigation Properties
        public virtual Occupant? Occupant { get; set; }
        public virtual PropertyUnit? PropertyUnit { get; set; }
        public virtual Payment? Payment { get; set; }
        public virtual WorkOrder? WorkOrder { get; set; }
        public virtual Chat? Chat { get; set; }
    }
}