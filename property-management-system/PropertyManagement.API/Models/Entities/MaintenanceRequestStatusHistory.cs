using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PropertyManagement.API.Models.Enums;

namespace PropertyManagement.API.Models.Entities
{
    public class MaintenanceRequestStatusHistory : BaseEntity
    {
        [ForeignKey("MaintenanceRequest")]
        public long RequestId { get; set; }

        public RequestStatus? PreviousStatus { get; set; }

        public RequestStatus NewStatus { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Remarks { get; set; }

        [Required]
        [MaxLength(100)]
        public string PerformedBy { get; set; } = "System";

        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

        public virtual MaintenanceRequest? MaintenanceRequest { get; set; }
    }
}
