using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models.Entities
{
    public class PropertyServiceType : BaseEntity
    {
        [ForeignKey("Property")]
        public long PropertyId { get; set; }
        
        [ForeignKey("ServiceType")]
        public long ServiceTypeId { get; set; }
        
        // Navigation Properties
        public virtual Property? Property { get; set; }
        public virtual ServiceType? ServiceType { get; set; }
    }
}