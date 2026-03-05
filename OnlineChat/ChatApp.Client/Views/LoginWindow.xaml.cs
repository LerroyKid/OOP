using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using ChatApp.Client.Configuration;
using ChatApp.Shared.DTOs;
using Microsoft.Win32;

namespace ChatApp.Client.Views;

public partial class LoginWindow : Window
{
    private readonly HttpClient _httpClient;

    public LoginWindow()
    {
        InitializeComponent();
        
        try
        {
            var serverUrl = AppSettings.Instance.ServerUrl?.Trim() ?? "http://localhost:56188";
            _httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка инициализации:\n\nНеправильный URL в appsettings.json\n\nURL: {AppSettings.Instance.ServerUrl}\n\nОшибка: {ex.Message}\n\nИспользуется localhost по умолчанию.", 
                "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:56188") };
        }
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || string.IsNullOrWhiteSpace(PasswordBox.Password))
        {
            MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Блокируем кнопки и показываем индикатор
        LoginButton.IsEnabled = false;
        RegisterButton.IsEnabled = false;
        LoadingPanel.Visibility = Visibility.Visible;

        try
        {
            var request = new LoginRequest
            {
                Email = EmailTextBox.Text,
                Password = PasswordBox.Password
            };

            // Добавляем таймаут 10 секунд
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
            
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request, cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                
                if (result == null)
                {
                    MessageBox.Show("Ошибка: сервер вернул пустой ответ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (result.Success == true)
                {
                    if (string.IsNullOrEmpty(result.Token))
                    {
                        MessageBox.Show("Ошибка: токен не получен", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    if (string.IsNullOrEmpty(result.UserId))
                    {
                        MessageBox.Show("Ошибка: ID пользователя не получен", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    try
                    {
                        var mainWindow = new MainWindow(result.Token, result.UserId);
                        mainWindow.Show();
                        Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при открытии главного окна:\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show(result.Error ?? "Ошибка входа", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // Неуспешный ответ от сервера
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    MessageBox.Show("Неверный email или пароль.\n\nПроверьте правильность введенных данных.", 
                        "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Ошибка входа (код {response.StatusCode}).\n\nСервер: {_httpClient.BaseAddress}\n\nОтвет: {errorContent}", 
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            MessageBox.Show($"Не удается подключиться к серверу.\n\nСервер: {_httpClient.BaseAddress}\n\nОшибка: {ex.Message}\n\nУбедитесь что:\n• Сервер запущен\n• IP адрес указан правильно в appsettings.json\n• Файрвол не блокирует порт 56188", 
                "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (TaskCanceledException)
        {
            MessageBox.Show($"Превышено время ожидания ответа от сервера.\n\nСервер: {_httpClient.BaseAddress}\n\nВозможные причины:\n• Сервер не запущен\n• Неправильный IP адрес\n• Файрвол блокирует подключение\n• Сервер не отвечает", 
                "Таймаут", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Произошла неожиданная ошибка:\n\nТип: {ex.GetType().Name}\nСообщение: {ex.Message}\nСервер: {_httpClient.BaseAddress}\n\nПодробности:\n{ex.StackTrace}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Разблокируем кнопки и скрываем индикатор
            LoginButton.IsEnabled = true;
            RegisterButton.IsEnabled = true;
            LoadingPanel.Visibility = Visibility.Collapsed;
        }
    }

    private async void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || string.IsNullOrWhiteSpace(PasswordBox.Password))
        {
            MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var request = new RegisterRequest
            {
                Email = EmailTextBox.Text,
                Username = EmailTextBox.Text.Split('@')[0],
                Password = PasswordBox.Password
            };

            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result?.Success == true)
                {
                    MessageBox.Show("Регистрация успешна!\n\nТеперь войдите в систему с вашим email и паролем.", 
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Очищаем поле пароля для входа
                    PasswordBox.Clear();
                }
                else
                {
                    string errorMessage = result?.Error ?? "Ошибка регистрации";
                    
                    // Более понятное сообщение для пользователя
                    if (errorMessage.Contains("уже существует") || errorMessage.Contains("already exists"))
                    {
                        MessageBox.Show($"Пользователь с email '{EmailTextBox.Text}' уже зарегистрирован.\n\nИспользуйте другой email или войдите в существующий аккаунт.", 
                            "Email занят", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show(errorMessage, "Ошибка регистрации", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Ошибка при регистрации.\n\nПопробуйте еще раз.", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (HttpRequestException)
        {
            MessageBox.Show("Не удается подключиться к серверу.\n\nУбедитесь что сервер запущен:\n• Запустите start-server.bat\n• Или выполните: cd ChatApp.Server && dotnet run", 
                "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Произошла ошибка:\n{ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
