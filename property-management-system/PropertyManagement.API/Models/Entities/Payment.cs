using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models.Entities
{
    public class Payment : BaseEntity
    {
        [ForeignKey("MaintenanceRequest")]
        public long RequestId { get; set; }
        
        public decimal Amount { get; set; }
        
        [MaxLength(50)]
        public string? PaymentMethod { get; set; } // Cash, Card, Online
        
        [MaxLength(50)]
        public string? PaymentStatus { get; set; } = "Pending";
        
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual MaintenanceRequest? MaintenanceRequest { get; set; }
    }
}