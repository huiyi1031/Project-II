using System;

namespace PropertyManagement.API.Models.DTOs.Chats
{
    public class ChatParticipantDto
    {
        public long ParticipantID { get; set; }
        public long ChatID { get; set; }
        public long AccountID { get; set; }
        public string? FullName { get; set; }
        public string? Role { get; set; }
        public bool IsAdmin { get; set; }
    }
}
