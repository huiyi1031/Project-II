using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models.Entities
{
    public class ChatParticipant : BaseEntity
    {
        [ForeignKey("Chat")]
        public long ChatId { get; set; }
        
        [ForeignKey("UserAccount")]
        public long UserAccountId { get; set; }
        
        // Navigation Properties
        public virtual Chat? Chat { get; set; }
        public virtual UserAccount? UserAccount { get; set; }
    }
}