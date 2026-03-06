using System;

namespace ChatApp.Shared.Models;

public class Message
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SenderId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    public bool IsEdited { get; set; }
}

public enum MessageType
{
    Text,
    Image,
    Document
}

public enum MessageStatus
{
    Sent,
    Delivered,
    Read
}
