using System.Collections.Generic;

namespace Battleship
{
    public class Ship
    {
        public int Size { get; set; }
        public List<(int x, int y)> Coordinates { get; set; }
        public int HitCount { get; set; }

        public Ship(int size)
        {
            Size = size;
            Coordinates = new List<(int x, int y)>();
            HitCount = 0;
        }

        public bool IsSunk()
        {
            return HitCount >= Size;
        }
    }
}
