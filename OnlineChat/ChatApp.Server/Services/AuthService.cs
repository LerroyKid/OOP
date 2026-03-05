using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ChatApp.Server.Data;
using ChatApp.Shared.DTOs;
using ChatApp.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ChatApp.Server.Services;

public class AuthService : IAuthService
{
    private readonly ChatDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ChatDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            Console.WriteLine($"⚠️ Попытка регистрации с существующим email: {request.Email}");
            return new LoginResponse { Success = false, Error = "Пользователь с таким email уже существует" };
        }

        var passwordHash = HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            Username = request.Username,
            PasswordHash = passwordHash,
            Status = UserStatus.Offline
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        Console.WriteLine($"✅ Новый пользователь зарегистрирован: {user.Username} ({user.Email})");

        var token = GenerateJwtToken(user);
        return new LoginResponse { Success = true, Token = token, UserId = user.Id };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        Console.WriteLine($"🔍 Попытка входа: {request.Email}");
        
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            Console.WriteLine($"❌ Пользователь не найден: {request.Email}");
            return new LoginResponse { Success = false, Error = "Неверный email или пароль" };
        }

        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            Console.WriteLine($"❌ Неверный пароль для: {request.Email}");
            return new LoginResponse { Success = false, Error = "Неверный email или пароль" };
        }

        Console.WriteLine($"✅ Успешный вход: {user.Username} ({user.Email})");
        
        var token = GenerateJwtToken(user);
        return new LoginResponse { Success = true, Token = token, UserId = user.Id };
    }

    public async Task<LoginResponse> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return new LoginResponse { Success = false, Error = "User not found" };

        if (!VerifyPassword(currentPassword, user.PasswordHash))
            return new LoginResponse { Success = false, Error = "Current password is incorrect" };

        user.PasswordHash = HashPassword(newPassword);
        await _context.SaveChangesAsync();

        return new LoginResponse { Success = true };
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private bool VerifyPassword(string password, string passwordHash)
    {
        var hash = HashPassword(password);
        return hash == passwordHash;
    }
}
