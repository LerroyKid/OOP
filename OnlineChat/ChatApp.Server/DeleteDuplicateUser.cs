using System;
using System.Linq;
using System.Threading.Tasks;
using ChatApp.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Server;

public class DeleteDuplicateUser
{
    public static async Task DeleteUserByEmail(string email)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ChatDbContext>();
        optionsBuilder.UseSqlite("Data Source=chatapp.db");

        using var context = new ChatDbContext(optionsBuilder.Options);

        // Находим всех пользователей с этим email
        var users = await context.Users.Where(u => u.Email == email).ToListAsync();

        Console.WriteLine($"Найдено пользователей с email '{email}': {users.Count}");

        if (users.Count == 0)
        {
            Console.WriteLine("Пользователи не найдены.");
            return;
        }

        // Показываем всех найденных пользователей
        foreach (var user in users)
        {
            Console.WriteLine($"ID: {user.Id}, Email: {user.Email}, Username: {user.Username}");
        }

        // Если больше одного - удаляем дубликаты (оставляем первого)
        if (users.Count > 1)
        {
            Console.WriteLine($"\nОставляем первого пользователя (ID: {users[0].Id})");
            Console.WriteLine("Удаляем дубликаты...");

            for (int i = 1; i < users.Count; i++)
            {
                var userToDelete = users[i];
                Console.WriteLine($"Удаление пользователя ID: {userToDelete.Id}");

                // Удаляем сообщения
                var sentMessages = await context.Messages.Where(m => m.SenderId == userToDelete.Id).ToListAsync();
                var receivedMessages = await context.Messages.Where(m => m.ReceiverId == userToDelete.Id).ToListAsync();

                Console.WriteLine($"  - Удаление {sentMessages.Count} отправленных сообщений");
                context.Messages.RemoveRange(sentMessages);

                Console.WriteLine($"  - Удаление {receivedMessages.Count} полученных сообщений");
                context.Messages.RemoveRange(receivedMessages);

                // Удаляем пользователя
                context.Users.Remove(userToDelete);
            }

            await context.SaveChangesAsync();
            Console.WriteLine("\nДубликаты успешно удалены!");
        }
        else
        {
            Console.WriteLine("\nДубликатов не найдено.");
        }

        // Проверка
        var remainingUsers = await context.Users.Where(u => u.Email == email).ToListAsync();
        Console.WriteLine($"\nОсталось пользователей с email '{email}': {remainingUsers.Count}");
    }

    public static async Task DeleteUserByUsername(string username)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ChatDbContext>();
        optionsBuilder.UseSqlite("Data Source=chatapp.db");

        using var context = new ChatDbContext(optionsBuilder.Options);

        // Находим всех пользователей с этим username
        var users = await context.Users.Where(u => u.Username.Contains(username)).ToListAsync();

        Console.WriteLine($"Найдено пользователей с username содержащим '{username}': {users.Count}");

        if (users.Count == 0)
        {
            Console.WriteLine("Пользователи не найдены.");
            return;
        }

        // Показываем всех найденных пользователей
        Console.WriteLine("\nНайденные пользователи:");
        for (int i = 0; i < users.Count; i++)
        {
            var user = users[i];
            Console.WriteLine($"{i + 1}. ID: {user.Id}");
            Console.WriteLine($"   Email: {user.Email}");
            Console.WriteLine($"   Username: {user.Username}");
            Console.WriteLine();
        }

        if (users.Count == 1)
        {
            Console.Write($"Удалить этого пользователя? (y/n): ");
            var answer = Console.ReadLine();
            if (answer?.ToLower() != "y")
            {
                Console.WriteLine("Отменено.");
                return;
            }

            await DeleteUser(context, users[0]);
        }
        else
        {
            Console.Write($"Введите номер пользователя для удаления (1-{users.Count}) или 0 для отмены: ");
            if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= users.Count)
            {
                await DeleteUser(context, users[choice - 1]);
            }
            else
            {
                Console.WriteLine("Отменено.");
            }
        }
    }

    private static async Task DeleteUser(ChatDbContext context, ChatApp.Shared.Models.User user)
    {
        Console.WriteLine($"\nУдаление пользователя: {user.Username} ({user.Email})");

        // Удаляем сообщения
        var sentMessages = await context.Messages.Where(m => m.SenderId == user.Id).ToListAsync();
        var receivedMessages = await context.Messages.Where(m => m.ReceiverId == user.Id).ToListAsync();

        Console.WriteLine($"  - Удаление {sentMessages.Count} отправленных сообщений");
        context.Messages.RemoveRange(sentMessages);

        Console.WriteLine($"  - Удаление {receivedMessages.Count} полученных сообщений");
        context.Messages.RemoveRange(receivedMessages);

        // Удаляем пользователя
        context.Users.Remove(user);

        await context.SaveChangesAsync();
        Console.WriteLine("\n✅ Пользователь успешно удален!");
    }
}
