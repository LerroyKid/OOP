using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatApp.Server.Data;
using ChatApp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Server.Services;

public class MessageService : IMessageService
{
    private readonly ChatDbContext _context;

    public MessageService(ChatDbContext context)
    {
        _context = context;
    }

    public async Task<Message> SendMessageAsync(Message message)
    {
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<List<Message>> GetConversationAsync(string userId1, string userId2)
    {
        return await _context.Messages
            .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                       (m.SenderId == userId2 && m.ReceiverId == userId1))
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task<Message?> EditMessageAsync(string messageId, string newContent)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message != null)
        {
            message.Content = newContent;
            message.IsEdited = true;
            await _context.SaveChangesAsync();
        }
        return message;
    }

    public async Task<bool> DeleteMessageAsync(string messageId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message != null)
        {
            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<bool> UpdateMessageStatusAsync(string messageId, MessageStatus status)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message != null)
        {
            message.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }
}
