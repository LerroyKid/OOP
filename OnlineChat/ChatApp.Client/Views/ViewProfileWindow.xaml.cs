using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChatApp.Shared.Models;

namespace ChatApp.Client.Views;

public partial class ViewProfileWindow : Window
{
    private readonly HttpClient _httpClient;
    private readonly string _userId;

    public ViewProfileWindow(HttpClient httpClient, string userId, string username)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _userId = userId;
        Title = $"Профиль: {username}";
        LoadProfileAsync();
    }

    private async void LoadProfileAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/user/{_userId}");
            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<UserInfo>();
                if (user != null)
                {
                    UsernameTextBox.Text = user.Username;
                    EmailTextBox.Text = user.Email;
                    BioTextBox.Text = user.Bio ?? "Информация не указана";
                    UserIdText.Text = $"ID: {user.Id}";
                    
                    // Установка статуса
                    var (statusColor, statusText) = user.Status switch
                    {
                        UserStatus.Online => (Color.FromRgb(34, 197, 94), "Онлайн"),
                        UserStatus.DoNotDisturb => (Color.FromRgb(239, 68, 68), "Не беспокоить"),
                        _ => (Color.FromRgb(156, 163, 175), "Офлайн")
                    };
                    
                    StatusIndicator.Fill = new SolidColorBrush(statusColor);
                    StatusTextBlock.Text = statusText;

                    // Загрузка аватара если есть
                    if (!string.IsNullOrEmpty(user.AvatarUrl))
                    {
                        await LoadAvatarAsync(user.AvatarUrl);
                    }
                }
            }
            else
            {
                MessageBox.Show("Не удалось загрузить профиль пользователя", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки профиля: {ex.Message}", "Ошибка", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private async System.Threading.Tasks.Task LoadAvatarAsync(string avatarUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync(avatarUrl);
            if (response.IsSuccessStatusCode)
            {
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                var bitmap = new BitmapImage();
                using (var stream = new MemoryStream(imageBytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                }
                AvatarImage.Source = bitmap;
                AvatarPlaceholder.Visibility = Visibility.Collapsed;
            }
        }
        catch { }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
