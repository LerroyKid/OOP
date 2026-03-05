using System;
using System.Collections.Generic;
using System.Linq;

namespace Battleship
{
    public enum CellState
    {
        Empty,
        Ship,
        Miss,
        Hit
    }

    public class GameBoard
    {
        public const int BoardSize = 10;
        public CellState[,] Grid { get; private set; }
        public List<Ship> Ships { get; private set; }

        public GameBoard()
        {
            Grid = new CellState[BoardSize, BoardSize];
            Ships = new List<Ship>();
        }

        public bool PlaceShip(int x, int y, int size, bool horizontal)
        {
            if (!CanPlaceShip(x, y, size, horizontal))
                return false;

            var ship = new Ship(size);
            for (int i = 0; i < size; i++)
            {
                int px = horizontal ? x + i : x;
                int py = horizontal ? y : y + i;
                Grid[px, py] = CellState.Ship;
                ship.Coordinates.Add((px, py));
            }
            Ships.Add(ship);
            return true;
        }

        private bool CanPlaceShip(int x, int y, int size, bool horizontal)
        {
            for (int i = 0; i < size; i++)
            {
                int px = horizontal ? x + i : x;
                int py = horizontal ? y : y + i;

                if (px >= BoardSize || py >= BoardSize)
                    return false;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int checkX = px + dx;
                        int checkY = py + dy;
                        if (checkX >= 0 && checkX < BoardSize && checkY >= 0 && checkY < BoardSize)
                        {
                            if (Grid[checkX, checkY] == CellState.Ship)
                                return false;
                        }
                    }
                }
            }
            return true;
        }

        public bool CanPlaceShipPublic(int x, int y, int size, bool horizontal)
        {
            return CanPlaceShip(x, y, size, horizontal);
        }

        public bool ProcessShot(int x, int y, out bool shipSunk)
        {
            shipSunk = false;
            if (Grid[x, y] == CellState.Ship)
            {
                Grid[x, y] = CellState.Hit;
                var ship = Ships.FirstOrDefault(s => s.Coordinates.Contains((x, y)));
                if (ship != null)
                {
                    ship.HitCount++;
                    if (ship.IsSunk())
                    {
                        shipSunk = true;
                    }
                }
                return true;
            }
            else if (Grid[x, y] == CellState.Empty)
            {
                Grid[x, y] = CellState.Miss;
                return false;
            }
            return false;
        }

        public bool AllShipsSunk()
        {
            return Ships.All(s => s.IsSunk());
        }

        public void AutoPlaceShips()
        {
            Random rand = new Random();
            int[] shipSizes = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

            foreach (int size in shipSizes)
            {
                bool placed = false;
                int attempts = 0;
                while (!placed && attempts < 1000)
                {
                    int x = rand.Next(BoardSize);
                    int y = rand.Next(BoardSize);
                    bool horizontal = rand.Next(2) == 0;
                    placed = PlaceShip(x, y, size, horizontal);
                    attempts++;
                }
            }
        }
    }
}
