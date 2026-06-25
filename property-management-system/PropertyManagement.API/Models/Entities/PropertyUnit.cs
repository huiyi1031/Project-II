using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models.Entities
{
    public class PropertyUnit : BaseEntity
    {
        [ForeignKey("Property")]
        public long PropertyId { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string UnitNumber { get; set; } = string.Empty;
        
        [MaxLength(10)]
        public string? FloorLevel { get; set; }
        
        [MaxLength(10)]
        public string? Block { get; set; }
        
        [MaxLength(20)]
        public string? Status { get; set; } = "Vacant";
        
        public int? Bedrooms { get; set; }
        
        public int? Bathrooms { get; set; }
        
        public decimal? AreaSqft { get; set; }
        
        [MaxLength(20)]
        public string? UnitType { get; set; }
        
        public int MaxOccupants { get; set; } = 4;
        
        public int CurrentOccupants { get; set; } = 0;
        
        // Navigation Properties
        public virtual Property? Property { get; set; }
        public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
        public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    }
}