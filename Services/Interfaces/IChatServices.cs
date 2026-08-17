using ChatApp.Models.DTOs;

namespace ChatApp.Services.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<UserDto?> GetUserByIdAsync(int userId);
    Task<UserDto> CreateOrUpdateUserAsync(string username, string? displayName = null);
    Task<UserDto> UpdateUserConnectionAsync(string username, string? connectionId);
    Task<UserDto> SetUserOnlineStatusAsync(string username, bool isOnline);
    Task<List<UserDto>> GetAllUsersAsync();
    Task<List<UserDto>> SearchUsersAsync(string searchTerm, int excludeUserId);
}

public interface IChatRoomService
{
    Task<ChatRoomDto> CreatePrivateChatAsync(string user1Username, string user2Username);
    Task<ChatRoomDto> CreateGroupChatAsync(string name, string createdByUsername, List<string> participantUsernames);
    Task<ChatRoomDto?> GetChatRoomByIdAsync(int chatRoomId);
    Task<List<ChatRoomDto>> GetUserChatRoomsAsync(string username);
    Task<ChatRoomDto> AddParticipantAsync(int chatRoomId, string username);
    Task<ChatRoomDto> RemoveParticipantAsync(int chatRoomId, string username);
    Task<bool> IsUserInChatAsync(int chatRoomId, string username);
}

public interface IMessageService
{
    Task<MessageDto> SendMessageAsync(int chatRoomId, string senderUsername, string content);
    Task<List<MessageDto>> GetChatMessagesAsync(int chatRoomId, int take = 50, int skip = 0);
    Task MarkMessagesAsReadAsync(int chatRoomId, string username);
}
