using Microsoft.EntityFrameworkCore;
using AutoMapper;
using ChatApp.Data;
using ChatApp.Models.Entities;
using ChatApp.Models.DTOs;
using ChatApp.Services.Interfaces;

namespace ChatApp.Services.Implementations;

public class ChatRoomService : IChatRoomService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public ChatRoomService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ChatRoomDto> CreatePrivateChatAsync(string user1Username, string user2Username)
    {
        // Check if a private chat already exists between these two users
        var existingChat = await _context.ChatRooms
            .Include(cr => cr.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(cr => cr.Type == ChatRoomType.Private &&
                cr.Participants.Count() == 2 &&
                cr.Participants.Any(p => p.User.Username.ToLower() == user1Username.ToLower()) &&
                cr.Participants.Any(p => p.User.Username.ToLower() == user2Username.ToLower()));

        if (existingChat != null)
        {
            return _mapper.Map<ChatRoomDto>(existingChat);
        }

        // Create new private chat
        var user1 = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == user1Username.ToLower());
        var user2 = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == user2Username.ToLower());

        if (user1 == null || user2 == null)
        {
            throw new ArgumentException("One or both users not found");
        }

        var chatRoom = new ChatRoom
        {
            Type = ChatRoomType.Private,
            CreatedByUserId = user1.Id,
            CreatedAt = DateTime.UtcNow,
            Participants = new List<ChatRoomParticipant>
            {
                new() { UserId = user1.Id, JoinedAt = DateTime.UtcNow },
                new() { UserId = user2.Id, JoinedAt = DateTime.UtcNow }
            }
        };

        _context.ChatRooms.Add(chatRoom);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        await _context.Entry(chatRoom).Collection(c => c.Participants).LoadAsync();
        foreach (var participant in chatRoom.Participants)
        {
            await _context.Entry(participant).Reference(p => p.User).LoadAsync();
        }

        return _mapper.Map<ChatRoomDto>(chatRoom);
    }

    public async Task<ChatRoomDto> CreateGroupChatAsync(string name, string createdByUsername, List<string> participantUsernames)
    {
        var creator = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == createdByUsername.ToLower());

        if (creator == null)
        {
            throw new ArgumentException($"User '{createdByUsername}' not found");
        }

        // Get all participant users
        var participants = await _context.Users
            .Where(u => participantUsernames.Contains(u.Username))
            .ToListAsync();

        if (participants.Count != participantUsernames.Count)
        {
            throw new ArgumentException("One or more participants not found");
        }

        // Add creator to participants if not already included
        if (!participants.Any(p => p.Id == creator.Id))
        {
            participants.Add(creator);
        }

        var chatRoom = new ChatRoom
        {
            Name = name,
            Type = ChatRoomType.Group,
            CreatedByUserId = creator.Id,
            CreatedAt = DateTime.UtcNow,
            Participants = participants.Select(p => new ChatRoomParticipant
            {
                UserId = p.Id,
                JoinedAt = DateTime.UtcNow
            }).ToList()
        };

        _context.ChatRooms.Add(chatRoom);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        await _context.Entry(chatRoom).Collection(c => c.Participants).LoadAsync();
        foreach (var participant in chatRoom.Participants)
        {
            await _context.Entry(participant).Reference(p => p.User).LoadAsync();
        }
        await _context.Entry(chatRoom).Reference(c => c.CreatedByUser).LoadAsync();

        return _mapper.Map<ChatRoomDto>(chatRoom);
    }

    public async Task<ChatRoomDto?> GetChatRoomByIdAsync(int chatRoomId)
    {
        var chatRoom = await _context.ChatRooms
            .Include(cr => cr.Participants)
                .ThenInclude(p => p.User)
            .Include(cr => cr.CreatedByUser)
            .FirstOrDefaultAsync(cr => cr.Id == chatRoomId);

        if (chatRoom == null)
        {
            return null;
        }

        return _mapper.Map<ChatRoomDto>(chatRoom);
    }

    public async Task<List<ChatRoomDto>> GetUserChatRoomsAsync(string username)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null)
        {
            return new List<ChatRoomDto>();
        }

        var chatRooms = await _context.ChatRooms
            .Include(cr => cr.Participants)
                .ThenInclude(p => p.User)
            .Include(cr => cr.CreatedByUser)
            .Where(cr => cr.Participants.Any(p => p.UserId == user.Id))
            .OrderByDescending(cr => cr.Messages.OrderByDescending(m => m.SentAt).Select(m => (DateTime?)m.SentAt).FirstOrDefault() ?? cr.CreatedAt)
            .ToListAsync();

        return _mapper.Map<List<ChatRoomDto>>(chatRooms);
    }

    public async Task<ChatRoomDto> AddParticipantAsync(int chatRoomId, string username)
    {
        var chatRoom = await _context.ChatRooms
            .Include(cr => cr.Participants)
            .FirstOrDefaultAsync(cr => cr.Id == chatRoomId);

        if (chatRoom == null)
        {
            throw new ArgumentException("Chat room not found");
        }

        if (chatRoom.Type != ChatRoomType.Group)
        {
            throw new InvalidOperationException("Cannot add participants to a private chat");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null)
        {
            throw new ArgumentException($"User '{username}' not found");
        }

        if (chatRoom.Participants.Any(p => p.UserId == user.Id))
        {
            throw new InvalidOperationException("User is already in the chat");
        }

        chatRoom.Participants.Add(new ChatRoomParticipant
        {
            ChatRoomId = chatRoomId,
            UserId = user.Id,
            JoinedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Reload
        await _context.Entry(chatRoom).Collection(c => c.Participants).LoadAsync();
        foreach (var participant in chatRoom.Participants)
        {
            await _context.Entry(participant).Reference(p => p.User).LoadAsync();
        }
        await _context.Entry(chatRoom).Reference(c => c.CreatedByUser).LoadAsync();

        return _mapper.Map<ChatRoomDto>(chatRoom);
    }

    public async Task<ChatRoomDto> RemoveParticipantAsync(int chatRoomId, string username)
    {
        var chatRoom = await _context.ChatRooms
            .Include(cr => cr.Participants)
            .FirstOrDefaultAsync(cr => cr.Id == chatRoomId);

        if (chatRoom == null)
        {
            throw new ArgumentException("Chat room not found");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null)
        {
            throw new ArgumentException($"User '{username}' not found");
        }

        var participant = chatRoom.Participants.FirstOrDefault(p => p.UserId == user.Id);
        if (participant == null)
        {
            throw new InvalidOperationException("User is not in the chat");
        }

        chatRoom.Participants.Remove(participant);
        _context.ChatRoomParticipants.Remove(participant);

        await _context.SaveChangesAsync();

        // Reload
        await _context.Entry(chatRoom).Collection(c => c.Participants).LoadAsync();
        foreach (var p in chatRoom.Participants)
        {
            await _context.Entry(p).Reference(part => part.User).LoadAsync();
        }
        await _context.Entry(chatRoom).Reference(c => c.CreatedByUser).LoadAsync();

        return _mapper.Map<ChatRoomDto>(chatRoom);
    }

    public async Task<bool> IsUserInChatAsync(int chatRoomId, string username)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null)
        {
            return false;
        }

        return await _context.ChatRoomParticipants
            .AnyAsync(p => p.ChatRoomId == chatRoomId && p.UserId == user.Id);
    }
}
