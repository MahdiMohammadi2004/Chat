using Microsoft.EntityFrameworkCore;
using AutoMapper;
using ChatApp.Data;
using ChatApp.Models.Entities;
using ChatApp.Models.DTOs;
using ChatApp.Services.Interfaces;

namespace ChatApp.Services.Implementations;

public class MessageService : IMessageService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public MessageService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<MessageDto> SendMessageAsync(int chatRoomId, string senderUsername, string content)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == senderUsername.ToLower());

        if (user == null)
        {
            throw new ArgumentException($"User '{senderUsername}' not found");
        }

        var chatRoom = await _context.ChatRooms
            .Include(cr => cr.Participants)
            .FirstOrDefaultAsync(cr => cr.Id == chatRoomId);

        if (chatRoom == null)
        {
            throw new ArgumentException("Chat room not found");
        }

        // Verify user is a participant
        if (!chatRoom.Participants.Any(p => p.UserId == user.Id))
        {
            throw new InvalidOperationException("User is not a participant in this chat");
        }

        var message = new Message
        {
            ChatRoomId = chatRoomId,
            SenderUserId = user.Id,
            Content = content.Trim(),
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        await _context.Entry(message).Reference(m => m.SenderUser).LoadAsync();
        await _context.Entry(message).Reference(m => m.ChatRoom).LoadAsync();

        return _mapper.Map<MessageDto>(message);
    }

    public async Task<List<MessageDto>> GetChatMessagesAsync(int chatRoomId, int take = 50, int skip = 0)
    {
        var messages = await _context.Messages
            .Include(m => m.SenderUser)
            .Include(m => m.ChatRoom)
            .Where(m => m.ChatRoomId == chatRoomId)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        // Return in ascending order (oldest first)
        messages.Reverse();

        return _mapper.Map<List<MessageDto>>(messages);
    }

    public async Task MarkMessagesAsReadAsync(int chatRoomId, string username)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null)
        {
            throw new ArgumentException($"User '{username}' not found");
        }

        // Mark all messages from other users as read
        var unreadMessages = await _context.Messages
            .Where(m => m.ChatRoomId == chatRoomId && 
                       m.SenderUserId != user.Id && 
                       !m.IsRead)
            .ToListAsync();

        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
        }

        await _context.SaveChangesAsync();
    }
}
