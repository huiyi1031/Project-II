using System.ComponentModel.DataAnnotations;
using PropertyManagement.API.Models.Enums;

namespace PropertyManagement.API.Models.Entities
{
    public class UserAccount : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required]
        public RoleType RoleType { get; set; }
        
        public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;
        
        public DateTime? LastLogin { get; set; }
        
        [MaxLength(255)]
        public string? ProfilePictureUrl { get; set; }
        
        // One-to-One Relationships
        public virtual Occupant? Occupant { get; set; }
        public virtual Technician? Technician { get; set; }
        public virtual PropertyManager? PropertyManager { get; set; }
        
        // One-to-Many Relationships
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
        public virtual ICollection<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();
    }
}