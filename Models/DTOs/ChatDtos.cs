namespace ChatApp.Models.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }
}

public class ChatRoomDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Type { get; set; }
    public int CreatedByUserId { get; set; }
    public string CreatedByUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<UserDto> Participants { get; set; } = new();
}

public class MessageDto
{
    public int Id { get; set; }
    public int ChatRoomId { get; set; }
    public int SenderUserId { get; set; }
    public string SenderUsername { get; set; } = string.Empty;
    public string? SenderDisplayName { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
}

public class CreatePrivateChatRequest
{
    public string CurrentUsername { get; set; } = string.Empty;
    public string TargetUsername { get; set; } = string.Empty;
}

public class CreateGroupChatRequest
{
    public string Name { get; set; } = string.Empty;
    public string CreatedByUsername { get; set; } = string.Empty;
    public List<string> ParticipantUsernames { get; set; } = new();
}

public class SendMessageRequest
{
    public int ChatRoomId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class JoinChatRequest
{
    public int ChatRoomId { get; set; }
    public string Username { get; set; } = string.Empty;
}

public class LeaveChatRequest
{
    public int ChatRoomId { get; set; }
    public string Username { get; set; } = string.Empty;
}
