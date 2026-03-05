using System;
using System.Drawing;
using System.Windows.Forms;

namespace Battleship
{
    public partial class GameForm : Form
    {
        private const int CellSize = 30;
        private const int BoardOffset = 50;
        
        private NetworkManager network;
        private GameBoard myBoard;
        private GameBoard enemyBoard;
        private Button[,] myButtons;
        private Button[,] enemyButtons;
        private Label lblStatus = null!;
        private Label lblMyShips = null!;
        private Label lblEnemyShips = null!;
        private bool isMyTurn;
        private bool gameStarted;
        private bool isHost;

        public GameForm(NetworkManager networkManager, bool host, GameBoard playerBoard)
        {
            network = networkManager;
            isHost = host;
            isMyTurn = isHost;
            gameStarted = true; // Игра начинается сразу
            
            myBoard = playerBoard;
            enemyBoard = new GameBoard();
            myButtons = new Button[GameBoard.BoardSize, GameBoard.BoardSize];
            enemyButtons = new Button[GameBoard.BoardSize, GameBoard.BoardSize];

            InitializeComponent();
            
            network.MessageReceived += OnMessageReceived;
            network.ConnectionLost += OnConnectionLost;
            
            // Устанавливаем статус и доступность кнопок
            lblStatus.Text = isMyTurn ? "Ваш ход!" : "Ход противника...";
            SetEnemyBoardEnabled(isMyTurn);
        }

        private void InitializeComponent()
        {
            this.Text = "Морской бой";
            this.Size = new Size(BoardOffset * 3 + CellSize * GameBoard.BoardSize * 2 + 50, 
                                 BoardOffset * 2 + CellSize * GameBoard.BoardSize + 150);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            Label lblMyBoard = new Label
            {
                Text = "Мое поле",
                Location = new Point(BoardOffset + CellSize * GameBoard.BoardSize / 2 - 40, 10),
                Size = new Size(100, 20),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            Label lblEnemyBoard = new Label
            {
                Text = "Поле противника",
                Location = new Point(BoardOffset * 2 + CellSize * GameBoard.BoardSize + 
                                    CellSize * GameBoard.BoardSize / 2 - 70, 10),
                Size = new Size(150, 20),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            for (int i = 0; i < GameBoard.BoardSize; i++)
            {
                for (int j = 0; j < GameBoard.BoardSize; j++)
                {
                    int x = i, y = j;
                    
                    myButtons[i, j] = new Button
                    {
                        Size = new Size(CellSize, CellSize),
                        Location = new Point(BoardOffset + i * CellSize, BoardOffset + j * CellSize),
                        BackColor = Color.LightBlue,
                        Enabled = false,
                        FlatStyle = FlatStyle.Flat,
                        FlatAppearance = { BorderSize = 1 }
                    };
                    myButtons[i, j].FlatAppearance.BorderColor = Color.Gray;

                    enemyButtons[i, j] = new Button
                    {
                        Size = new Size(CellSize, CellSize),
                        Location = new Point(BoardOffset * 2 + CellSize * GameBoard.BoardSize + i * CellSize, 
                                            BoardOffset + j * CellSize),
                        BackColor = Color.LightGray,
                        Tag = new Point(x, y),
                        FlatStyle = FlatStyle.Flat,
                        FlatAppearance = { BorderSize = 1 }
                    };
                    enemyButtons[i, j].FlatAppearance.BorderColor = Color.Gray;
                    enemyButtons[i, j].Click += EnemyCell_Click;

                    this.Controls.Add(myButtons[i, j]);
                    this.Controls.Add(enemyButtons[i, j]);
                }
            }

            lblStatus = new Label
            {
                Location = new Point(BoardOffset, BoardOffset + CellSize * GameBoard.BoardSize + 20),
                Size = new Size(600, 30),
                Font = new Font("Arial", 12, FontStyle.Bold),
                Text = isHost ? "Ожидание готовности противника..." : "Подключено! Отправка готовности..."
            };

            lblMyShips = new Label
            {
                Location = new Point(BoardOffset, BoardOffset + CellSize * GameBoard.BoardSize + 50),
                Size = new Size(300, 20),
                Text = "Мои корабли: 10"
            };

            lblEnemyShips = new Label
            {
                Location = new Point(BoardOffset, BoardOffset + CellSize * GameBoard.BoardSize + 70),
                Size = new Size(300, 20),
                Text = "Корабли противника: 10"
            };

            this.Controls.Add(lblMyBoard);
            this.Controls.Add(lblEnemyBoard);
            this.Controls.Add(lblStatus);
            this.Controls.Add(lblMyShips);
            this.Controls.Add(lblEnemyShips);

            UpdateMyBoard();
        }

        private void UpdateMyBoard()
        {
            for (int i = 0; i < GameBoard.BoardSize; i++)
            {
                for (int j = 0; j < GameBoard.BoardSize; j++)
                {
                    myButtons[i, j].Text = "";
                    myButtons[i, j].Paint -= DrawCellContent;
                    
                    switch (myBoard.Grid[i, j])
                    {
                        case CellState.Ship:
                            myButtons[i, j].BackColor = Color.Gray;
                            break;
                        case CellState.Hit:
                            myButtons[i, j].BackColor = Color.Red;
                            myButtons[i, j].Paint += DrawCellContent;
                            myButtons[i, j].Tag = "X";
                            break;
                        case CellState.Miss:
                            myButtons[i, j].BackColor = Color.LightBlue;
                            myButtons[i, j].Paint += DrawCellContent;
                            myButtons[i, j].Tag = "●";
                            break;
                        case CellState.Empty:
                            myButtons[i, j].BackColor = Color.LightBlue;
                            break;
                    }
                    myButtons[i, j].Invalidate();
                }
            }
            lblMyShips.Text = $"Мои корабли: {myBoard.Ships.Count - myBoard.Ships.FindAll(s => s.IsSunk()).Count}";
        }

        private void UpdateEnemyBoard()
        {
            for (int i = 0; i < GameBoard.BoardSize; i++)
            {
                for (int j = 0; j < GameBoard.BoardSize; j++)
                {
                    enemyButtons[i, j].Text = "";
                    enemyButtons[i, j].Paint -= DrawCellContent;
                    
                    switch (enemyBoard.Grid[i, j])
                    {
                        case CellState.Hit:
                            enemyButtons[i, j].BackColor = Color.Red;
                            enemyButtons[i, j].Paint += DrawCellContent;
                            enemyButtons[i, j].Tag = "X";
                            enemyButtons[i, j].Enabled = false;
                            break;
                        case CellState.Miss:
                            enemyButtons[i, j].BackColor = Color.LightBlue;
                            enemyButtons[i, j].Paint += DrawCellContent;
                            enemyButtons[i, j].Tag = "●";
                            enemyButtons[i, j].Enabled = false;
                            break;
                        case CellState.Empty:
                            enemyButtons[i, j].BackColor = Color.LightGray;
                            break;
                    }
                    enemyButtons[i, j].Invalidate();
                }
            }
            lblEnemyShips.Text = $"Корабли противника: {10 - enemyBoard.Ships.Count}";
        }

        private void DrawCellContent(object? sender, PaintEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string symbol)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                
                using (var font = new Font("Arial", 18, FontStyle.Bold))
                {
                    var color = symbol == "X" ? Color.White : Color.DarkBlue;
                    using (var brush = new SolidBrush(color))
                    {
                        var size = e.Graphics.MeasureString(symbol, font);
                        var x = (btn.Width - size.Width) / 2;
                        var y = (btn.Height - size.Height) / 2;
                        e.Graphics.DrawString(symbol, font, brush, x, y);
                    }
                }
            }
        }

        private async void EnemyCell_Click(object? sender, EventArgs e)
        {
            if (!gameStarted || !isMyTurn) return;

            Button? btn = sender as Button;
            if (btn?.Tag is Point point)
            {
                int x = point.X;
                int y = point.Y;
                
                // Проверяем, не стреляли ли мы уже в эту клетку
                if (enemyBoard.Grid[x, y] != CellState.Empty)
                {
                    return;
                }
                
                await network.SendMessage($"SHOT:{x},{y}");
                isMyTurn = false;
                lblStatus.Text = "Ожидание результата...";
                SetEnemyBoardEnabled(false);
            }
        }

        private void OnMessageReceived(string message)
        {
            this.Invoke((MethodInvoker)delegate
            {
                ProcessMessage(message);
            });
        }

        private async void ProcessMessage(string message)
        {
            string[] parts = message.Split(':');
            
            switch (parts[0])
            {
                case "SHOT":
                    string[] coords = parts[1].Split(',');
                    int x = int.Parse(coords[0]);
                    int y = int.Parse(coords[1]);
                    bool hit = myBoard.ProcessShot(x, y, out bool sunk);
                    UpdateMyBoard();
                    
                    // Отправляем результат с координатами
                    await network.SendMessage($"RESULT:{x},{y},{hit},{sunk}");
                    
                    if (myBoard.AllShipsSunk())
                    {
                        await network.SendMessage("GAMEOVER:LOSE");
                        ShowGameOver(false);
                    }
                    else if (!hit)
                    {
                        isMyTurn = true;
                        lblStatus.Text = "Противник промахнулся! Ваш ход!";
                        SetEnemyBoardEnabled(true);
                    }
                    else
                    {
                        lblStatus.Text = sunk ? "Противник потопил ваш корабль!" : "Противник попал!";
                    }
                    break;

                case "RESULT":
                    string[] resultParts = parts[1].Split(',');
                    int shotX = int.Parse(resultParts[0]);
                    int shotY = int.Parse(resultParts[1]);
                    bool wasHit = bool.Parse(resultParts[2]);
                    bool wasSunk = bool.Parse(resultParts[3]);
                    
                    // Обновляем поле противника
                    if (wasHit)
                    {
                        enemyBoard.Grid[shotX, shotY] = CellState.Hit;
                        enemyBoard.Ships.Add(new Ship(1));
                        if (wasSunk)
                        {
                            lblStatus.Text = "Вы потопили корабль! Ваш ход!";
                        }
                        else
                        {
                            lblStatus.Text = "Попадание! Ваш ход!";
                        }
                        isMyTurn = true;
                        SetEnemyBoardEnabled(true);
                    }
                    else
                    {
                        enemyBoard.Grid[shotX, shotY] = CellState.Miss;
                        lblStatus.Text = "Промах! Ход противника...";
                    }
                    
                    UpdateEnemyBoard();
                    break;

                case "GAMEOVER":
                    ShowGameOver(parts[1] == "LOSE");
                    break;

                case "REMATCH":
                    if (MessageBox.Show("Противник предлагает переиграть. Согласны?", 
                        "Новая игра", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        await network.SendMessage("REMATCH_ACCEPT");
                        RestartGame();
                    }
                    else
                    {
                        await network.SendMessage("REMATCH_DECLINE");
                        ReturnToConnection();
                    }
                    break;

                case "REMATCH_ACCEPT":
                    RestartGame();
                    break;

                case "REMATCH_DECLINE":
                    MessageBox.Show("Противник отказался от новой игры.", "Игра окончена");
                    ReturnToConnection();
                    break;
            }
        }

        private void SetEnemyBoardEnabled(bool enabled)
        {
            for (int i = 0; i < GameBoard.BoardSize; i++)
            {
                for (int j = 0; j < GameBoard.BoardSize; j++)
                {
                    if (enemyBoard.Grid[i, j] == CellState.Empty)
                    {
                        enemyButtons[i, j].Enabled = enabled;
                    }
                }
            }
        }

        private async void ShowGameOver(bool won)
        {
            string message = won ? "Поздравляем! Вы победили!" : "Вы проиграли!";
            lblStatus.Text = message;
            SetEnemyBoardEnabled(false);

            if (won)
            {
                // Победитель ждет решения проигравшего
                MessageBox.Show($"{message}\n\nОжидание решения противника о переигровке...", "Игра окончена");
            }
            else
            {
                // Проигравший предлагает переиграть
                if (MessageBox.Show($"{message}\n\nХотите переиграть?", "Игра окончена", 
                    MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    await network.SendMessage("REMATCH");
                }
                else
                {
                    await network.SendMessage("REMATCH_DECLINE");
                    ReturnToConnection();
                }
            }
        }

        private void RestartGame()
        {
            this.Hide();
            var placementForm = new ShipPlacementForm(network, isHost);
            placementForm.FormClosed += (s, args) => this.Close();
            placementForm.Show();
        }

        private void ReturnToConnection()
        {
            network.Disconnect();
            this.Hide();
            var connectionForm = new ConnectionForm();
            connectionForm.FormClosed += (s, args) => this.Close();
            connectionForm.Show();
        }

        private void OnConnectionLost()
        {
            if (this.IsDisposed || this.Disposing) return;
            
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    MessageBox.Show("Соединение потеряно!", "Ошибка");
                    ReturnToConnection();
                });
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            network.Disconnect();
            base.OnFormClosing(e);
        }
    }
}
