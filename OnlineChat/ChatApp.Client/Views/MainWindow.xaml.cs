using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChatApp.Client.Configuration;
using ChatApp.Client.Services;
using ChatApp.Shared.Models;
using Microsoft.Win32;

namespace ChatApp.Client.Views;

public partial class MainWindow : Window
{
    private readonly ChatService _chatService = new();
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly string _userId;
    private string? _selectedContactId;

    public MainWindow(string token, string userId)
    {
        InitializeComponent();
        _token = token;
        _userId = userId;
        
        try
        {
            var serverUrl = AppSettings.Instance.ServerUrl?.Trim() ?? "http://localhost:56188";
            _httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };
        }
        catch
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:56188") };
        }
        
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            var serverUrl = AppSettings.Instance.ServerUrl?.Trim() ?? "http://localhost:56188";
            await _chatService.ConnectAsync(serverUrl, _token);
            _chatService.MessageReceived += OnMessageReceived;
            _chatService.MessageStatusUpdated += OnMessageStatusUpdated;
            _chatService.UserStatusChanged += OnUserStatusChanged;
            _chatService.MessageSent += OnMessageSent;
            await _chatService.UpdateUserStatusAsync(_userId, UserStatus.Online);
            
            // Загрузка информации о текущем пользователе
            await LoadCurrentUserAsync();
            
            // Загрузка списка пользователей
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка инициализации:\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadCurrentUserAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/user/{_userId}");
            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<UserInfo>();
                if (user != null)
                {
                    UsernameText.Text = user.Username;
                    
                    // Загрузка аватара
                    if (!string.IsNullOrEmpty(user.AvatarUrl))
                    {
                        await LoadProfileAvatarAsync(user.AvatarUrl);
                    }
                    else
                    {
                        ProfileAvatarPlaceholder.Visibility = Visibility.Visible;
                    }
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Ошибка загрузки профиля: {response.StatusCode}\n{error}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки профиля:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task LoadProfileAvatarAsync(string avatarUrl)
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
                ProfileAvatarImage.Source = bitmap;
                ProfileAvatarPlaceholder.Visibility = Visibility.Collapsed;
            }
        }
        catch { }
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/user/all");
            if (response.IsSuccessStatusCode)
            {
                var users = await response.Content.ReadFromJsonAsync<List<UserInfo>>();
                if (users != null)
                {
                    ContactsListBox.Items.Clear();
                    
                    if (users.Count == 0)
                    {
                        var emptyItem = new ListBoxItem
                        {
                            Content = "Нет других пользователей",
                            IsEnabled = false
                        };
                        ContactsListBox.Items.Add(emptyItem);
                    }
                    else
                    {
                        foreach (var user in users)
                        {
                            var statusColor = user.Status switch
                            {
                                UserStatus.Online => System.Windows.Media.Color.FromRgb(34, 197, 94),      // Яркий зеленый (#22C55E)
                                UserStatus.DoNotDisturb => System.Windows.Media.Color.FromRgb(239, 68, 68), // Яркий красный (#EF4444)
                                _ => System.Windows.Media.Color.FromRgb(156, 163, 175)                      // Светло-серый (#9CA3AF)
                            };
                            
                            // Создаем Grid для контакта
                            var contactGrid = new Grid
                            {
                                Margin = new Thickness(5)
                            };
                            
                            contactGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) }); // Аватар
                            contactGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Имя
                            contactGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) }); // Статус
                            
                            // Аватар
                            var avatarBorder = new Border
                            {
                                Width = 35,
                                Height = 35,
                                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 204)),
                                BorderThickness = new Thickness(1),
                                CornerRadius = new CornerRadius(17.5),
                                HorizontalAlignment = HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            avatarBorder.Clip = new EllipseGeometry(new Point(17.5, 17.5), 16.5, 16.5);
                            
                            var avatarGrid = new Grid();
                            var avatarImage = new Image { Stretch = System.Windows.Media.Stretch.UniformToFill };
                            var avatarPlaceholder = new TextBlock
                            {
                                Text = "👤",
                                FontSize = 20,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Foreground = System.Windows.Media.Brushes.Gray
                            };
                            
                            avatarGrid.Children.Add(avatarImage);
                            avatarGrid.Children.Add(avatarPlaceholder);
                            avatarBorder.Child = avatarGrid;
                            Grid.SetColumn(avatarBorder, 0);
                            contactGrid.Children.Add(avatarBorder);
                            
                            // Загружаем аватар асинхронно
                            if (!string.IsNullOrEmpty(user.AvatarUrl))
                            {
                                LoadContactAvatar(avatarImage, avatarPlaceholder, user.AvatarUrl);
                            }
                            
                            // Имя пользователя
                            var nameText = new TextBlock
                            {
                                Text = user.Username,
                                VerticalAlignment = VerticalAlignment.Center,
                                Margin = new Thickness(10, 0, 5, 0),
                                FontSize = 14
                            };
                            Grid.SetColumn(nameText, 1);
                            contactGrid.Children.Add(nameText);
                            
                            // Статус - цветной кружок
                            var statusEllipse = new System.Windows.Shapes.Ellipse
                            {
                                Width = 12,
                                Height = 12,
                                Fill = new System.Windows.Media.SolidColorBrush(statusColor),
                                VerticalAlignment = VerticalAlignment.Center,
                                HorizontalAlignment = HorizontalAlignment.Right,
                                Tag = "statusIndicator" // Помечаем для поиска
                            };
                            Grid.SetColumn(statusEllipse, 2);
                            contactGrid.Children.Add(statusEllipse);
                            
                            var item = new ListBoxItem
                            {
                                Content = contactGrid,
                                Tag = user.Id,
                                Padding = new Thickness(5)
                            };
                            ContactsListBox.Items.Add(item);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void LoadContactAvatar(Image avatarImage, TextBlock placeholder, string avatarUrl)
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
                avatarImage.Source = bitmap;
                placeholder.Visibility = Visibility.Collapsed;
            }
        }
        catch { }
    }

    private void OnMessageReceived(Message message)
    {
        Dispatcher.Invoke(async () =>
        {
            Console.WriteLine($"📥 OnMessageReceived: ID={message.Id}, From={message.SenderId}, Status={message.Status}");
            
            // Показываем сообщение только если:
            // 1. Мы получатели (message.ReceiverId == _userId)
            // 2. И отправитель - это выбранный контакт (message.SenderId == _selectedContactId)
            if (message.ReceiverId == _userId && message.SenderId == _selectedContactId)
            {
                Console.WriteLine($"✓ Показываем сообщение в UI");
                
                if (message.Type == MessageType.Text)
                {
                    AddMessageToUI(message);
                }
                else
                {
                    var fileName = Path.GetFileName(message.Content);
                    AddFileMessageToUI(message, fileName);
                }
                
                // Обновляем статус на "Доставлено"
                Console.WriteLine($"📊 Отправляем статус Delivered для {message.Id}");
                await _chatService.UpdateMessageStatusAsync(message.Id, MessageStatus.Delivered);
                
                // Если диалог активен, сразу помечаем как прочитанное
                if (IsActive)
                {
                    Console.WriteLine($"📊 Отправляем статус Read для {message.Id}");
                    await _chatService.UpdateMessageStatusAsync(message.Id, MessageStatus.Read);
                }
            }
            else
            {
                Console.WriteLine($"✗ Сообщение не для текущего диалога");
            }
        });
    }

    private void OnMessageStatusUpdated(string messageId, MessageStatus status)
    {
        Dispatcher.Invoke(() =>
        {
            Console.WriteLine($"📊 OnMessageStatusUpdated: ID={messageId}, Status={status}");
            
            // Обновляем статус сообщения в UI
            // Проходим по всем сообщениям и обновляем статус
            foreach (var item in MessagesPanel.Items)
            {
                if (item is StackPanel panel && panel.Tag?.ToString() == messageId)
                {
                    Console.WriteLine($"✓ Найдено сообщение для обновления: {messageId}");
                    // Ищем TextBlock со статусом во всех дочерних элементах (включая вложенные)
                    FindAndUpdateStatus(panel, status);
                    break;
                }
            }
        });
    }

    private void FindAndUpdateStatus(StackPanel panel, MessageStatus status)
    {
        foreach (var child in panel.Children)
        {
            if (child is TextBlock statusBlock && statusBlock.Tag?.ToString() == "status")
            {
                Console.WriteLine($"✓ Обновляем галочку на: {status}");
                statusBlock.Text = status switch
                {
                    MessageStatus.Sent => "✓",
                    MessageStatus.Delivered => "✓✓",
                    MessageStatus.Read => "✓✓",
                    _ => ""
                };
                statusBlock.Foreground = status == MessageStatus.Read
                    ? System.Windows.Media.Brushes.Blue
                    : System.Windows.Media.Brushes.Gray;
                return;
            }
            else if (child is StackPanel childPanel)
            {
                // Рекурсивно ищем в дочерних панелях
                FindAndUpdateStatus(childPanel, status);
            }
        }
    }

    private void OnUserStatusChanged(string userId, UserStatus status)
    {
        Dispatcher.Invoke(() =>
        {
            Console.WriteLine($"🔄 OnUserStatusChanged: userId={userId}, status={status}");
            
            // Обновляем статус пользователя в списке контактов
            foreach (var item in ContactsListBox.Items)
            {
                if (item is ListBoxItem listItem && listItem.Tag?.ToString() == userId)
                {
                    if (listItem.Content is Grid grid)
                    {
                        // Ищем Ellipse со статусом (последний столбец)
                        foreach (var child in grid.Children)
                        {
                            if (child is System.Windows.Shapes.Ellipse ellipse && ellipse.Tag?.ToString() == "statusIndicator")
                            {
                                var statusColor = status switch
                                {
                                    UserStatus.Online => System.Windows.Media.Color.FromRgb(34, 197, 94),      // Яркий зеленый (#22C55E)
                                    UserStatus.DoNotDisturb => System.Windows.Media.Color.FromRgb(239, 68, 68), // Яркий красный (#EF4444)
                                    _ => System.Windows.Media.Color.FromRgb(156, 163, 175)                      // Светло-серый (#9CA3AF)
                                };
                                ellipse.Fill = new System.Windows.Media.SolidColorBrush(statusColor);
                                Console.WriteLine($"✓ Обновлен статус для {userId}: {status}");
                                break;
                            }
                        }
                    }
                    break;
                }
            }
        });
    }

    private void AddMessageToUI(Message message)
    {
        bool isMyMessage = message.SenderId == _userId;
        
        // Определяем иконку статуса
        string statusIcon = message.Status switch
        {
            MessageStatus.Sent => "✓",      // Отправлено
            MessageStatus.Delivered => "✓✓", // Доставлено
            MessageStatus.Read => "✓✓",      // Прочитано
            _ => ""
        };
        
        // Цвет статуса
        var statusColor = message.Status == MessageStatus.Read 
            ? System.Windows.Media.Brushes.Blue 
            : System.Windows.Media.Brushes.Gray;
        
        // Создаем панель для сообщения
        var messagePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(5),
            HorizontalAlignment = isMyMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            Tag = message.Id // Сохраняем ID сообщения
        };
        
        // Текст сообщения с обводкой
        var border = new Border
        {
            Background = isMyMessage 
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 248, 198))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)),
            BorderBrush = isMyMessage
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 230, 150))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 5, 0),
            MaxWidth = 400
        };
        
        var textBlock = new TextBlock
        {
            Text = message.Content,
            TextWrapping = TextWrapping.Wrap
        };
        
        border.Child = textBlock;
        messagePanel.Children.Add(border);
        
        // Показываем статус только для своих сообщений
        if (isMyMessage && !string.IsNullOrEmpty(statusIcon))
        {
            var statusBlock = new TextBlock
            {
                Text = statusIcon,
                Foreground = statusColor,
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 5),
                Tag = "status" // Помечаем как статус
            };
            messagePanel.Children.Add(statusBlock);
        }
        
        MessagesPanel.Items.Add(messagePanel);
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(MessageTextBox.Text) || _selectedContactId == null)
            return;

        var message = new Message
        {
            SenderId = _userId,
            ReceiverId = _selectedContactId,
            Content = MessageTextBox.Text,
            Type = MessageType.Text
        };

        MessageTextBox.Clear();
        await _chatService.SendMessageAsync(message);
        // Сообщение будет добавлено в UI через OnMessageSent когда сервер вернет его с ID
    }

    private void OnMessageSent(Message message)
    {
        Dispatcher.Invoke(() =>
        {
            Console.WriteLine($"📤 OnMessageSent: ID={message.Id}, Status={message.Status}, Type={message.Type}");
            
            // Добавляем отправленное сообщение в UI с правильным ID и статусом
            if (message.Type == MessageType.Text)
            {
                AddMessageToUI(message);
            }
            else
            {
                // Для файлов извлекаем имя из URL
                var fileName = Path.GetFileName(message.Content);
                // Убираем GUID префикс если есть (формат: guid_filename.ext)
                var parts = fileName.Split('_', 2);
                if (parts.Length > 1)
                {
                    fileName = parts[1];
                }
                AddFileMessageToUI(message, fileName);
            }
        });
    }

    private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            SendButton_Click(sender, e);
    }

    private void ContactsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ContactsListBox.SelectedItem is ListBoxItem item && item.Tag is string userId)
        {
            _selectedContactId = userId;
            
            // Извлекаем имя пользователя из Grid
            string username = "Пользователь";
            if (item.Content is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is TextBlock textBlock && Grid.GetColumn(textBlock) == 1)
                    {
                        username = textBlock.Text;
                        break;
                    }
                }
            }
            
            ChatHeaderText.Text = username;
            MessagesPanel.Items.Clear();
            
            // Загрузка аватара собеседника
            LoadContactAvatarAsync(userId);
            
            // Загрузка истории сообщений
            LoadConversationAsync(userId);
        }
    }

    private async void LoadContactAvatarAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/user/{userId}");
            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<UserInfo>();
                if (user != null && !string.IsNullOrEmpty(user.AvatarUrl))
                {
                    var avatarResponse = await _httpClient.GetAsync(user.AvatarUrl);
                    if (avatarResponse.IsSuccessStatusCode)
                    {
                        var imageBytes = await avatarResponse.Content.ReadAsByteArrayAsync();
                        var bitmap = new BitmapImage();
                        using (var stream = new MemoryStream(imageBytes))
                        {
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = stream;
                            bitmap.EndInit();
                        }
                        ContactAvatarImage.Source = bitmap;
                        ContactAvatarPlaceholder.Visibility = Visibility.Collapsed;
                        return;
                    }
                }
            }
            
            // Если аватара нет - показываем placeholder
            ContactAvatarImage.Source = null;
            ContactAvatarPlaceholder.Visibility = Visibility.Visible;
        }
        catch
        {
            // При ошибке показываем placeholder
            ContactAvatarImage.Source = null;
            ContactAvatarPlaceholder.Visibility = Visibility.Visible;
        }
    }

    private async void LoadConversationAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/message/conversation/{userId}");
            if (response.IsSuccessStatusCode)
            {
                var messages = await response.Content.ReadFromJsonAsync<List<Message>>();
                if (messages != null)
                {
                    foreach (var message in messages)
                    {
                        if (message.Type == MessageType.Text)
                        {
                            AddMessageToUI(message);
                        }
                        else
                        {
                            var fileName = Path.GetFileName(message.Content);
                            AddFileMessageToUI(message, fileName);
                        }
                        
                        // Если это входящее сообщение (мы получатели) и оно еще не прочитано
                        if (message.ReceiverId == _userId && message.Status != MessageStatus.Read)
                        {
                            Console.WriteLine($"📖 Помечаем сообщение {message.Id} как прочитанное");
                            await _chatService.UpdateMessageStatusAsync(message.Id, MessageStatus.Read);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки истории: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var status = StatusComboBox.SelectedIndex switch
        {
            0 => UserStatus.Online,
            1 => UserStatus.DoNotDisturb,
            _ => UserStatus.Offline
        };
        await _chatService.UpdateUserStatusAsync(_userId, status);
    }

    private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (SearchBox != null && SearchBox.Text == "Поиск контактов...")
            SearchBox.Text = "";
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (ContactsListBox == null || ContactsListBox.Items == null)
            return;
            
        var searchText = SearchBox.Text.ToLower();
        
        if (string.IsNullOrWhiteSpace(searchText) || searchText == "поиск контактов...")
        {
            foreach (var item in ContactsListBox.Items)
            {
                if (item is ListBoxItem listBoxItem)
                    listBoxItem.Visibility = Visibility.Visible;
            }
        }
        else
        {
            foreach (var item in ContactsListBox.Items)
            {
                if (item is ListBoxItem listBoxItem)
                {
                    var content = listBoxItem.Content?.ToString()?.ToLower() ?? "";
                    listBoxItem.Visibility = content.Contains(searchText) ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }
    }

    private async void AttachButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Выберите файл",
            Filter = "Все файлы (*.*)|*.*|Изображения (*.jpg;*.jpeg;*.png;*.gif)|*.jpg;*.jpeg;*.png;*.gif|Документы (*.pdf;*.doc;*.docx;*.txt)|*.pdf;*.doc;*.docx;*.txt",
            FilterIndex = 1
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                var filePath = openFileDialog.FileName;
                var fileName = Path.GetFileName(filePath);
                
                // Проверка размера файла (макс 10 МБ)
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > 10 * 1024 * 1024)
                {
                    MessageBox.Show("Файл слишком большой. Максимальный размер: 10 МБ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Загрузка файла на сервер
                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                content.Add(fileContent, "file", fileName);

                var response = await _httpClient.PostAsync("/api/file/upload", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<FileUploadResponse>();
                    
                    if (result != null && _selectedContactId != null)
                    {
                        // Определяем тип сообщения
                        var messageType = IsImageFile(fileName) ? MessageType.Image : MessageType.Document;
                        
                        var message = new Message
                        {
                            SenderId = _userId,
                            ReceiverId = _selectedContactId,
                            Content = result.FileUrl,
                            Type = messageType
                        };

                        await _chatService.SendMessageAsync(message);
                        // Сообщение будет добавлено в UI через OnMessageSent
                    }
                }
                else
                {
                    MessageBox.Show("Ошибка загрузки файла", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private bool IsImageFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif";
    }

    private async void AddFileMessageToUI(Message message, string originalName)
    {
        bool isMyMessage = message.SenderId == _userId;
        
        // Создаем панель для сообщения
        var messagePanel = new StackPanel
        {
            Margin = new Thickness(5),
            HorizontalAlignment = isMyMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            MaxWidth = 400,
            Tag = message.Id
        };
        
        // Сразу добавляем панель в UI чтобы сохранить порядок
        MessagesPanel.Items.Add(messagePanel);
        
        // Если это изображение - показываем превью
        if (message.Type == MessageType.Image)
        {
            // Сначала показываем placeholder
            var loadingText = new TextBlock
            {
                Text = "Загрузка изображения...",
                FontStyle = FontStyles.Italic,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(5)
            };
            messagePanel.Children.Add(loadingText);
            
            try
            {
                var response = await _httpClient.GetAsync(message.Content);
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
                    
                    // Убираем placeholder
                    messagePanel.Children.Clear();
                    
                    var border = new Border
                    {
                        Background = isMyMessage 
                            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 248, 198))
                            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)),
                        BorderBrush = isMyMessage
                            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 230, 150))
                            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200)),
                        BorderThickness = new Thickness(2),
                        Padding = new Thickness(5),
                        CornerRadius = new CornerRadius(10),
                        Cursor = Cursors.Hand
                    };
                    
                    var image = new Image
                    {
                        Source = bitmap,
                        MaxWidth = 300,
                        MaxHeight = 300,
                        Stretch = System.Windows.Media.Stretch.Uniform
                    };
                    
                    border.Child = image;
                    border.MouseLeftButtonDown += (s, e) => DownloadFile(message.Content, originalName);
                    
                    messagePanel.Children.Add(border);
                    
                    // Добавляем подпись
                    var caption = new TextBlock
                    {
                        Text = $"🖼️ {originalName}",
                        FontSize = 11,
                        Foreground = System.Windows.Media.Brushes.Gray,
                        Margin = new Thickness(5, 2, 5, 0)
                    };
                    messagePanel.Children.Add(caption);
                }
            }
            catch
            {
                // Если не удалось загрузить изображение, показываем как файл
                messagePanel.Children.Clear();
                AddFileAsLinkToPanel(messagePanel, message, originalName, isMyMessage, "🖼️");
            }
        }
        else
        {
            // Для документов показываем ссылку
            AddFileAsLinkToPanel(messagePanel, message, originalName, isMyMessage, "📄");
        }
        
        // Добавляем статус для своих сообщений
        if (isMyMessage)
        {
            var statusPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5, 2, 5, 0)
            };
            
            var statusIcon = message.Status switch
            {
                MessageStatus.Sent => "✓",
                MessageStatus.Delivered => "✓✓",
                MessageStatus.Read => "✓✓",
                _ => ""
            };
            
            var statusColor = message.Status == MessageStatus.Read 
                ? System.Windows.Media.Brushes.Blue 
                : System.Windows.Media.Brushes.Gray;
            
            var statusBlock = new TextBlock
            {
                Text = statusIcon,
                Foreground = statusColor,
                FontSize = 10,
                Tag = "status"
            };
            
            statusPanel.Children.Add(statusBlock);
            messagePanel.Children.Add(statusPanel);
        }
    }
    
    private void AddFileAsLinkToPanel(StackPanel messagePanel, Message message, string originalName, bool isMyMessage, string icon)
    {
        var border = new Border
        {
            Background = isMyMessage 
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 248, 198))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)),
            BorderBrush = isMyMessage
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 230, 150))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200)),
            BorderThickness = new Thickness(2),
            Padding = new Thickness(10, 5, 10, 5),
            CornerRadius = new CornerRadius(10),
            Cursor = Cursors.Hand
        };
        
        var textBlock = new TextBlock
        {
            Text = $"{icon} {originalName}",
            TextWrapping = TextWrapping.Wrap,
            Foreground = System.Windows.Media.Brushes.Blue
        };
        
        border.Child = textBlock;
        border.MouseLeftButtonDown += (s, e) => DownloadFile(message.Content, originalName);
        
        messagePanel.Children.Add(border);
    }

    private async void DownloadFile(string fileUrl, string fileName)
    {
        try
        {
            var response = await _httpClient.GetAsync(fileUrl);
            if (response.IsSuccessStatusCode)
            {
                var saveFileDialog = new SaveFileDialog
                {
                    FileName = fileName,
                    Filter = "Все файлы (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    File.WriteAllBytes(saveFileDialog.FileName, fileBytes);
                    MessageBox.Show("Файл сохранен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        Console.WriteLine($"🚪 Закрытие окна, устанавливаем статус Offline для {_userId}");
        await _chatService.UpdateUserStatusAsync(_userId, UserStatus.Offline);
        await _chatService.DisconnectAsync();
        base.OnClosing(e);
    }

    private async void EditProfileButton_Click(object sender, RoutedEventArgs e)
    {
        var profileWindow = new ProfileWindow(_httpClient, _userId);
        if (profileWindow.ShowDialog() == true)
        {
            // Обновить отображение профиля
            await LoadCurrentUserAsync();
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.ShowDialog();
    }

    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход", 
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                // Установить статус офлайн перед выходом
                await _chatService.UpdateUserStatusAsync(_userId, UserStatus.Offline);
                await _chatService.DisconnectAsync();
                
                // Открыть окно входа
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                
                // Закрыть текущее окно
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выходе: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
