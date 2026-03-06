using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FileController : ControllerBase
{
    private readonly string _uploadPath;

    public FileController(IWebHostEnvironment env)
    {
        _uploadPath = Path.Combine(env.ContentRootPath, "uploads");
        if (!Directory.Exists(_uploadPath))
            Directory.CreateDirectory(_uploadPath);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(_uploadPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var fileUrl = $"/api/file/download/{fileName}";
        return Ok(new { fileName, fileUrl, originalName = file.FileName, size = file.Length });
    }

    [HttpGet("download/{fileName}")]
    [AllowAnonymous]
    public IActionResult Download(string fileName)
    {
        var filePath = Path.Combine(_uploadPath, fileName);
        
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        var contentType = GetContentType(fileName);
        
        return File(fileBytes, contentType, Path.GetFileName(fileName));
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}
