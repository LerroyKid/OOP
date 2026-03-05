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

        if (!string.IsNullOrEmpty(request.Username))
            user.Username = request.Username;
        
        if (!string.IsNullOrEmpty(request.Bio))
            user.Bio = request.Bio;
        
        if (!string.IsNullOrEmpty(request.AvatarUrl))
            user.AvatarUrl = request.AvatarUrl;

        await _context.SaveChangesAsync();
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
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}
