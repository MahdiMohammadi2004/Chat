// Chat Application Client-Side JavaScript

class ChatApp {
    constructor() {
        this.connection = null;
        this.currentUser = null;
        this.currentChatRoom = null;
        this.users = [];
        this.chatRooms = [];
        this.typingTimeout = null;
        
        this.initializeElements();
        this.initializeEventListeners();
    }

    initializeElements() {
        // Login Modal
        this.loginModal = document.getElementById('loginModal');
        this.usernameInput = document.getElementById('usernameInput');
        this.loginBtn = document.getElementById('loginBtn');
        
        // Sidebar
        this.userListContainer = document.getElementById('userList');
        this.chatRoomsContainer = document.getElementById('chatRoomsList');
        this.searchInput = document.getElementById('searchInput');
        this.currentUserDisplay = document.getElementById('currentUserDisplay');
        
        // Main Chat Area
        this.chatMain = document.getElementById('chatMain');
        this.emptyChatView = document.getElementById('emptyChatView');
        this.activeChatView = document.getElementById('activeChatView');
        this.chatHeaderName = document.getElementById('chatHeaderName');
        this.chatHeaderStatus = document.getElementById('chatHeaderStatus');
        this.chatHeaderAvatar = document.getElementById('chatHeaderAvatar');
        this.messagesContainer = document.getElementById('messagesContainer');
        this.messageInput = document.getElementById('messageInput');
        this.sendBtn = document.getElementById('sendBtn');
        this.typingIndicator = document.getElementById('typingIndicator');
        
        // Modals
        this.createGroupModal = document.getElementById('createGroupModal');
        this.groupNameInput = document.getElementById('groupNameInput');
        this.participantsList = document.getElementById('participantsList');
        this.createGroupBtn = document.getElementById('createGroupBtn');
        this.closeGroupModalBtns = document.querySelectorAll('.close-modal');
        
        // Buttons
        this.createPrivateChatBtn = document.getElementById('createPrivateChatBtn');
        this.createGroupChatBtn = document.getElementById('createGroupChatBtn');
        this.leaveChatBtn = document.getElementById('leaveChatBtn');
    }

    initializeEventListeners() {
        // Login
        this.loginBtn.addEventListener('click', () => this.handleLogin());
        this.usernameInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') this.handleLogin();
        });
        
        // Search
        this.searchInput.addEventListener('input', (e) => {
            const searchTerm = e.target.value.trim();
            if (searchTerm.length >= 2) {
                this.searchUsers(searchTerm);
            } else if (searchTerm.length === 0) {
                this.renderUserList(this.users);
            }
        });
        
        // Message Input
        this.messageInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.sendMessage();
            }
        });
        
        this.messageInput.addEventListener('input', () => {
            this.handleTyping();
        });
        
        this.sendBtn.addEventListener('click', () => this.sendMessage());
        
        // Create Chat Buttons
        this.createGroupChatBtn.addEventListener('click', () => this.openCreateGroupModal());
        
        // Close Modals
        this.closeGroupModalBtns.forEach(btn => {
            btn.addEventListener('click', () => this.closeModals());
        });
        
        // Create Group
        this.createGroupBtn.addEventListener('click', () => this.createGroupChat());
        
        // Leave Chat
        this.leaveChatBtn.addEventListener('click', () => this.leaveCurrentChat());
        
        // Close modal when clicking outside
        window.addEventListener('click', (e) => {
            if (e.target === this.createGroupModal) {
                this.closeModals();
            }
        });
    }

    async initializeConnection() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/chatHub')
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .build();

        // Register event handlers
        this.registerEventHandlers();

        try {
            await this.connection.start();
            console.log('SignalR Connected');
            
            // Register user after connection
            if (this.currentUser) {
                await this.connection.invoke('RegisterUser', this.currentUser.username);
            }
        } catch (err) {
            console.error('SignalR Connection Error:', err);
            setTimeout(() => this.initializeConnection(), 5000);
        }
    }

    registerEventHandlers() {
        this.connection.on('UserRegistered', (user) => {
            console.log('User Registered:', user);
            this.currentUser = user;
            this.updateCurrentUserDisplay();
        });

        this.connection.on('UserListUpdated', (users) => {
            console.log('Users Updated:', users);
            this.users = users;
            this.renderUserList(users);
        });

        this.connection.on('UserOnline', (username) => {
            console.log('User Online:', username);
            this.updateUserStatus(username, true);
        });

        this.connection.on('UserOffline', (username) => {
            console.log('User Offline:', username);
            this.updateUserStatus(username, false);
        });

        this.connection.on('SearchResults', (users) => {
            console.log('Search Results:', users);
            this.renderUserList(users);
        });

        this.connection.on('PrivateChatCreated', (chatRoom) => {
            console.log('Private Chat Created:', chatRoom);
            this.addOrUpdateChatRoom(chatRoom);
            this.selectChatRoom(chatRoom);
        });

        this.connection.on('GroupChatCreated', (chatRoom) => {
            console.log('Group Chat Created:', chatRoom);
            this.addOrUpdateChatRoom(chatRoom);
            this.selectChatRoom(chatRoom);
        });

        this.connection.on('NewChatInvitation', (chatRoom) => {
            console.log('New Chat Invitation:', chatRoom);
            this.addOrUpdateChatRoom(chatRoom);
            // Optionally show notification
        });

        this.connection.on('ChatRoomsUpdated', (chatRooms) => {
            console.log('Chat Rooms Updated:', chatRooms);
            this.chatRooms = chatRooms;
            this.renderChatRoomsList();
        });

        this.connection.on('JoinedChat', (chatRoom) => {
            console.log('Joined Chat:', chatRoom);
            this.addOrUpdateChatRoom(chatRoom);
            this.selectChatRoom(chatRoom);
        });

        this.connection.on('LeftChat', (chatRoom) => {
            console.log('Left Chat:', chatRoom);
            this.removeChatRoom(chatRoom.id);
            if (this.currentChatRoom && this.currentChatRoom.id === chatRoom.id) {
                this.showEmptyChat();
            }
        });

        this.connection.on('UserJoinedChat', (username, chatRoom) => {
            console.log('User Joined Chat:', username);
            this.addOrUpdateChatRoom(chatRoom);
        });

        this.connection.on('UserLeftChat', (username, chatRoom) => {
            console.log('User Left Chat:', username);
            this.addOrUpdateChatRoom(chatRoom);
        });

        this.connection.on('MessageReceived', (message) => {
            console.log('Message Received:', message);
            this.handleNewMessage(message);
        });

        this.connection.on('ChatHistoryLoaded', (messages) => {
            console.log('Chat History Loaded:', messages);
            this.renderMessages(messages);
        });

        this.connection.on('UserTyping', (username, chatRoomId) => {
            if (this.currentChatRoom && this.currentChatRoom.id === chatRoomId) {
                this.showTypingIndicator(username);
            }
        });

        this.connection.on('UserStoppedTyping', (username, chatRoomId) => {
            if (this.currentChatRoom && this.currentChatRoom.id === chatRoomId) {
                this.hideTypingIndicator();
            }
        });

        this.connection.on('Error', (message) => {
            console.error('Error:', message);
            alert(message);
        });
    }

    async handleLogin() {
        const username = this.usernameInput.value.trim();
        if (!username) {
            alert('Please enter a username');
            return;
        }

        this.currentUser = { username: username };
        this.loginModal.style.display = 'none';
        
        await this.initializeConnection();
    }

    updateCurrentUserDisplay() {
        if (this.currentUser) {
            this.currentUserDisplay.textContent = `👤 ${this.currentUser.username}`;
        }
    }

    renderUserList(users) {
        this.userListContainer.innerHTML = '';
        
        users.forEach(user => {
            const userItem = document.createElement('div');
            userItem.className = 'user-item';
            userItem.innerHTML = `
                <div class="user-avatar ${user.isOnline ? 'online' : ''}">
                    ${(user.displayName || user.username)[0].toUpperCase()}
                </div>
                <div class="user-info-item">
                    <div class="user-name">${this.escapeHtml(user.displayName || user.username)}</div>
                    <div class="user-status">${user.isOnline ? 'Online' : 'Offline'}</div>
                </div>
            `;
            
            userItem.addEventListener('click', () => {
                if (user.username !== this.currentUser.username) {
                    this.createPrivateChat(user.username);
                }
            });
            
            this.userListContainer.appendChild(userItem);
        });
    }

    updateUserStatus(username, isOnline) {
        const user = this.users.find(u => u.username === username);
        if (user) {
            user.isOnline = isOnline;
            this.renderUserList(this.users);
        }
    }

    async searchUsers(searchTerm) {
        await this.connection.invoke('SearchUsers', searchTerm);
    }

    async createPrivateChat(targetUsername) {
        await this.connection.invoke('CreatePrivateChat', targetUsername);
    }

    openCreateGroupModal() {
        this.groupNameInput.value = '';
        this.renderParticipantList();
        this.createGroupModal.classList.add('show');
    }

    closeModals() {
        this.createGroupModal.classList.remove('show');
    }

    renderParticipantList() {
        this.participantsList.innerHTML = '';
        
        this.users.filter(u => u.username !== this.currentUser.username).forEach(user => {
            const checkbox = document.createElement('div');
            checkbox.className = 'participant-checkbox';
            checkbox.innerHTML = `
                <input type="checkbox" id="user_${user.id}" value="${user.username}">
                <label for="user_${user.id}">${this.escapeHtml(user.displayName || user.username)}</label>
            `;
            this.participantsList.appendChild(checkbox);
        });
    }

    async createGroupChat() {
        const name = this.groupNameInput.value.trim();
        if (!name) {
            alert('Please enter a group name');
            return;
        }

        const checkboxes = this.participantsList.querySelectorAll('input[type="checkbox"]:checked');
        const participants = Array.from(checkboxes).map(cb => cb.value);

        if (participants.length === 0) {
            alert('Please select at least one participant');
            return;
        }

        await this.connection.invoke('CreateGroupChat', name, participants);
        this.closeModals();
    }

    addOrUpdateChatRoom(chatRoom) {
        const existingIndex = this.chatRooms.findIndex(cr => cr.id === chatRoom.id);
        
        if (existingIndex >= 0) {
            this.chatRooms[existingIndex] = chatRoom;
        } else {
            this.chatRooms.unshift(chatRoom);
        }
        
        this.renderChatRoomsList();
    }

    removeChatRoom(chatRoomId) {
        this.chatRooms = this.chatRooms.filter(cr => cr.id !== chatRoomId);
        this.renderChatRoomsList();
    }

    renderChatRoomsList() {
        this.chatRoomsContainer.innerHTML = '';
        
        this.chatRooms.forEach(chatRoom => {
            const chatRoomItem = document.createElement('div');
            chatRoomItem.className = `chat-room-item ${this.currentChatRoom && this.currentChatRoom.id === chatRoom.id ? 'active' : ''}`;
            
            const isGroup = chatRoom.type === 2;
            const icon = isGroup ? '👥' : '💬';
            const iconClass = isGroup ? 'group' : '';
            
            // Get other participants (excluding current user)
            const otherParticipants = chatRoom.participants.filter(p => p.username !== this.currentUser.username);
            const displayName = isGroup ? chatRoom.name : (otherParticipants[0]?.displayName || otherParticipants[0]?.username);
            
            chatRoomItem.innerHTML = `
                <div class="chat-room-icon ${iconClass}">${icon}</div>
                <div class="chat-room-info">
                    <div class="chat-room-name">${this.escapeHtml(displayName || 'Unknown')}</div>
                    <div class="chat-room-last-message">${chatRoom.lastMessage || 'No messages yet'}</div>
                </div>
            `;
            
            chatRoomItem.addEventListener('click', () => {
                this.selectChatRoom(chatRoom);
            });
            
            this.chatRoomsContainer.appendChild(chatRoomItem);
        });
    }

    async selectChatRoom(chatRoom) {
        this.currentChatRoom = chatRoom;
        
        // Update UI
        this.emptyChatView.style.display = 'none';
        this.activeChatView.style.display = 'flex';
        this.activeChatView.style.flexDirection = 'column';
        
        // Update header
        const isGroup = chatRoom.type === 2;
        const otherParticipants = chatRoom.participants.filter(p => p.username !== this.currentUser.username);
        const displayName = isGroup ? chatRoom.name : (otherParticipants[0]?.displayName || otherParticipants[0]?.username);
        
        this.chatHeaderName.textContent = displayName || 'Unknown';
        this.chatHeaderAvatar.textContent = isGroup ? '👥' : (displayName ? displayName[0].toUpperCase() : '?');
        this.chatHeaderStatus.textContent = isGroup ? `${chatRoom.participants.length} members` : 'Private chat';
        
        // Show/Hide leave button (only for group chats)
        this.leaveChatBtn.style.display = isGroup ? 'inline-block' : 'none';
        
        // Update active state in list
        this.renderChatRoomsList();
        
        // Join the chat room group
        await this.connection.invoke('JoinChatRoomGroup', chatRoom.id);
        
        // Load chat history
        await this.connection.invoke('GetChatHistory', chatRoom.id);
        
        // Clear messages container
        this.messagesContainer.innerHTML = '';
        
        // Mark messages as read
        await this.connection.invoke('MarkMessagesRead', chatRoom.id);
    }

    showEmptyChat() {
        this.emptyChatView.style.display = 'flex';
        this.activeChatView.style.display = 'none';
        this.currentChatRoom = null;
        this.renderChatRoomsList();
    }

    renderMessages(messages) {
        this.messagesContainer.innerHTML = '';
        
        messages.forEach(message => {
            this.appendMessage(message);
        });
        
        this.scrollToBottom();
    }

    handleNewMessage(message) {
        // Only display if it's for the current chat room
        if (this.currentChatRoom && message.chatRoomId === this.currentChatRoom.id) {
            this.appendMessage(message);
            this.scrollToBottom();
            
            // Update last message in chat rooms list
            const chatRoom = this.chatRooms.find(cr => cr.id === message.chatRoomId);
            if (chatRoom) {
                chatRoom.lastMessage = `${message.senderUsername}: ${message.content}`;
                this.renderChatRoomsList();
            }
            
            // Mark as read if we're viewing this chat
            if (message.senderUserId !== this.currentUser.id) {
                this.connection.invoke('MarkMessagesRead', message.chatRoomId);
            }
        }
    }

    appendMessage(message) {
        const isSent = message.senderUserId === this.currentUser.id;
        const messageEl = document.createElement('div');
        messageEl.className = `message ${isSent ? 'sent' : 'received'}`;
        
        const time = new Date(message.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        
        messageEl.innerHTML = `
            <div class="message-avatar">
                ${(message.senderDisplayName || message.senderUsername)[0].toUpperCase()}
            </div>
            <div class="message-content">
                <div class="message-bubble">${this.escapeHtml(message.content)}</div>
                <div class="message-meta">${time}</div>
            </div>
        `;
        
        this.messagesContainer.appendChild(messageEl);
    }

    async sendMessage() {
        const content = this.messageInput.value.trim();
        if (!content || !this.currentChatRoom) return;

        try {
            await this.connection.invoke('SendMessage', this.currentChatRoom.id, content);
            this.messageInput.value = '';
            
            // Stop typing indicator
            await this.connection.invoke('TypingStop', this.currentChatRoom.id);
        } catch (err) {
            console.error('Send Message Error:', err);
        }
    }

    async handleTyping() {
        if (!this.currentChatRoom) return;

        // Clear existing timeout
        if (this.typingTimeout) {
            clearTimeout(this.typingTimeout);
        }

        // Send typing start
        await this.connection.invoke('TypingStart', this.currentChatRoom.id);

        // Set timeout to stop typing
        this.typingTimeout = setTimeout(async () => {
            await this.connection.invoke('TypingStop', this.currentChatRoom.id);
        }, 2000);
    }

    showTypingIndicator(username) {
        this.typingIndicator.textContent = `${username} is typing...`;
        this.typingIndicator.style.display = 'block';
    }

    hideTypingIndicator() {
        this.typingIndicator.style.display = 'none';
    }

    async leaveCurrentChat() {
        if (!this.currentChatRoom) return;
        
        if (confirm('Are you sure you want to leave this chat?')) {
            await this.connection.invoke('LeaveChat', this.currentChatRoom.id);
            await this.connection.invoke('LeaveChatRoomGroup', this.currentChatRoom.id);
            this.showEmptyChat();
        }
    }

    scrollToBottom() {
        this.messagesContainer.scrollTop = this.messagesContainer.scrollHeight;
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Initialize the app when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.chatApp = new ChatApp();
});
