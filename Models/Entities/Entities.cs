namespace ChatApp.Models.Entities;

public enum ChatRoomType
{
    Private = 1,
    Group = 2
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ConnectionId { get; set; }
    public bool IsOnline { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeenAt { get; set; }

    // Navigation Properties
    public ICollection<ChatRoomParticipant> ChatRoomParticipants { get; set; } = new List<ChatRoomParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public class ChatRoom
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public ChatRoomType Type { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public User CreatedByUser { get; set; } = null!;
    public ICollection<ChatRoomParticipant> Participants { get; set; } = new List<ChatRoomParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public class ChatRoomParticipant
{
    public int Id { get; set; }
    public int ChatRoomId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public ChatRoom ChatRoom { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class Message
{
    public int Id { get; set; }
    public int ChatRoomId { get; set; }
    public int SenderUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }

    // Navigation Properties
    public ChatRoom ChatRoom { get; set; } = null!;
    public User SenderUser { get; set; } = null!;
}
