using System;
using System.Windows;
using ChatApp.Client.Services;

namespace ChatApp.Client.Views;

public partial class SettingsWindow : Window
{
    private readonly NotificationSettings _settings;

    public SettingsWindow()
    {
        InitializeComponent();
        _settings = NotificationSettings.Instance;
        Console.WriteLine($"⚙️ SettingsWindow открыто, текущие настройки: Sound={_settings.SoundEnabled}, Banner={_settings.BannerEnabled}, Smart={_settings.SmartNotificationsEnabled}");
        LoadSettings();
    }

    private void LoadSettings()
    {
        SoundNotificationsCheckBox.IsChecked = _settings.SoundEnabled;
        BannerNotificationsCheckBox.IsChecked = _settings.BannerEnabled;
        SmartNotificationsCheckBox.IsChecked = _settings.SmartNotificationsEnabled;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        Console.WriteLine($"💾 Сохранение настроек:");
        Console.WriteLine($"   Sound: {_settings.SoundEnabled} → {SoundNotificationsCheckBox.IsChecked ?? true}");
        Console.WriteLine($"   Banner: {_settings.BannerEnabled} → {BannerNotificationsCheckBox.IsChecked ?? true}");
        Console.WriteLine($"   Smart: {_settings.SmartNotificationsEnabled} → {SmartNotificationsCheckBox.IsChecked ?? true}");
        
        _settings.SoundEnabled = SoundNotificationsCheckBox.IsChecked ?? true;
        _settings.BannerEnabled = BannerNotificationsCheckBox.IsChecked ?? true;
        _settings.SmartNotificationsEnabled = SmartNotificationsCheckBox.IsChecked ?? true;
        
        _settings.Save();
        
        Console.WriteLine($"✅ Настройки сохранены в файл");
        
        MessageBox.Show("Настройки сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
