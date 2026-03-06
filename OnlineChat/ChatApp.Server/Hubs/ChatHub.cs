using System;
using System.Threading.Tasks;
using ChatApp.Server.Data;
using ChatApp.Server.Services;
using ChatApp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Server.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly ChatDbContext _context;

    public ChatHub(IMessageService messageService, ChatDbContext context)
    {
        _messageService = messageService;
        _context = context;
    }

    public async Task SendMessage(Message message)
    {
        // Сохраняем сообщение в БД
        message.Id = Guid.NewGuid().ToString();
        message.Timestamp = DateTime.UtcNow;
        message.Status = MessageStatus.Sent;
        
        var savedMessage = await _messageService.SendMessageAsync(message);
        
        Console.WriteLine($"💬 Сообщение от {message.SenderId} к {message.ReceiverId}: {message.Content}");
        Console.WriteLine($"📤 Отправляем MessageSent отправителю (Caller)");
        
        // Отправляем получателю
        await Clients.User(message.ReceiverId).SendAsync("ReceiveMessage", savedMessage);
        
        // Также отправляем отправителю для подтверждения
        await Clients.Caller.SendAsync("MessageSent", savedMessage);
        
        Console.WriteLine($"✓ MessageSent отправлено с ID: {savedMessage.Id}");
    }

    public async Task UpdateMessageStatus(string messageId, MessageStatus status)
    {
        Console.WriteLine($"📊 UpdateMessageStatus: ID={messageId}, Status={status}");
        
        // Сохраняем статус в БД
        await _messageService.UpdateMessageStatusAsync(messageId, status);
        
        // Отправляем обновление всем клиентам
        await Clients.All.SendAsync("MessageStatusUpdated", messageId, status);
    }

    public async Task UpdateUserStatus(string userId, UserStatus status)
    {
        Console.WriteLine($"🔄 UpdateUserStatus: userId={userId}, status={status}");
        
        // Сохраняем статус в БД
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.Status = status;
            await _context.SaveChangesAsync();
            Console.WriteLine($"✓ Статус сохранен в БД");
        }
        
        // Отправляем обновление всем клиентам
        await Clients.All.SendAsync("UserStatusChanged", userId, status);
        Console.WriteLine($"✓ UserStatusChanged отправлено всем клиентам");
    }

    public async Task EditMessage(string messageId, string newContent)
    {
        Console.WriteLine($"✏️ EditMessage: ID={messageId}, новый текст={newContent}");
        
        var message = await _messageService.EditMessageAsync(messageId, newContent);
        if (message != null)
        {
            Console.WriteLine($"✓ Сообщение отредактировано");
            // Отправляем обновление всем клиентам
            await Clients.All.SendAsync("MessageEdited", messageId, newContent);
        }
        else
        {
            Console.WriteLine($"✗ Сообщение не найдено");
        }
    }

    public async Task DeleteMessage(string messageId)
    {
        Console.WriteLine($"🗑️ DeleteMessage: ID={messageId}");
        
        var deleted = await _messageService.DeleteMessageAsync(messageId);
        if (deleted)
        {
            Console.WriteLine($"✓ Сообщение удалено");
            // Отправляем обновление всем клиентам
            await Clients.All.SendAsync("MessageDeleted", messageId);
        }
        else
        {
            Console.WriteLine($"✗ Сообщение не найдено");
        }
    }
    
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            Console.WriteLine($"✅ Пользователь {userId} подключился. ConnectionId: {Context.ConnectionId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            Console.WriteLine($"❌ Пользователь {userId} отключился. ConnectionId: {Context.ConnectionId}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}
