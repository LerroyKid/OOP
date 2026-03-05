using System.Threading.Tasks;
using ChatApp.Server.Services;
using ChatApp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessageController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpGet("conversation/{userId}")]
    public async Task<IActionResult> GetConversation(string userId)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId == null) return Unauthorized();

        var messages = await _messageService.GetConversationAsync(currentUserId, userId);
        return Ok(messages);
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] Message message)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId == null) return Unauthorized();

        // Проверяем что отправитель - это текущий пользователь
        if (message.SenderId != currentUserId)
            return BadRequest("Invalid sender");

        var savedMessage = await _messageService.SendMessageAsync(message);
        return Ok(savedMessage);
    }

    [HttpPut("{messageId}")]
    public async Task<IActionResult> EditMessage(string messageId, [FromBody] string newContent)
    {
        var message = await _messageService.EditMessageAsync(messageId, newContent);
        return message != null ? Ok(message) : NotFound();
    }

    [HttpDelete("{messageId}")]
    public async Task<IActionResult> DeleteMessage(string messageId)
    {
        var result = await _messageService.DeleteMessageAsync(messageId);
        return result ? Ok() : NotFound();
    }
}
