using Microsoft.AspNetCore.SignalR;
using ChatApp.Services.Interfaces;
using ChatApp.Models.DTOs;

namespace ChatApp.Hubs;

public class ChatHub : Hub
{
    private readonly IUserService _userService;
    private readonly IChatRoomService _chatRoomService;
    private readonly IMessageService _messageService;
    private static readonly Dictionary<string, string> UserConnectionMap = new();

    public ChatHub(IUserService userService, IChatRoomService chatRoomService, IMessageService messageService)
    {
        _userService = userService;
        _chatRoomService = chatRoomService;
        _messageService = messageService;
    }

    #region Connection Lifecycle

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        
        // Find username for this connection
        var username = UserConnectionMap.FirstOrDefault(x => x.Value == connectionId).Key;
        
        if (!string.IsNullOrEmpty(username))
        {
            UserConnectionMap.Remove(username);
            await _userService.SetUserOnlineStatusAsync(username, false);
            
            // Notify others that user went offline
            await Clients.Others.SendAsync("UserOffline", username);
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    #endregion

    #region User Management

    public async Task RegisterUser(string username)
    {
        var user = await _userService.CreateOrUpdateUserAsync(username);
        await _userService.UpdateUserConnectionAsync(username, Context.ConnectionId);
        
        UserConnectionMap[username] = Context.ConnectionId;
        
        // Send back user info and all users list
        await Clients.Caller.SendAsync("UserRegistered", user);
        await Clients.Caller.SendAsync("UserListUpdated", await _userService.GetAllUsersAsync());
        
        // Notify others
        await Clients.Others.SendAsync("UserOnline", username);
    }

    public async Task GetUserList()
    {
        var users = await _userService.GetAllUsersAsync();
        await Clients.Caller.SendAsync("UserListUpdated", users);
    }

    public async Task SearchUsers(string searchTerm)
    {
        var excludeUserId = 0;
        var username = UserConnectionMap.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        
        if (!string.IsNullOrEmpty(username))
        {
            var user = await _userService.GetUserByUsernameAsync(username);
            if (user != null)
            {
                excludeUserId = user.Id;
            }
        }
        
        var users = await _userService.SearchUsersAsync(searchTerm, excludeUserId);
        await Clients.Caller.SendAsync("SearchResults", users);
    }

    #endregion

    #region Chat Room Management

    public async Task CreatePrivateChat(string targetUsername)
    {
        var username = UserConnectionMap.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        
        if (string.IsNullOrEmpty(username))
        {
            await Clients.Caller.SendAsync("Error", "User not registered");
            return;
        }

        try
        {
            var chatRoom = await _chatRoomService.CreatePrivateChatAsync(username, targetUsername);
            await Clients.Caller.SendAsync("PrivateChatCreated", chatRoom);
            
            // Notify the other user if online
            var targetUser = await _userService.GetUserByUsernameAsync(targetUsername);
            if (targetUser?.IsOnline == true && !string.IsNullOrEmpty(targetUser.Username))
            {
                await Clients.User(UserConnectionMap[targetUsername]).SendAsync("NewChatInvitation", chatRoom);
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task CreateGroupChat(string name, List<string> participantUsernames)
    {
        var username = UserConnectionMap.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        
        if (string.IsNullOrEmpty(username))
        {
            await Clients.Caller.SendAsync("Error", "User not registered");
            return;
        }

        try
        {
            var chatRoom = await _chatRoomService.CreateGroupChatAsync(name, username, participantUsernames);
            await Clients.Caller.SendAsync("GroupChatCreated", chatRoom);
            
            // Notify all participants
            foreach (var participant in chatRoom.Participants)
            {
                if (participant.User.IsOnline && !string.IsNullOrEmpty(participant.User.Username))
                {
                    var connectionId = UserConnectionMap.GetValueOrDefault(participant.User.Username);
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        await Clients.User(connectionId).SendAsync("NewChatInvitation", chatRoom);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task GetMyChatRooms()
    {
        var username = UserConnectionMap.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        
        if (string.IsNullOrEmpty(username))
        {
            await Clients.Caller.SendAsync("Error", "User not registered");
            return;
        }

        var chatRooms = await _chatRoomService.GetUserChatRoomsAsync(username);
        await Clients.Caller.SendAsync("ChatRoomsUpdated", chatRooms);
    }

    public async Task JoinChat(int chatRoomId)
    {
        var username = UserConnectionMap.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        
        if (string.IsNullOrEmpty(username))
        {
            await Clients.Caller.SendAsync("Error", "User not registered");
            return;
        }

        try
        {
            var chatRoom = await _chatRoomService.AddParticipantAsync(chatRoomId, username);
            await Clients.Caller.SendAsync("JoinedChat", chatRoom);
            
            // Notify existing members
            await Clients.Group($"chat_{chatRoomId}").SendAsync("UserJoinedChat", username, chatRoom);
            
            // Join SignalR group for this chat
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatRoomId}");
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task LeaveChat(int chatRoomId)
    {
        var username = UserConnectionMap.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        
        if (string.IsNullOrEmpty(username))
        {
            await Clients.Caller.SendAsync("Error", "User not registered");
            return;
        }

        try
        {
            var chatRoom = await _chatRoomService.RemoveParticipantAsync(chatRoomId, username);
            await Clients.Caller.SendAsync("LeftChat", chatRoom);
            
            // Notify remaining members
            await Clients.Group($"chat_{chatRoomId}").SendAsync("UserLeftChat", username, chatRoom);
            
            // Leave SignalR group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{chatRoomId}");
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task JoinChatRoomGroup(int chatRoomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatRoomId}");
    }

    public async Task LeaveChatRoomGroup(int chatRoomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{chatRoomId}");
    }

    #endregion

    #region Messaging

    public async Task SendMessage(int chatRoomId, string content)
    {
        var username = UserConnectionMap.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        
        if (string.IsNullOrEmpty(username))
        {
            await Clients.Caller.SendAsync("Error", "User not registered");
            return;
        }

        try
        {
            var message = await _messageService.SendMessageAsync(chatRoomId, username, content);
            
            // Send to all users in the chat room
            await Clients.Group($"chat_{chatRoomId}").SendAsync("MessageReceived", message);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task GetChatHistory(int chatRoomId, int take = 50, int skip = 0)
    {
        var messages = await _messageService.GetChatMessagesAsync(chatRoomId, take, skip);
        await Clients.Caller.SendAsync("ChatHistoryLoaded", messages);
    }

    public async Task MarkMessagesRead(int chatRoomId)
    {
        var username = UserConnectionMap.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        
        if (string.IsNullOrEmpty(username))
        {
            return;
        }

        await _messageService.MarkMessagesAsReadAsync(chatRoomId, username);
    }

    #endregion

    #region Typing Indicators

    public async Task TypingStart(int chatRoomId)
    {
        var username = UserConnectionMap.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        
        if (!string.IsNullOrEmpty(username))
        {
            await Clients.GroupExcept($"chat_{chatRoomId}", Context.ConnectionId)
                .SendAsync("UserTyping", username, chatRoomId);
        }
    }

    public async Task TypingStop(int chatRoomId)
    {
        var username = UserConnectionMap.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        
        if (!string.IsNullOrEmpty(username))
        {
            await Clients.GroupExcept($"chat_{chatRoomId}", Context.ConnectionId)
                .SendAsync("UserStoppedTyping", username, chatRoomId);
        }
    }

    #endregion
}