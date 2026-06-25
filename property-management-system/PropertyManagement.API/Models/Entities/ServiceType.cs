using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.Models.Entities
{
    public class ServiceType : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public decimal? BasePrice { get; set; }
        
        // Navigation Properties
        public virtual ICollection<Technician> Technicians { get; set; } = new List<Technician>();
        public virtual ICollection<PropertyServiceType> PropertyServiceTypes { get; set; } = new List<PropertyServiceType>();
    }
}