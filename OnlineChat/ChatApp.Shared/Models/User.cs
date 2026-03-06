using System;

namespace ChatApp.Shared.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Offline;
    public string? Bio { get; set; }
}

public enum UserStatus
{
    Online,
    Offline,
    DoNotDisturb
}
