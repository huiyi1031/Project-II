using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PropertyManagement.API.Models.Enums;

namespace PropertyManagement.API.Models.Entities
{
    public class Occupant : BaseEntity
    {
        [ForeignKey("UserAccount")]
        public long UserAccountId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string? IdentificationNo { get; set; }
        
        [MaxLength(20)]
        public string? ContactNumber { get; set; }
        
        [MaxLength(1)]
        public string? Gender { get; set; }
        public int? Age { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        
        public OccupantType OccupantType { get; set; } = OccupantType.Tenant;
        
        public long? ParentOccupantId { get; set; }
        
        [MaxLength(20)]
        public string? OccupantStatus { get; set; } = "Active";
        
        // Navigation Properties
        public virtual UserAccount? UserAccount { get; set; }
        public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
        public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    }
}