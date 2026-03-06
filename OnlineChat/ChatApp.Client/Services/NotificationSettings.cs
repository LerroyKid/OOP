using System;
using System.IO;
using System.Text.Json;

namespace ChatApp.Client.Services;

public class NotificationSettings
{
    private static NotificationSettings? _instance;
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatApp",
        "notification-settings.json"
    );

    public bool SoundEnabled { get; set; } = true;
    public bool BannerEnabled { get; set; } = true;
    public bool SmartNotificationsEnabled { get; set; } = true;

    public static NotificationSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Load();
                Console.WriteLine($"📋 NotificationSettings загружены впервые");
            }
            return _instance;
        }
    }

    private static NotificationSettings Load()
    {
        try
        {
            Console.WriteLine($"📂 Попытка загрузить настройки из: {SettingsPath}");
            
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                Console.WriteLine($"📄 Содержимое файла: {json}");
                var settings = JsonSerializer.Deserialize<NotificationSettings>(json) ?? new NotificationSettings();
                Console.WriteLine($"✅ Настройки загружены: Sound={settings.SoundEnabled}, Banner={settings.BannerEnabled}, Smart={settings.SmartNotificationsEnabled}");
                return settings;
            }
            else
            {
                Console.WriteLine($"⚠️ Файл настроек не найден, используем значения по умолчанию");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка загрузки настроек: {ex.Message}");
        }
        
        var defaultSettings = new NotificationSettings();
        Console.WriteLine($"📋 Используем настройки по умолчанию: Sound={defaultSettings.SoundEnabled}, Banner={defaultSettings.BannerEnabled}, Smart={defaultSettings.SmartNotificationsEnabled}");
        return defaultSettings;
    }

    public void Save()
    {
        try
        {
            Console.WriteLine($"💾 Сохранение настроек в: {SettingsPath}");
            Console.WriteLine($"   Sound={SoundEnabled}, Banner={BannerEnabled}, Smart={SmartNotificationsEnabled}");
            
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Console.WriteLine($"📁 Создаем директорию: {directory}");
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine($"📄 JSON для сохранения: {json}");
            
            File.WriteAllText(SettingsPath, json);
            
            Console.WriteLine($"✅ Настройки успешно сохранены");
            
            // Проверяем что файл действительно сохранился
            if (File.Exists(SettingsPath))
            {
                var savedContent = File.ReadAllText(SettingsPath);
                Console.WriteLine($"✓ Проверка: файл существует, содержимое: {savedContent}");
            }
            else
            {
                Console.WriteLine($"❌ ОШИБКА: Файл не был сохранен!");
            }
            
            // НЕ перезагружаем _instance, так как this уже содержит актуальные данные
            // и является тем же объектом что и _instance
            Console.WriteLine($"✓ Кеш остается актуальным (this === _instance)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка сохранения настроек уведомлений: {ex.Message}");
            Console.WriteLine($"   StackTrace: {ex.StackTrace}");
        }
    }
    
    // Метод для принудительной перезагрузки настроек из файла
    public static void Reload()
    {
        _instance = Load();
        Console.WriteLine($"🔄 Настройки перезагружены: Sound={_instance.SoundEnabled}, Banner={_instance.BannerEnabled}, Smart={_instance.SmartNotificationsEnabled}");
    }
}
