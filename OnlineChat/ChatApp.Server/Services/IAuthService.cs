using System.Threading.Tasks;
using ChatApp.Shared.DTOs;

namespace ChatApp.Server.Services;

public interface IAuthService
{
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
}
