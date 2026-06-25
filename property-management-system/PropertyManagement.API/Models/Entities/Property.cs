using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.Models.Entities
{
    public class Property : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string PropertyName { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string? PropertyType { get; set; }
        
        [MaxLength(255)]
        public string? Address { get; set; }
        
        [MaxLength(50)]
        public string? City { get; set; }
        
        [MaxLength(50)]
        public string? State { get; set; }
        
        [MaxLength(10)]
        public string? Postcode { get; set; }
        
        // Navigation Properties
        public virtual ICollection<PropertyUnit> PropertyUnits { get; set; } = new List<PropertyUnit>();
        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
        public virtual ICollection<PropertyServiceType> PropertyServiceTypes { get; set; } = new List<PropertyServiceType>();
    }
}