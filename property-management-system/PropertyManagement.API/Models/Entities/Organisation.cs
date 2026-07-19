using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.Models.Entities
{
    public class Organisation : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string OrganisationName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ContactPerson { get; set; }

        [MaxLength(100)]
        public string? ContactEmail { get; set; }

        [MaxLength(20)]
        public string? ContactPhone { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        /// <summary>Company registration number (e.g. SSM registration)</summary>
        [MaxLength(50)]
        public string? RegistrationNo { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
    }
}
