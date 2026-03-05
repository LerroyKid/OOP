using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ChatApp.Server;
using ChatApp.Server.Data;
using ChatApp.Server.Hubs;
using ChatApp.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

// Проверка аргументов командной строки для удаления дубликатов
if (args.Length > 0 && args[0] == "delete-duplicate")
{
    if (args.Length < 2)
    {
        Console.WriteLine("Использование: dotnet run delete-duplicate <email>");
        return;
    }
    
    await DeleteDuplicateUser.DeleteUserByEmail(args[1]);
    return;
}

// Удаление пользователя по username
if (args.Length > 0 && args[0] == "delete-user")
{
    if (args.Length < 2)
    {
        Console.WriteLine("Использование: dotnet run delete-user <username>");
        return;
    }
    
    await DeleteDuplicateUser.DeleteUserByUsername(args[1]);
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Настройка URL для прослушивания всех сетевых интерфейсов
builder.WebHost.UseUrls("http://0.0.0.0:56188", "http://localhost:56188");

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMessageService, MessageService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSecretKeyHere123456789012345678901234567890";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        
        // Настройка для SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
                {
                    context.Token = accessToken;
                }
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Создаем папку для загрузок
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
    Directory.CreateDirectory(uploadsPath);

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/chathub");

app.Run();
