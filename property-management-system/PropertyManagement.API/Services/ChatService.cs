using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models.DTOs.Chats;
using PropertyManagement.API.Models.Entities;

namespace PropertyManagement.API.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;

        public ChatService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ChatDto>> GetMyChatsAsync(long userId, CancellationToken cancellationToken)
        {
            var chats = await _context.Chats
                .Include(c => c.MaintenanceRequest)
                .Include(c => c.Participants)
                .Include(c => c.Messages)
                .Where(c => c.Participants.Any(p => p.UserAccountId == userId) && !c.IsDeleted)
                .OrderByDescending(c => c.Messages.Max(m => (DateTime?)m.SentAt) ?? c.CreatedAt)
                .ToListAsync(cancellationToken);

            return chats.Select(c => new ChatDto
            {
                ChatID = c.Id,
                RequestID = c.RequestId,
                CreatedAt = c.CreatedAt,
                RequestTitle = c.MaintenanceRequest?.Title,
                RequestNumber = c.MaintenanceRequest?.RequestNumber,
                RequestStatus = c.MaintenanceRequest?.Status.ToString(),
                LastMessage = c.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault()?.MessageContent,
                ParticipantCount = c.Participants.Count
            });
        }

        public async Task<ChatDto?> GetChatByIdAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var chat = await _context.Chats
                .Include(c => c.MaintenanceRequest)
                .Include(c => c.Participants)
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == chatId && c.Participants.Any(p => p.UserAccountId == userId) && !c.IsDeleted, cancellationToken);

            if (chat == null) return null;

            return new ChatDto
            {
                ChatID = chat.Id,
                RequestID = chat.RequestId,
                CreatedAt = chat.CreatedAt,
                RequestTitle = chat.MaintenanceRequest?.Title,
                RequestNumber = chat.MaintenanceRequest?.RequestNumber,
                RequestStatus = chat.MaintenanceRequest?.Status.ToString(),
                LastMessage = chat.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault()?.MessageContent,
                ParticipantCount = chat.Participants.Count
            };
        }

        public async Task<IEnumerable<MessageDto>> GetMessagesAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            // Verify access
            var hasAccess = await _context.ChatParticipants
                .AnyAsync(cp => cp.ChatId == chatId && cp.UserAccountId == userId, cancellationToken);
            
            if (!hasAccess) return Enumerable.Empty<MessageDto>();

            var messages = await _context.Messages
                .Include(m => m.Sender)
                    .ThenInclude(u => u.Occupant)
                .Include(m => m.Sender)
                    .ThenInclude(u => u.PropertyManager)
                .Include(m => m.Sender)
                    .ThenInclude(u => u.Technician)
                .Where(m => m.ChatId == chatId && !m.IsDeleted)
                .OrderBy(m => m.SentAt)
                .ToListAsync(cancellationToken);

            return messages.Select(m => new MessageDto
            {
                MessageID = m.Id,
                ChatID = m.ChatId,
                SenderAccountID = m.SenderId,
                SenderName = m.Sender?.Occupant?.FullName 
                             ?? m.Sender?.PropertyManager?.FullName 
                             ?? m.Sender?.Technician?.FullName 
                             ?? m.Sender?.Email 
                             ?? "Unknown",
                Content = m.MessageContent,
                SentAt = m.SentAt,
                AttachmentPath = m.AttachmentPath,
                IsOwn = m.SenderId == userId,
                MessageType = m.MessageType
            });
        }

        public async Task<MessageDto> SendMessageAsync(long chatId, long userId, SendMessageRequest request, CancellationToken cancellationToken)
        {
            var hasAccess = await _context.ChatParticipants
                .AnyAsync(cp => cp.ChatId == chatId && cp.UserAccountId == userId, cancellationToken);
                
            if (!hasAccess)
                throw new Exception("Chat not found or access denied.");

            var message = new Message
            {
                ChatId = chatId,
                SenderId = userId,
                MessageContent = request.Content,
                AttachmentPath = request.AttachmentPath,
                SentAt = DateTime.UtcNow,
                MessageType = "User",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync(cancellationToken);

            var savedMessage = await _context.Messages
                .Include(m => m.Sender)
                    .ThenInclude(u => u.Occupant)
                .Include(m => m.Sender)
                    .ThenInclude(u => u.PropertyManager)
                .Include(m => m.Sender)
                    .ThenInclude(u => u.Technician)
                .FirstOrDefaultAsync(m => m.Id == message.Id, cancellationToken);

            return new MessageDto
            {
                MessageID = savedMessage!.Id,
                ChatID = savedMessage.ChatId,
                SenderAccountID = savedMessage.SenderId,
                SenderName = savedMessage.Sender?.Occupant?.FullName 
                             ?? savedMessage.Sender?.PropertyManager?.FullName 
                             ?? savedMessage.Sender?.Technician?.FullName 
                             ?? savedMessage.Sender?.Email 
                             ?? "Unknown",
                Content = savedMessage.MessageContent,
                SentAt = savedMessage.SentAt,
                AttachmentPath = savedMessage.AttachmentPath,
                IsOwn = true,
                MessageType = savedMessage.MessageType
            };
        }

        public async Task<IEnumerable<ChatParticipantDto>> GetParticipantsAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var hasAccess = await _context.ChatParticipants
                .AnyAsync(cp => cp.ChatId == chatId && cp.UserAccountId == userId, cancellationToken);
                
            if (!hasAccess) return Enumerable.Empty<ChatParticipantDto>();

            var participants = await _context.ChatParticipants
                .Include(p => p.UserAccount)
                    .ThenInclude(u => u.Occupant)
                .Include(p => p.UserAccount)
                    .ThenInclude(u => u.PropertyManager)
                .Include(p => p.UserAccount)
                    .ThenInclude(u => u.Technician)
                .Where(p => p.ChatId == chatId && !p.IsDeleted)
                .ToListAsync(cancellationToken);

            return participants.Select(p => new ChatParticipantDto
            {
                ParticipantID = p.Id,
                ChatID = p.ChatId,
                AccountID = p.UserAccountId,
                FullName = p.UserAccount?.Occupant?.FullName 
                           ?? p.UserAccount?.PropertyManager?.FullName 
                           ?? p.UserAccount?.Technician?.FullName 
                           ?? p.UserAccount?.Email 
                           ?? "Unknown",
                Role = p.UserAccount?.RoleType.ToString(),
                IsAdmin = p.IsAdmin
            });
        }

        public async Task<bool> AddParticipantAsync(long chatId, long currentUserId, long userIdToAdd, CancellationToken cancellationToken)
        {
            var adminCheck = await _context.ChatParticipants
                .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserAccountId == currentUserId && cp.IsAdmin && !cp.IsDeleted, cancellationToken);

            if (adminCheck == null) return false;

            var existing = await _context.ChatParticipants
                .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserAccountId == userIdToAdd && !cp.IsDeleted, cancellationToken);

            if (existing != null) return true;

            _context.ChatParticipants.Add(new ChatParticipant
            {
                ChatId = chatId,
                UserAccountId = userIdToAdd,
                IsAdmin = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<IEnumerable<AvailableUserDto>> GetAvailableUsersToAddAsync(long chatId, long currentUserId, CancellationToken cancellationToken)
        {
            var hasAccess = await _context.ChatParticipants
                .AnyAsync(cp => cp.ChatId == chatId && cp.UserAccountId == currentUserId && cp.IsAdmin && !cp.IsDeleted, cancellationToken);

            if (!hasAccess) return Enumerable.Empty<AvailableUserDto>();

            var chat = await _context.Chats
                .Include(c => c.MaintenanceRequest)
                    .ThenInclude(r => r.PropertyUnit)
                        .ThenInclude(u => u.Property)
                .FirstOrDefaultAsync(c => c.Id == chatId, cancellationToken);
                
            string? requesterPropertyType = chat?.MaintenanceRequest?.PropertyUnit?.Property?.PropertyType;

            var requesterOccupantId = chat?.MaintenanceRequest?.OccupantId;

            var currentParticipantIds = await _context.ChatParticipants
                .Where(cp => cp.ChatId == chatId && !cp.IsDeleted)
                .Select(cp => cp.UserAccountId)
                .ToListAsync(cancellationToken);

            var currentUser = await _context.UserAccounts
                .Include(u => u.Occupant)
                .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

            var query = _context.UserAccounts
                .Include(u => u.Technician)
                .Include(u => u.PropertyManager)
                .Include(u => u.Occupant)
                .Where(u => !currentParticipantIds.Contains(u.Id) 
                         && u.AccountStatus == Models.Enums.AccountStatus.Active 
                         && !u.IsDeleted);

            if (currentUser != null)
            {
                if (currentUser.RoleType == Models.Enums.RoleType.PropertyManager)
                {
                    // Managers can only add Technicians (not other Managers)
                    query = query.Where(u => u.RoleType == Models.Enums.RoleType.Technician);
                }
                else if (currentUser.RoleType == Models.Enums.RoleType.Occupant && currentUser.Occupant != null)
                {
                    var occupant = currentUser.Occupant;
                    if (occupant.OccupantType == Models.Enums.OccupantType.Owner)
                    {
                        // Owners can add their Tenants and Residents (Family Members)
                        query = query.Where(u => u.RoleType == Models.Enums.RoleType.Occupant 
                            && u.Occupant != null 
                            && u.Occupant.ParentOccupantId == occupant.Id 
                            && (u.Occupant.OccupantType == Models.Enums.OccupantType.Tenant || u.Occupant.OccupantType == Models.Enums.OccupantType.Resident));
                    }
                    else if (occupant.OccupantType == Models.Enums.OccupantType.Tenant)
                    {
                        // Tenants can add other Tenants under the same Owner
                        query = query.Where(u => u.RoleType == Models.Enums.RoleType.Occupant 
                            && u.Occupant != null 
                            && u.Occupant.ParentOccupantId == occupant.ParentOccupantId 
                            && u.Occupant.OccupantType == Models.Enums.OccupantType.Tenant);
                    }
                    else if (occupant.OccupantType == Models.Enums.OccupantType.Resident)
                    {
                        // Residents (Family Members) can add other Residents under the same Owner
                        query = query.Where(u => u.RoleType == Models.Enums.RoleType.Occupant 
                            && u.Occupant != null 
                            && u.Occupant.ParentOccupantId == occupant.ParentOccupantId 
                            && u.Occupant.OccupantType == Models.Enums.OccupantType.Resident);
                    }
                }
                else 
                {
                    // Fallback for any other roles (like Admin)
                    query = query.Where(u => u.RoleType == Models.Enums.RoleType.Technician);
                }
            }

            var users = await query.ToListAsync(cancellationToken);

            return users.Select(u => new AvailableUserDto
            {
                AccountId = u.Id,
                FullName = u.Technician?.FullName ?? u.PropertyManager?.FullName ?? u.Occupant?.FullName ?? u.Email ?? "Unknown",
                Role = u.RoleType.ToString()
            });
        }
    }
}
