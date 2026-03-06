using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ChatApp.Shared.Models;
using Microsoft.Win32;

namespace ChatApp.Client.Views;

public partial class ProfileWindow : Window
{
    private readonly HttpClient _httpClient;
    private readonly string _userId;
    private UserInfo? _currentUser;
    private string? _avatarFilePath;

    public ProfileWindow(HttpClient httpClient, string userId)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _userId = userId;
        LoadProfileAsync();
    }

    private async void LoadProfileAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/user/{_userId}");
            if (response.IsSuccessStatusCode)
            {
                _currentUser = await response.Content.ReadFromJsonAsync<UserInfo>();
                if (_currentUser != null)
                {
                    UsernameTextBox.Text = _currentUser.Username;
                    EmailTextBox.Text = _currentUser.Email;
                    BioTextBox.Text = _currentUser.Bio ?? "";
                    UserIdText.Text = $"ID: {_currentUser.Id}";
                    
                    StatusComboBox.SelectedIndex = _currentUser.Status switch
                    {
                        UserStatus.Online => 0,
                        UserStatus.DoNotDisturb => 1,
                        _ => 2
                    };

                    // Загрузка аватара если есть
                    if (!string.IsNullOrEmpty(_currentUser.AvatarUrl))
                    {
                        LoadAvatar(_currentUser.AvatarUrl);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки профиля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadAvatar(string avatarUrl)
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

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Валидация email
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || !EmailTextBox.Text.Contains("@"))
            {
                MessageBox.Show("Введите корректный email адрес", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string? avatarUrl = _currentUser?.AvatarUrl;

            // Загрузка нового аватара если выбран
            if (!string.IsNullOrEmpty(_avatarFilePath))
            {
                avatarUrl = await UploadAvatarAsync(_avatarFilePath);
            }

            var updateRequest = new
            {
                Username = UsernameTextBox.Text,
                Email = EmailTextBox.Text,
                Bio = BioTextBox.Text.Length > 500 ? BioTextBox.Text.Substring(0, 500) : BioTextBox.Text,
                AvatarUrl = avatarUrl
            };

            var response = await _httpClient.PutAsJsonAsync("/api/user/profile", updateRequest);
            
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("Профиль успешно обновлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Ошибка сохранения: {error}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task<string?> UploadAvatarAsync(string filePath)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            content.Add(fileContent, "file", Path.GetFileName(filePath));

            var response = await _httpClient.PostAsync("/api/file/upload", content);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<FileUploadResponse>();
                return result?.FileUrl;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки аватара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        return null;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ChangeAvatarButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Выберите изображение для аватара",
            Filter = "Изображения (*.jpg;*.jpeg;*.png;*.gif)|*.jpg;*.jpeg;*.png;*.gif",
            FilterIndex = 1
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                var filePath = openFileDialog.FileName;
                var fileInfo = new FileInfo(filePath);
                
                // Проверка размера (макс 5 МБ)
                if (fileInfo.Length > 5 * 1024 * 1024)
                {
                    MessageBox.Show("Файл слишком большой. Максимальный размер: 5 МБ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Предпросмотр
                var bitmap = new BitmapImage(new Uri(filePath));
                AvatarImage.Source = bitmap;
                AvatarPlaceholder.Visibility = Visibility.Collapsed;
                _avatarFilePath = filePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void RemoveAvatarButton_Click(object sender, RoutedEventArgs e)
    {
        AvatarImage.Source = null;
        AvatarPlaceholder.Visibility = Visibility.Visible;
        _avatarFilePath = null;
        
        if (_currentUser != null)
            _currentUser.AvatarUrl = null;
    }

    private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        var passwordWindow = new ChangePasswordWindow(_httpClient);
        passwordWindow.ShowDialog();
    }
}
