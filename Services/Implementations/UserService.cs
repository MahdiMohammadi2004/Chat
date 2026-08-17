using Microsoft.EntityFrameworkCore;
using AutoMapper;
using ChatApp.Data;
using ChatApp.Models.Entities;
using ChatApp.Models.DTOs;
using ChatApp.Services.Interfaces;

namespace ChatApp.Services.Implementations;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UserService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        
        return user != null ? _mapper.Map<UserDto>(user) : null;
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null ? _mapper.Map<UserDto>(user) : null;
    }

    public async Task<UserDto> CreateOrUpdateUserAsync(string username, string? displayName = null)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null)
        {
            user = new User
            {
                Username = username,
                DisplayName = displayName ?? username,
                IsOnline = false
            };
            _context.Users.Add(user);
        }
        else
        {
            if (!string.IsNullOrEmpty(displayName))
            {
                user.DisplayName = displayName;
            }
        }

        await _context.SaveChangesAsync();
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> UpdateUserConnectionAsync(string username, string? connectionId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null)
        {
            throw new ArgumentException($"User '{username}' not found");
        }

        user.ConnectionId = connectionId;
        if (connectionId != null)
        {
            user.IsOnline = true;
            user.LastSeenAt = null;
        }

        await _context.SaveChangesAsync();
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> SetUserOnlineStatusAsync(string username, bool isOnline)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null)
        {
            throw new ArgumentException($"User '{username}' not found");
        }

        user.IsOnline = isOnline;
        if (!isOnline)
        {
            user.LastSeenAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return _mapper.Map<UserDto>(user);
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .OrderBy(u => u.Username)
            .ToListAsync();

        return _mapper.Map<List<UserDto>>(users);
    }

    public async Task<List<UserDto>> SearchUsersAsync(string searchTerm, int excludeUserId)
    {
        var users = await _context.Users
            .Where(u => u.Id != excludeUserId && 
                       (u.Username.Contains(searchTerm) || 
                        (u.DisplayName != null && u.DisplayName.Contains(searchTerm))))
            .OrderBy(u => u.Username)
            .Take(20)
            .ToListAsync();

        return _mapper.Map<List<UserDto>>(users);
    }
}
