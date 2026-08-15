using System;
using PropertyManagement.API.Models.Entities;

namespace PropertyManagement.API.Models.DTOs.Chats
{
    public class ChatDto
    {
        public long ChatID { get; set; }
        public long RequestID { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? RequestTitle { get; set; }
        public string? RequestNumber { get; set; }
        public string? RequestStatus { get; set; }
        public string? LastMessage { get; set; }
        public int ParticipantCount { get; set; }
    }
}
