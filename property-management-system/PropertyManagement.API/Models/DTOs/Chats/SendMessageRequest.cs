using System;
using Microsoft.AspNetCore.Http;

namespace PropertyManagement.API.Models.DTOs.Chats
{
    public class SendMessageRequest
    {
        public string Content { get; set; } = string.Empty;
        public string? AttachmentPath { get; set; }
        public IFormFile? Attachment { get; set; }
    }
}
