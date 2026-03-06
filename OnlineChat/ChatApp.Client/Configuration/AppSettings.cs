using System;
using System.IO;
using System.Text.Json;

namespace ChatApp.Client.Configuration;

public class AppSettings
{
    public string ServerUrl { get; set; } = "http://localhost:56188";

    private static AppSettings? _instance;
    
    public static AppSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Load();
            }
            return _instance;
        }
    }

    private static AppSettings Load()
    {
        try
        {
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
        }
        catch
        {
            // Если не удалось загрузить - используем значения по умолчанию
        }
        
        return new AppSettings();
    }
}
