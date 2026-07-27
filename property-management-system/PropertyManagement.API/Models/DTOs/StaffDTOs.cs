using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.Models.DTOs
{
    public class CreateStaffDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string ContactNumber { get; set; } = string.Empty;
        
        [Required]
        public string RoleType { get; set; } = string.Empty; // "Technician" or "PropertyManager"
        
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Age { get; set; }
        public long? PropertyId { get; set; }

        public long? ServiceTypeID { get; set; }
        public string? ExperienceLevel { get; set; }
        public string? AvailabilityStatus { get; set; }
        public decimal? PriorityRank { get; set; }
        public string? Position { get; set; }
    }

    public class UpdateStaffDto
    {
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? ContactNumber { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Age { get; set; }
        public long? PropertyId { get; set; }
        public long? ServiceTypeID { get; set; }
        public string? ExperienceLevel { get; set; }
        public string? AvailabilityStatus { get; set; }
        public decimal? PriorityRank { get; set; }
        public string? Position { get; set; }
    }

    public class DeactivateStaffDto
    {
        public string? Reason { get; set; }
    }
}
