using System;
using System.Threading.Tasks;
using ChatApp.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatApp.Client.Services;

public class ChatService
{
    private HubConnection? _connection;
    public event Action<Message>? MessageReceived;
    public event Action<string, MessageStatus>? MessageStatusUpdated;
    public event Action<string, UserStatus>? UserStatusChanged;
    public event Action<Message>? MessageSent;
    public event Action<string, string>? MessageEdited;
    public event Action<string>? MessageDeleted;

    public async Task ConnectAsync(string serverUrl, string token)
    {
        try
        {
            Console.WriteLine($"[ChatService] Подключение к {serverUrl}...");
            
            _connection = new HubConnectionBuilder()
                .WithUrl($"{serverUrl}/chathub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.On<Message>("ReceiveMessage", message =>
            {
                Console.WriteLine($"[ChatService] ReceiveMessage: {message.Id}");
                MessageReceived?.Invoke(message);
            });

            _connection.On<Message>("MessageSent", message =>
            {
                Console.WriteLine($"[ChatService] MessageSent: {message.Id}, Status: {message.Status}");
                MessageSent?.Invoke(message);
            });

            _connection.On<string, MessageStatus>("MessageStatusUpdated", (messageId, status) =>
            {
                Console.WriteLine($"[ChatService] MessageStatusUpdated: {messageId}, Status: {status}");
                MessageStatusUpdated?.Invoke(messageId, status);
            });

            _connection.On<string, UserStatus>("UserStatusChanged", (userId, status) =>
            {
                Console.WriteLine($"[ChatService] UserStatusChanged: {userId}, Status: {status}");
                UserStatusChanged?.Invoke(userId, status);
            });

            _connection.On<string, string>("MessageEdited", (messageId, newContent) =>
            {
                Console.WriteLine($"[ChatService] MessageEdited: {messageId}");
                MessageEdited?.Invoke(messageId, newContent);
            });

            _connection.On<string>("MessageDeleted", (messageId) =>
            {
                Console.WriteLine($"[ChatService] MessageDeleted: {messageId}");
                MessageDeleted?.Invoke(messageId);
            });

            await _connection.StartAsync();
            Console.WriteLine($"[ChatService] ✓ Подключено! State: {_connection.State}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatService] ✗ Ошибка подключения: {ex.Message}");
            throw new Exception($"Failed to connect to SignalR hub: {ex.Message}", ex);
        }
    }

    public async Task SendMessageAsync(Message message)
    {
        if (_connection != null && _connection.State == HubConnectionState.Connected)
            await _connection.InvokeAsync("SendMessage", message);
    }

    public async Task UpdateMessageStatusAsync(string messageId, MessageStatus status)
    {
        if (_connection != null && _connection.State == HubConnectionState.Connected)
            await _connection.InvokeAsync("UpdateMessageStatus", messageId, status);
    }

    public async Task UpdateUserStatusAsync(string userId, UserStatus status)
    {
        if (_connection != null && _connection.State == HubConnectionState.Connected)
            await _connection.InvokeAsync("UpdateUserStatus", userId, status);
    }

    public async Task EditMessageAsync(string messageId, string newContent)
    {
        if (_connection != null && _connection.State == HubConnectionState.Connected)
            await _connection.InvokeAsync("EditMessage", messageId, newContent);
    }

    public async Task DeleteMessageAsync(string messageId)
    {
        if (_connection != null && _connection.State == HubConnectionState.Connected)
            await _connection.InvokeAsync("DeleteMessage", messageId);
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
            await _connection.StopAsync();
    }
}
