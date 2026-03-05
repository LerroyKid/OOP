using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Battleship
{
    public partial class ShipPlacementForm : Form
    {
        private const int CellSize = 35;
        private const int BoardOffset = 50;
        
        private NetworkManager network;
        private GameBoard myBoard;
        private Button[,] boardButtons;
        private Label lblStatus = null!;
        private Label lblCurrentShip = null!;
        private Button btnRotate = null!;
        private Button btnRandom = null!;
        private Button btnReady = null!;
        private bool isHost;
        private bool isHorizontal = true;
        
        private List<int> shipsToPlace = new List<int> { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
        private int currentShipIndex = 0;
        private List<(int x, int y)> previewCells = new List<(int x, int y)>();
        private bool readySent = false;
        private bool opponentReady = false;

        public ShipPlacementForm(NetworkManager networkManager, bool host)
        {
            network = networkManager;
            isHost = host;
            myBoard = new GameBoard();
            boardButtons = new Button[GameBoard.BoardSize, GameBoard.BoardSize];

            InitializeComponent();
            UpdateShipLabel();
            
            network.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(string message)
        {
            if (this.IsDisposed || this.Disposing) return;
            
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    if (message == "READY")
                    {
                        opponentReady = true;
                        
                        if (readySent)
                        {
                            // Оба игрока готовы, запускаем игру
                            StartGame();
                        }
                        else
                        {
                            // Противник готов, ждем пока мы нажмем готов
                            lblStatus.Text = "Противник готов! Нажмите 'Готов!' для начала игры";
                        }
                    }
                });
            }
            catch { }
        }

        private void StartGame()
        {
            network.MessageReceived -= OnMessageReceived;
            var gameForm = new GameForm(network, isHost, myBoard);
            this.Hide();
            gameForm.FormClosed += (s, args) => this.Close();
            gameForm.Show();
        }

        private async void BtnReady_Click(object? sender, EventArgs e)
        {
            btnReady.Enabled = false;
            btnRandom.Enabled = false;
            btnRotate.Enabled = false;
            
            // Отключаем все кнопки на поле
            for (int i = 0; i < GameBoard.BoardSize; i++)
            {
                for (int j = 0; j < GameBoard.BoardSize; j++)
                {
                    boardButtons[i, j].Enabled = false;
                }
            }
            
            readySent = true;
            await network.SendMessage("READY");
            
            if (opponentReady)
            {
                // Противник уже готов, запускаем игру
                lblStatus.Text = "Запуск игры...";
                await Task.Delay(500);
                StartGame();
            }
            else
            {
                // Ждем противника
                lblStatus.Text = "Ожидание противника...";
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Морской бой - Расстановка кораблей";
            this.Size = new Size(BoardOffset * 2 + CellSize * GameBoard.BoardSize + 250, 
                                 BoardOffset * 2 + CellSize * GameBoard.BoardSize + 100);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            Label lblTitle = new Label
            {
                Text = "Расставьте свои корабли",
                Location = new Point(BoardOffset, 10),
                Size = new Size(300, 25),
                Font = new Font("Arial", 12, FontStyle.Bold)
            };

            for (int i = 0; i < GameBoard.BoardSize; i++)
            {
                for (int j = 0; j < GameBoard.BoardSize; j++)
                {
                    int x = i, y = j;
                    
                    boardButtons[i, j] = new Button
                    {
                        Size = new Size(CellSize, CellSize),
                        Location = new Point(BoardOffset + i * CellSize, BoardOffset + j * CellSize),
                        BackColor = Color.LightBlue,
                        Tag = new Point(x, y)
                    };
                    boardButtons[i, j].MouseEnter += Cell_MouseEnter;
                    boardButtons[i, j].MouseLeave += Cell_MouseLeave;
                    boardButtons[i, j].Click += Cell_Click;

                    this.Controls.Add(boardButtons[i, j]);
                }
            }

            int rightPanelX = BoardOffset * 2 + CellSize * GameBoard.BoardSize;

            lblCurrentShip = new Label
            {
                Location = new Point(rightPanelX, BoardOffset),
                Size = new Size(200, 60),
                Font = new Font("Arial", 10, FontStyle.Bold),
                Text = ""
            };

            btnRotate = new Button
            {
                Text = "Повернуть (R)",
                Location = new Point(rightPanelX, BoardOffset + 70),
                Size = new Size(200, 35)
            };
            btnRotate.Click += BtnRotate_Click;

            btnRandom = new Button
            {
                Text = "Случайная расстановка",
                Location = new Point(rightPanelX, BoardOffset + 115),
                Size = new Size(200, 35)
            };
            btnRandom.Click += BtnRandom_Click;

            btnReady = new Button
            {
                Text = "Готов!",
                Location = new Point(rightPanelX, BoardOffset + 160),
                Size = new Size(200, 40),
                Enabled = false,
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightGreen
            };
            btnReady.Click += BtnReady_Click;

            lblStatus = new Label
            {
                Location = new Point(rightPanelX, BoardOffset + 210),
                Size = new Size(200, 100),
                Font = new Font("Arial", 9),
                Text = "Нажмите на поле для размещения корабля"
            };

            Label lblHelp = new Label
            {
                Location = new Point(BoardOffset, BoardOffset + CellSize * GameBoard.BoardSize + 10),
                Size = new Size(600, 40),
                Text = "Наведите курсор для предпросмотра\nR - повернуть корабль",
                Font = new Font("Arial", 9)
            };

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblCurrentShip);
            this.Controls.Add(btnRotate);
            this.Controls.Add(btnRandom);
            this.Controls.Add(btnReady);
            this.Controls.Add(lblStatus);
            this.Controls.Add(lblHelp);

            this.KeyPreview = true;
            this.KeyDown += Form_KeyDown;
        }

        private void Form_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.R)
            {
                BtnRotate_Click(null, EventArgs.Empty);
            }
        }

        private void UpdateShipLabel()
        {
            if (currentShipIndex < shipsToPlace.Count)
            {
                int size = shipsToPlace[currentShipIndex];
                string shipName = size switch
                {
                    4 => "Линкор",
                    3 => "Крейсер",
                    2 => "Эсминец",
                    1 => "Катер",
                    _ => "Корабль"
                };
                lblCurrentShip.Text = $"Размещаем:\n{shipName}\n({size} клеток)\n\nОсталось: {shipsToPlace.Count - currentShipIndex}";
            }
            else
            {
                lblCurrentShip.Text = "Все корабли\nразмещены!";
                btnReady.Enabled = true;
            }
        }

        private void Cell_MouseEnter(object? sender, EventArgs e)
        {
            if (currentShipIndex >= shipsToPlace.Count) return;

            Button? btn = sender as Button;
            if (btn?.Tag is Point point)
            {
                ShowPreview(point.X, point.Y);
            }
        }

        private void Cell_MouseLeave(object? sender, EventArgs e)
        {
            ClearPreview();
        }

        private void ShowPreview(int x, int y)
        {
            ClearPreview();
            
            if (currentShipIndex >= shipsToPlace.Count) return;

            int size = shipsToPlace[currentShipIndex];
            bool canPlace = true;
            previewCells.Clear();

            for (int i = 0; i < size; i++)
            {
                int px = isHorizontal ? x + i : x;
                int py = isHorizontal ? y : y + i;

                if (px >= GameBoard.BoardSize || py >= GameBoard.BoardSize)
                {
                    canPlace = false;
                    break;
                }

                previewCells.Add((px, py));
            }

            if (canPlace)
            {
                canPlace = myBoard.CanPlaceShipPublic(x, y, size, isHorizontal);
            }

            foreach (var cell in previewCells)
            {
                if (cell.x < GameBoard.BoardSize && cell.y < GameBoard.BoardSize)
                {
                    boardButtons[cell.x, cell.y].BackColor = canPlace ? Color.LightGreen : Color.LightCoral;
                }
            }
        }

        private void ClearPreview()
        {
            foreach (var cell in previewCells)
            {
                if (cell.x < GameBoard.BoardSize && cell.y < GameBoard.BoardSize)
                {
                    if (myBoard.Grid[cell.x, cell.y] == CellState.Ship)
                    {
                        boardButtons[cell.x, cell.y].BackColor = Color.Gray;
                    }
                    else
                    {
                        boardButtons[cell.x, cell.y].BackColor = Color.LightBlue;
                    }
                }
            }
            previewCells.Clear();
        }

        private void Cell_Click(object? sender, EventArgs e)
        {
            if (currentShipIndex >= shipsToPlace.Count) return;

            Button? btn = sender as Button;
            if (btn?.Tag is Point point)
            {
                int size = shipsToPlace[currentShipIndex];
                
                if (myBoard.PlaceShip(point.X, point.Y, size, isHorizontal))
                {
                    UpdateBoard();
                    currentShipIndex++;
                    UpdateShipLabel();
                    ClearPreview();
                }
                else
                {
                    lblStatus.Text = "Невозможно разместить корабль здесь!";
                }
            }
        }

        private void BtnRotate_Click(object? sender, EventArgs e)
        {
            isHorizontal = !isHorizontal;
            btnRotate.Text = isHorizontal ? "Повернуть (R)\n→" : "Повернуть (R)\n↓";
        }

        private void BtnRandom_Click(object? sender, EventArgs e)
        {
            myBoard = new GameBoard();
            myBoard.AutoPlaceShips();
            currentShipIndex = shipsToPlace.Count;
            UpdateBoard();
            UpdateShipLabel();
        }

        private void UpdateBoard()
        {
            for (int i = 0; i < GameBoard.BoardSize; i++)
            {
                for (int j = 0; j < GameBoard.BoardSize; j++)
                {
                    if (myBoard.Grid[i, j] == CellState.Ship)
                    {
                        boardButtons[i, j].BackColor = Color.Gray;
                    }
                    else
                    {
                        boardButtons[i, j].BackColor = Color.LightBlue;
                    }
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (btnReady.Enabled || currentShipIndex < shipsToPlace.Count)
            {
                if (MessageBox.Show("Вы уверены, что хотите выйти?", "Выход", 
                    MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
            base.OnFormClosing(e);
        }
    }
}
