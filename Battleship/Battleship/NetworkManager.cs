using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Battleship
{
    public class NetworkManager
    {
        private TcpListener? listener;
        private TcpClient? client;
        private NetworkStream? stream;
        private CancellationTokenSource? cancellationTokenSource;
        public bool IsConnected => client?.Connected ?? false;

        public event Action<string>? MessageReceived;
        public event Action? ConnectionLost;
        public event Action<string>? LogMessage;

        public async Task<bool> StartServer(int port)
        {
            try
            {
                LogMessage?.Invoke($"Запуск сервера на порту {port}...");
                
                // Закрываем предыдущий listener если есть
                listener?.Stop();
                
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                LogMessage?.Invoke($"Сервер запущен на всех интерфейсах (0.0.0.0:{port})");
                LogMessage?.Invoke("Ожидание подключения клиента...");
                
                cancellationTokenSource = new CancellationTokenSource();
                client = await listener.AcceptTcpClientAsync();
                
                LogMessage?.Invoke($"Клиент подключился: {client.Client.RemoteEndPoint}");
                stream = client.GetStream();
                StartListening();
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Ошибка сервера: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ConnectToServer(string ip, int port, int timeoutSeconds = 10)
        {
            try
            {
                LogMessage?.Invoke($"Попытка подключения к {ip}:{port}...");
                
                // Закрываем предыдущее соединение если есть
                client?.Close();
                
                client = new TcpClient();
                cancellationTokenSource = new CancellationTokenSource();
                
                LogMessage?.Invoke("Установка соединения...");
                var connectTask = client.ConnectAsync(ip, port);
                var timeoutTask = Task.Delay(timeoutSeconds * 1000, cancellationTokenSource.Token);
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    LogMessage?.Invoke($"Превышено время ожидания ({timeoutSeconds} сек)");
                    client?.Close();
                    return false;
                }
                
                await connectTask;
                
                if (client.Connected)
                {
                    LogMessage?.Invoke("TCP соединение установлено!");
                    stream = client.GetStream();
                    LogMessage?.Invoke("Начало прослушивания сообщений...");
                    StartListening();
                    return true;
                }
                
                LogMessage?.Invoke("Не удалось установить соединение");
                return false;
            }
            catch (SocketException ex)
            {
                LogMessage?.Invoke($"Ошибка сокета: {ex.Message}");
                LogMessage?.Invoke($"Код ошибки: {ex.SocketErrorCode}");
                if (ex.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    LogMessage?.Invoke("Сервер отклонил подключение. Убедитесь, что сервер запущен.");
                }
                else if (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    LogMessage?.Invoke("Время ожидания истекло. Проверьте сетевое подключение.");
                }
                client?.Close();
                return false;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Ошибка подключения: {ex.Message}");
                client?.Close();
                return false;
            }
        }

        private async void StartListening()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (client != null && stream != null && client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        MessageReceived?.Invoke(message);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch { }
            finally
            {
                ConnectionLost?.Invoke();
            }
        }

        public async Task SendMessage(string message)
        {
            if (stream != null && client?.Connected == true)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
            }
        }

        public void Disconnect()
        {
            cancellationTokenSource?.Cancel();
            stream?.Close();
            client?.Close();
            listener?.Stop();
        }

        public static string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "127.0.0.1";
        }

        public static string GetAllIPAddresses()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ips = new System.Collections.Generic.List<string>();
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ips.Add(ip.ToString());
                    }
                }
                return string.Join(", ", ips);
            }
            catch { }
            return "127.0.0.1";
        }
    }
}
