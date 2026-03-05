using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Battleship
{
    public partial class ConnectionForm : Form
    {
        private TextBox txtIP = null!;
        private TextBox txtPort = null!;
        private Button btnHost = null!;
        private Button btnConnect = null!;
        private Label lblStatus = null!;
        private TextBox txtLog = null!;
        private Label lblMyIP = null!;

        public ConnectionForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Морской бой - Подключение";
            this.Size = new System.Drawing.Size(500, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            Label lblTitle = new Label
            {
                Text = "Сетевая игра Морской бой",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(450, 30),
                Font = new System.Drawing.Font("Arial", 14, System.Drawing.FontStyle.Bold)
            };

            lblMyIP = new Label
            {
                Text = $"Ваш IP: {NetworkManager.GetLocalIPAddress()}",
                Location = new System.Drawing.Point(20, 55),
                Size = new System.Drawing.Size(450, 20),
                Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.Green
            };

            Label lblIPLabel = new Label
            {
                Text = "IP адрес:",
                Location = new System.Drawing.Point(20, 85),
                Size = new System.Drawing.Size(80, 20)
            };

            txtIP = new TextBox
            {
                Location = new System.Drawing.Point(110, 83),
                Size = new System.Drawing.Size(350, 20),
                Text = "127.0.0.1"
            };

            Label lblPortLabel = new Label
            {
                Text = "Порт:",
                Location = new System.Drawing.Point(20, 115),
                Size = new System.Drawing.Size(80, 20)
            };

            txtPort = new TextBox
            {
                Location = new System.Drawing.Point(110, 113),
                Size = new System.Drawing.Size(350, 20),
                Text = "5000"
            };

            btnHost = new Button
            {
                Text = "Создать игру (Сервер)",
                Location = new System.Drawing.Point(20, 155),
                Size = new System.Drawing.Size(210, 35)
            };
            btnHost.Click += BtnHost_Click;

            btnConnect = new Button
            {
                Text = "Подключиться",
                Location = new System.Drawing.Point(250, 155),
                Size = new System.Drawing.Size(210, 35)
            };
            btnConnect.Click += BtnConnect_Click;

            lblStatus = new Label
            {
                Text = "",
                Location = new System.Drawing.Point(20, 200),
                Size = new System.Drawing.Size(450, 20),
                ForeColor = System.Drawing.Color.Red
            };

            Label lblLogLabel = new Label
            {
                Text = "Лог подключения:",
                Location = new System.Drawing.Point(20, 230),
                Size = new System.Drawing.Size(450, 20)
            };

            txtLog = new TextBox
            {
                Location = new System.Drawing.Point(20, 255),
                Size = new System.Drawing.Size(440, 150),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = System.Drawing.Color.White
            };

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblMyIP);
            this.Controls.Add(lblIPLabel);
            this.Controls.Add(txtIP);
            this.Controls.Add(lblPortLabel);
            this.Controls.Add(txtPort);
            this.Controls.Add(btnHost);
            this.Controls.Add(btnConnect);
            this.Controls.Add(lblStatus);
            this.Controls.Add(lblLogLabel);
            this.Controls.Add(txtLog);
        }

        private void AddLog(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(AddLog), message);
            }
            else
            {
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            }
        }

        private async void BtnHost_Click(object? sender, EventArgs e)
        {
            if (!int.TryParse(txtPort.Text, out int port) || port < 1024 || port > 65535)
            {
                lblStatus.Text = "Неверный порт! Используйте порт от 1024 до 65535";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                AddLog("Ошибка: неверный порт");
                return;
            }

            btnHost.Enabled = false;
            btnConnect.Enabled = false;
            txtLog.Clear();
            lblStatus.Text = $"Ожидание подключения на порту {port}...";
            lblStatus.ForeColor = System.Drawing.Color.Blue;

            var network = new NetworkManager();
            network.LogMessage += AddLog;
            
            try
            {
                AddLog($"Создание сервера на IP: {NetworkManager.GetLocalIPAddress()}, порт: {port}");
                AddLog($"Все доступные IP адреса: {NetworkManager.GetAllIPAddresses()}");
                AddLog($"Сервер будет слушать на ВСЕХ интерфейсах (0.0.0.0:{port})");
                AddLog("");
                AddLog($"Сообщите второму игроку:");
                AddLog($"IP: {NetworkManager.GetLocalIPAddress()}");
                AddLog($"Порт: {port}");
                AddLog("");
                
                bool success = await network.StartServer(port);

                if (success)
                {
                    lblStatus.Text = "Игрок подключился!";
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                    AddLog("Успешное подключение! Переход к расстановке кораблей...");
                    
                    await Task.Delay(1000);
                    
                    var placementForm = new ShipPlacementForm(network, true);
                    this.Hide();
                    placementForm.FormClosed += (s, args) => this.Close();
                    placementForm.Show();
                }
                else
                {
                    lblStatus.Text = "Ошибка создания сервера! Проверьте, не занят ли порт.";
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                    AddLog("Не удалось создать сервер");
                    btnHost.Enabled = true;
                    btnConnect.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Ошибка: {ex.Message}";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                AddLog($"Исключение: {ex.Message}");
                btnHost.Enabled = true;
                btnConnect.Enabled = true;
            }
        }

        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (!int.TryParse(txtPort.Text, out int port) || port < 1024 || port > 65535)
            {
                lblStatus.Text = "Неверный порт! Используйте порт от 1024 до 65535";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                AddLog("Ошибка: неверный порт");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtIP.Text))
            {
                lblStatus.Text = "Введите IP адрес!";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                AddLog("Ошибка: не указан IP адрес");
                return;
            }

            btnHost.Enabled = false;
            btnConnect.Enabled = false;
            txtLog.Clear();
            lblStatus.Text = $"Подключение к {txtIP.Text}:{port}...";
            lblStatus.ForeColor = System.Drawing.Color.Blue;

            var network = new NetworkManager();
            network.LogMessage += AddLog;
            
            try
            {
                AddLog($"Попытка подключения к серверу {txtIP.Text}:{port}");
                AddLog("Убедитесь, что сервер запущен и ожидает подключения");
                AddLog("");
                AddLog("Проверка сети...");
                
                // Проверка пинга
                try
                {
                    var ping = new System.Net.NetworkInformation.Ping();
                    var reply = await ping.SendPingAsync(txtIP.Text, 2000);
                    if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                    {
                        AddLog($"✓ Пинг успешен ({reply.RoundtripTime}ms) - сеть работает");
                    }
                    else
                    {
                        AddLog($"✗ Пинг не прошел: {reply.Status}");
                        AddLog("Возможно, компьютеры в разных сетях");
                    }
                }
                catch (Exception ex)
                {
                    AddLog($"✗ Ошибка пинга: {ex.Message}");
                }
                
                AddLog("");
                AddLog("Попытка подключения (таймаут 20 сек)...");
                
                bool success = await network.ConnectToServer(txtIP.Text, port, 20);

                if (success)
                {
                    lblStatus.Text = "Подключено!";
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                    AddLog("Успешное подключение! Переход к расстановке кораблей...");
                    
                    await Task.Delay(1000);
                    
                    var placementForm = new ShipPlacementForm(network, false);
                    this.Hide();
                    placementForm.FormClosed += (s, args) => this.Close();
                    placementForm.Show();
                }
                else
                {
                    lblStatus.Text = "Не удалось подключиться!";
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                    AddLog("Подключение не установлено");
                    AddLog("");
                    AddLog("Возможные причины:");
                    AddLog("1. Сервер не запущен или не ожидает подключения");
                    AddLog("2. Неправильный IP адрес");
                    AddLog("3. Компьютеры в разных сетях");
                    btnHost.Enabled = true;
                    btnConnect.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Ошибка: {ex.Message}";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                AddLog($"Исключение: {ex.Message}");
                btnHost.Enabled = true;
                btnConnect.Enabled = true;
            }
        }
    }
}
