using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatApp.Server.Data;
using ChatApp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ChatDbContext _context;

    public UserController(ChatDbContext context)
    {
        _context = context;
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllUsers()
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        var users = await _context.Users
            .Where(u => u.Id != currentUserId)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.Status,
                u.AvatarUrl
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("debug/all-emails")]
    public async Task<IActionResult> GetAllEmails()
    {
        // Временный endpoint для отладки
        var users = await _context.Users
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                EmailLower = u.Email.ToLower()
            })
            .ToListAsync();

        Console.WriteLine("📋 Все пользователи в базе:");
        foreach (var u in users)
        {
            Console.WriteLine($"   Id={u.Id}, Username={u.Username}, Email={u.Email}, EmailLower={u.EmailLower}");
        }

        return Ok(users);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            user.Status,
            user.AvatarUrl,
            user.Bio
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _context.Users.FindAsync(currentUserId);
        
        if (user == null)
            return NotFound();

        Console.WriteLine($"📝 UpdateProfile вызван: userId={currentUserId}, текущий email={user.Email}");
        Console.WriteLine($"📝 Запрос: Username={request.Username}, Email={request.Email}");

        // Проверка на уникальность email если он изменился
        if (!string.IsNullOrEmpty(request.Email))
        {
            var emailChanged = !string.Equals(request.Email, user.Email, StringComparison.OrdinalIgnoreCase);
            Console.WriteLine($"🔍 Email изменился: {emailChanged} (старый={user.Email}, новый={request.Email})");
            
            if (emailChanged)
            {
                // Получаем всех пользователей с таким email (для отладки)
                var usersWithEmail = await _context.Users
                    .Where(u => u.Email.ToLower() == request.Email.ToLower())
                    .Select(u => new { u.Id, u.Email, u.Username })
                    .ToListAsync();
                
                Console.WriteLine($"🔍 Найдено пользователей с email '{request.Email}': {usersWithEmail.Count}");
                foreach (var u in usersWithEmail)
                {
                    Console.WriteLine($"   - Id={u.Id}, Email={u.Email}, Username={u.Username}, Это я={u.Id == currentUserId}");
                }
                
                // Проверяем есть ли ДРУГОЙ пользователь с таким email
                var emailExists = usersWithEmail.Any(u => u.Id != currentUserId);
                Console.WriteLine($"🔍 Email занят другим пользователем: {emailExists}");
                
                if (emailExists)
                {
                    Console.WriteLine($"❌ БЛОКИРОВКА: Попытка использовать занятый email: {request.Email}");
                    return BadRequest("Этот email уже используется другим пользователем");
                }
                
                user.Email = request.Email;
                Console.WriteLine($"✅ Email будет обновлен: {user.Email}");
            }
        }

        if (!string.IsNullOrEmpty(request.Username))
            user.Username = request.Username;
        
        if (!string.IsNullOrEmpty(request.Bio))
            user.Bio = request.Bio;
        
        if (!string.IsNullOrEmpty(request.AvatarUrl))
            user.AvatarUrl = request.AvatarUrl;

        // ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА ПЕРЕД СОХРАНЕНИЕМ
        if (!string.IsNullOrEmpty(request.Email))
        {
            var finalCheck = await _context.Users
                .Where(u => u.Email.ToLower() == request.Email.ToLower() && u.Id != currentUserId)
                .ToListAsync();
            
            if (finalCheck.Any())
            {
                Console.WriteLine($"⚠️ КРИТИЧЕСКАЯ ОШИБКА: Перед сохранением найден дубликат!");
                foreach (var dup in finalCheck)
                {
                    Console.WriteLine($"   Дубликат: Id={dup.Id}, Email={dup.Email}, Username={dup.Username}");
                }
                return BadRequest("Этот email уже используется другим пользователем (финальная проверка)");
            }
        }

        await _context.SaveChangesAsync();
        Console.WriteLine($"💾 Профиль сохранен для пользователя {currentUserId}");
        
        // Проверка ПОСЛЕ сохранения
        var duplicatesAfter = await _context.Users
            .Where(u => u.Email.ToLower() == user.Email.ToLower())
            .Select(u => new { u.Id, u.Email, u.Username })
            .ToListAsync();
        
        Console.WriteLine($"🔍 После сохранения найдено пользователей с email '{user.Email}': {duplicatesAfter.Count}");
        foreach (var u in duplicatesAfter)
        {
            Console.WriteLine($"   - Id={u.Id}, Email={u.Email}, Username={u.Username}");
        }
        
        return Ok(user);
    }

    [HttpPut("status/{userId}")]
    public async Task<IActionResult> UpdateUserStatus(string userId, [FromBody] UserStatus status)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        user.Status = status;
        await _context.SaveChangesAsync();
        return Ok();
    }
}

public class UpdateProfileRequest
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}
