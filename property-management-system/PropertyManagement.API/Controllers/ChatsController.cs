using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.API.Models.DTOs.Chats;
using PropertyManagement.API.Services;
namespace PropertyManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatsController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IWebHostEnvironment _environment;

        public ChatsController(IChatService chatService, IWebHostEnvironment environment)
        {
            _chatService = chatService;
            _environment = environment;
        }

        private long GetAccountId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(idClaim, out var id))
            {
                return id;
            }
            throw new Exception("User account ID not found in token.");
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyChats(CancellationToken cancellationToken)
        {
            var userId = GetAccountId();
            var chats = await _chatService.GetMyChatsAsync(userId, cancellationToken);
            return Ok(chats);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetChat(long id, CancellationToken cancellationToken)
        {
            var userId = GetAccountId();
            var chat = await _chatService.GetChatByIdAsync(id, userId, cancellationToken);
            
            if (chat == null) return NotFound();
            
            return Ok(chat);
        }

        [HttpGet("{id:long}/messages")]
        public async Task<IActionResult> GetMessages(long id, CancellationToken cancellationToken)
        {
            var userId = GetAccountId();
            var messages = await _chatService.GetMessagesAsync(id, userId, cancellationToken);
            return Ok(messages);
        }

        [HttpPost("{id:long}/messages")]
        public async Task<IActionResult> SendMessage(long id, [FromForm] SendMessageRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetAccountId();

                if (request.Attachment != null)
                {
                    var extension = Path.GetExtension(request.Attachment.FileName);
                    var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var uploadFolder = Path.Combine(webRoot, "uploads", "chats");
                    Directory.CreateDirectory(uploadFolder);

                    var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
                    var filePath = Path.Combine(uploadFolder, fileName);

                    await using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.Attachment.CopyToAsync(stream, cancellationToken);
                    }

                    request.AttachmentPath = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/uploads/chats/{fileName}";
                }

                var message = await _chatService.SendMessageAsync(id, userId, request, cancellationToken);
                return Ok(message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:long}/participants")]
        public async Task<IActionResult> GetParticipants(long id, CancellationToken cancellationToken)
        {
            var userId = GetAccountId();
            var participants = await _chatService.GetParticipantsAsync(id, userId, cancellationToken);
            return Ok(participants);
        }

        [HttpPost("{id:long}/participants")]
        public async Task<IActionResult> AddParticipant(long id, [FromBody] AddParticipantRequest request, CancellationToken cancellationToken)
        {
            var userId = GetAccountId();
            var success = await _chatService.AddParticipantAsync(id, userId, request.AccountId, cancellationToken);
            if (!success) return Forbid();
            return Ok();
        }

        [HttpGet("{id:long}/available-users")]
        public async Task<IActionResult> GetAvailableUsers(long id, CancellationToken cancellationToken)
        {
            var userId = GetAccountId();
            var users = await _chatService.GetAvailableUsersToAddAsync(id, userId, cancellationToken);
            return Ok(users);
        }
    }
}
