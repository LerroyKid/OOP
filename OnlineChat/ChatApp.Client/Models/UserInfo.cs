using ChatApp.Shared.Models;

namespace ChatApp.Client.Views;

public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
}
