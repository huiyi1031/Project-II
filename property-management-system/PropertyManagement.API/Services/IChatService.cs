using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyManagement.API.Models.DTOs.Chats;

namespace PropertyManagement.API.Services
{
    public interface IChatService
    {
        Task<IEnumerable<ChatDto>> GetMyChatsAsync(long userId, CancellationToken cancellationToken);
        Task<ChatDto?> GetChatByIdAsync(long chatId, long userId, CancellationToken cancellationToken);
        Task<IEnumerable<MessageDto>> GetMessagesAsync(long chatId, long userId, CancellationToken cancellationToken);
        Task<MessageDto> SendMessageAsync(long chatId, long userId, SendMessageRequest request, CancellationToken cancellationToken);
        Task<IEnumerable<ChatParticipantDto>> GetParticipantsAsync(long chatId, long userId, CancellationToken cancellationToken);
        Task<bool> AddParticipantAsync(long chatId, long currentUserId, long userIdToAdd, CancellationToken cancellationToken);
        Task<IEnumerable<AvailableUserDto>> GetAvailableUsersToAddAsync(long chatId, long currentUserId, CancellationToken cancellationToken);
    }
}
