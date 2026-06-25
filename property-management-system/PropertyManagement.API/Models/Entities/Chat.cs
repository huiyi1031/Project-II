using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models.Entities
{
    public class Chat : BaseEntity
    {
        [ForeignKey("MaintenanceRequest")]
        public long RequestId { get; set; }
        
        // Navigation Properties
        public virtual MaintenanceRequest? MaintenanceRequest { get; set; }
        public virtual ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}