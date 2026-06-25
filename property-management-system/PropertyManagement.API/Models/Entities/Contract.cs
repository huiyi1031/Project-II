using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models.Entities
{
    public class Contract : BaseEntity
    {
        [ForeignKey("Occupant")]
        public long OccupantId { get; set; }
        
        [ForeignKey("PropertyUnit")]
        public long UnitId { get; set; }
        
        [MaxLength(20)]
        public string? ContractType { get; set; } // Tenancy, Ownership
        
        public DateTime StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        public bool IsPrimaryOccupant { get; set; } = false;
        
        [MaxLength(255)]
        public string? DocumentPath { get; set; }
        
        [MaxLength(20)]
        public string? Status { get; set; } = "Active";
        
        // Navigation Properties
        public virtual Occupant? Occupant { get; set; }
        public virtual PropertyUnit? PropertyUnit { get; set; }
    }
}