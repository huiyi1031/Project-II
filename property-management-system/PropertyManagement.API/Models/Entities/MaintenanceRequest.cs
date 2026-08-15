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
        
        [MaxLength(50)]
        public string RequestNumber { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;
        
        public DateTime? PreferredAccessDateTime { get; set; }

        [MaxLength(500)]
        public string? ResolutionNotes { get; set; }
        
        public DateTime? ResolvedDate { get; set; }
        
        public DateTime? ScheduledDate { get; set; }
        
        [MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? UpdatedBy { get; set; }
        
        public DateTime? ApprovedAt { get; set; }
        
        [MaxLength(100)]
        public string? ApprovedBy { get; set; }
        
        public DateTime? RejectedAt { get; set; }
        
        [MaxLength(100)]
        public string? RejectedBy { get; set; }
        
        [MaxLength(500)]
        public string? RejectionReason { get; set; }
        
        public DateTime? CancelledAt { get; set; }
        
        [MaxLength(100)]
        public string? CancelledBy { get; set; }
        
        [MaxLength(500)]
        public string? CancellationReason { get; set; }
        
        public virtual ICollection<MaintenanceRequestStatusHistory> StatusHistories { get; set; } = new List<MaintenanceRequestStatusHistory>();
        
        // Navigation Properties
        public virtual Occupant? Occupant { get; set; }
        public virtual PropertyUnit? PropertyUnit { get; set; }
        public virtual Payment? Payment { get; set; }
        public virtual WorkOrder? WorkOrder { get; set; }
        public virtual Chat? Chat { get; set; }
    }
}