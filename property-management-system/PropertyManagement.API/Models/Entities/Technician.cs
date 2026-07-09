using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models.Entities
{
    public class Technician : BaseEntity
    {
        [ForeignKey("UserAccount")]
        public long UserAccountId { get; set; }
        
        [ForeignKey("ServiceType")]
        public long? ServiceTypeId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string? ContactNumber { get; set; }
        
        [MaxLength(1)]
        public string? Gender { get; set; }
        public int? Age { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        
        [MaxLength(50)]
        public string? ExperienceLevel { get; set; }
        
        [MaxLength(20)]
        public string? AvailabilityStatus { get; set; } = "Available";
        
        public decimal? Ranking { get; set; }
        
        // Navigation Properties
        public virtual UserAccount? UserAccount { get; set; }
        public virtual ServiceType? ServiceType { get; set; }
        public virtual ICollection<WorkAssignment> WorkAssignments { get; set; } = new List<WorkAssignment>();
    }
}