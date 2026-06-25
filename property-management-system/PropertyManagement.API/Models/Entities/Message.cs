using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models.Entities
{
    public class Message : BaseEntity
    {
        [ForeignKey("Chat")]
        public long ChatId { get; set; }
        
        [ForeignKey("UserAccount")]
        public long SenderId { get; set; }
        
        [Required]
        public string MessageContent { get; set; } = string.Empty;
        
        [MaxLength(255)]
        public string? AttachmentPath { get; set; }
        
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(20)]
        public string? ReadStatus { get; set; } = "Unread";
        
        // Navigation Properties
        public virtual Chat? Chat { get; set; }
        public virtual UserAccount? Sender { get; set; }
    }
}