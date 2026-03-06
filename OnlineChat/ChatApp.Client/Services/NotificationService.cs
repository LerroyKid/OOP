using System;
using System.Media;
using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

namespace ChatApp.Client.Services;

public class NotificationService
{
    public NotificationService()
    {
        // Регистрируем приложение для уведомлений
        try
        {
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                // Обработка клика по уведомлению (опционально)
                Console.WriteLine("Уведомление активировано");
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка регистрации уведомлений: {ex.Message}");
        }
    }

    public void ShowNotification(string title, string message, bool isActiveDialog = false)
    {
        // Читаем актуальные настройки каждый раз
        var settings = NotificationSettings.Instance;
        
        Console.WriteLine($"🔔 ShowNotification вызван:");
        Console.WriteLine($"   Title: {title}");
        Console.WriteLine($"   Message: {message}");
        Console.WriteLine($"   isActiveDialog: {isActiveDialog}");
        Console.WriteLine($"   SoundEnabled: {settings.SoundEnabled}");
        Console.WriteLine($"   BannerEnabled: {settings.BannerEnabled}");
        Console.WriteLine($"   SmartNotificationsEnabled: {settings.SmartNotificationsEnabled}");
        
        // Умные уведомления: не показывать если это текущий открытый диалог
        if (settings.SmartNotificationsEnabled && isActiveDialog)
        {
            Console.WriteLine("🔕 Уведомление пропущено (текущий диалог + умные уведомления включены)");
            return;
        }

        // Звук
        if (settings.SoundEnabled)
        {
            Console.WriteLine("🔊 Воспроизводим звук");
            PlayNotificationSound();
        }

        // Баннер (нативное уведомление Windows)
        if (settings.BannerEnabled)
        {
            bool nativeSuccess = false;
            
            try
            {
                Console.WriteLine("📢 Попытка показать нативное уведомление Windows");
                
                var toastContent = new ToastContentBuilder()
                    .AddText(title)
                    .AddText(message)
                    .SetToastDuration(ToastDuration.Short);
                
                // Отключаем звук если он отключен в настройках
                if (!settings.SoundEnabled)
                {
                    toastContent.AddAudio(null, silent: true);
                }
                
                toastContent.Show();
                nativeSuccess = true;
                
                Console.WriteLine("✅ Нативное уведомление Windows успешно отправлено");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка показа нативного уведомления: {ex.Message}");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
                Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                nativeSuccess = false;
            }
            
            // Всегда показываем fallback окно для надежности
            // (нативные уведомления могут быть заблокированы Windows)
            if (!nativeSuccess)
            {
                try
                {
                    Console.WriteLine("📢 Fallback: показываем собственное окно уведомления");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var notificationWindow = new Views.NotificationWindow(title, message);
                        notificationWindow.Show();
                        Console.WriteLine("✅ Собственное окно уведомления показано");
                    });
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"❌ Ошибка fallback уведомления: {ex2.Message}");
                    Console.WriteLine($"   Type: {ex2.GetType().Name}");
                    Console.WriteLine($"   StackTrace: {ex2.StackTrace}");
                }
            }
        }
        else
        {
            Console.WriteLine("🔕 Баннеры отключены в настройках");
        }
    }

    private void PlayNotificationSound()
    {
        try
        {
            // Воспроизводим системный звук
            SystemSounds.Beep.Play();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка воспроизведения звука: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            ToastNotificationManagerCompat.Uninstall();
        }
        catch { }
    }
}
