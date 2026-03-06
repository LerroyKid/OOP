using System.Collections.Generic;
using System.Threading.Tasks;
using ChatApp.Shared.Models;

namespace ChatApp.Server.Services;

public interface IMessageService
{
    Task<Message> SendMessageAsync(Message message);
    Task<List<Message>> GetConversationAsync(string userId1, string userId2);
    Task<Message?> EditMessageAsync(string messageId, string newContent);
    Task<bool> DeleteMessageAsync(string messageId);
    Task<bool> UpdateMessageStatusAsync(string messageId, MessageStatus status);
}
