using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace ChatApp.Client.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly HttpClient _httpClient;

    public ChangePasswordWindow(HttpClient httpClient)
    {
        InitializeComponent();
        _httpClient = httpClient;
    }

    private async void ChangeButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CurrentPasswordBox.Password) ||
            string.IsNullOrWhiteSpace(NewPasswordBox.Password) ||
            string.IsNullOrWhiteSpace(ConfirmPasswordBox.Password))
        {
            MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
        {
            MessageBox.Show("Новые пароли не совпадают", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (NewPasswordBox.Password.Length < 6)
        {
            MessageBox.Show("Пароль должен содержать минимум 6 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var request = new
            {
                CurrentPassword = CurrentPasswordBox.Password,
                NewPassword = NewPasswordBox.Password
            };

            var response = await _httpClient.PostAsJsonAsync("/api/auth/change-password", request);
            
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("Пароль успешно изменен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Ошибка: {error}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
