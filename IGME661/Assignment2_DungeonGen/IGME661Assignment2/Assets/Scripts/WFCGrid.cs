using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Runtime representation of the WFC wave
    ///
    /// Each cell contains a set of possible tile indices
    /// This represents the current wave
    /// Every cell starts with every tile as a possibility
    /// </summary>
    public class WFCGrid
    {
        public class Cell
        {
            private readonly bool[] possible;

            public int PossibilityCount { get; private set; }

            public bool IsCollapsed => PossibilityCount == 1;

            public Cell(int tileCount)
            {
                possible = new bool[tileCount];

                for (int i = 0; i < tileCount; i++)
                {
                    possible[i] = true;
                }

                PossibilityCount = tileCount;
            }

            private Cell(bool[] source)
            {
                possible = (bool[])source.Clone();

                PossibilityCount = 0;

                for (int i = 0; i < possible.Length; i++)
                {
                    if (possible[i])
                    {
                        PossibilityCount++;
                    }
                }
            }

            public void Restore(int tileIndex)
            {
                if (possible[tileIndex])
                    return;

                possible[tileIndex] = true;
                PossibilityCount++;
            }

            public bool Contains(int tileIndex)
            {
                return possible[tileIndex];
            }

            public IEnumerable<int> GetPossibleTiles()
            {
                for (int i = 0; i < possible.Length; i++)
                {
                    if (possible[i])
                    {
                        yield return i;
                    }
                }
            }

            public bool Remove(int tileIndex)
            {
                if (!possible[tileIndex])
                {
                    return false;
                }

                possible[tileIndex] = false;
                PossibilityCount--;

                return true;
            }

            public void KeepOnly(int tileIndex)
            {
                for (int i = 0; i < possible.Length; i++)
                {
                    possible[i] = i == tileIndex;
                }

                PossibilityCount = 1;
            }

            public Cell Clone()
            {
                return new Cell(possible);
            }
        }

        private readonly Cell[,] cells;

        public int Width { get; }
        public int Height { get; }

        public WFCGrid(int width, int height, int tileCount)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));

            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            if (tileCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(tileCount));

            Width = width;
            Height = height;

            cells = new Cell[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    cells[x, y] = new Cell(tileCount);
                }
            }
        }

        public Cell GetCell(GridLocation location)
        {
            return cells[location.x, location.y];
        }

        public bool IsInside(GridLocation location)
        {
            return
                location.x >= 0 &&
                location.x < Width &&
                location.y >= 0 &&
                location.y < Height;
        }

        public IEnumerable<GridLocation> GetLocations()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    yield return new GridLocation(x, y);
                }
            }
        }
    }
}
