using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models.Entities
{
    public class Asset : BaseEntity
    {
        [ForeignKey("Property")]
        public long PropertyId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string AssetName { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string? AssetType { get; set; }
        
        [MaxLength(100)]
        public string? Location { get; set; }
        
        public DateTime InstallationDate { get; set; }
        
        [MaxLength(100)]
        public string? Manufacturer { get; set; }
        
        [MaxLength(50)]
        public string? ModelNumber { get; set; }
        
        public int ExpLifespanYears { get; set; } = 10;
        
        public int CriticalityLevel { get; set; } = 3; // 1-5
        
        public int MaintenanceIntervalDays { get; set; } = 30;
        
        [MaxLength(50)]
        public string? Status { get; set; } = "Active";
        
        public bool IsHighRisk { get; set; } = false;
        
        [MaxLength(20)]
        public string? RiskLevel { get; set; }
        
        public decimal RiskScore { get; set; } = 0;
        
        [MaxLength(255)]
        public string? QrCode { get; set; }
        
        // Navigation Properties
        public virtual Property? Property { get; set; }
        public virtual ICollection<MaintenancePlan> MaintenancePlans { get; set; } = new List<MaintenancePlan>();
        public virtual ICollection<AssetMaintenanceHistory> MaintenanceHistories { get; set; } = new List<AssetMaintenanceHistory>();
    }
}