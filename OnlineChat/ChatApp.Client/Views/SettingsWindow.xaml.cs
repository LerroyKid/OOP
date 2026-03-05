using System.Windows;

namespace ChatApp.Client.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        // Загрузка сохраненных настроек (можно использовать Properties.Settings или файл конфигурации)
        // Пока используем значения по умолчанию
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Сохранение настроек
        bool soundEnabled = SoundNotificationsCheckBox.IsChecked ?? true;
        bool bannerEnabled = BannerNotificationsCheckBox.IsChecked ?? true;
        bool smartEnabled = SmartNotificationsCheckBox.IsChecked ?? true;
        bool showOnline = ShowOnlineStatusCheckBox.IsChecked ?? true;
        bool showReadReceipts = ShowReadReceiptsCheckBox.IsChecked ?? true;

        // TODO: Сохранить настройки в Properties.Settings или файл конфигурации
        
        MessageBox.Show("Настройки сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
