using System;
using System.Windows;
using System.Windows.Threading;

namespace ChatApp.Client.Views;

public partial class NotificationWindow : Window
{
    private readonly DispatcherTimer _timer;

    public NotificationWindow(string title, string message)
    {
        InitializeComponent();
        
        TitleText.Text = title;
        MessageText.Text = message;
        
        // Позиционируем в правом нижнем углу
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 10;
        Top = workArea.Bottom - Height - 10;
        
        // Автоматически закрываем через 4 секунды
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(4)
        };
        _timer.Tick += (s, e) =>
        {
            _timer.Stop();
            Close();
        };
        _timer.Start();
        
        // Закрытие по клику
        MouseLeftButtonDown += (s, e) =>
        {
            _timer.Stop();
            Close();
        };
    }
}
