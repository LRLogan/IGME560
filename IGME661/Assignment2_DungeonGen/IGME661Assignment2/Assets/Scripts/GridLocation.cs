using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Represents a location on the 2D dungeon grid.
    ///
    /// The dungeon is logically 2D even though the generated
    /// objects are 3D prefabs.
    /// </summary>
    [Serializable]
    public readonly struct GridLocation : IEquatable<GridLocation>
    {
        public readonly int x;
        public readonly int y;

        public GridLocation(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Returns the location adjacent to this location
        /// in the supplied direction.
        /// </summary>
        public GridLocation GetAdjacent(Direction direction)
        {
            return direction switch
            {
                Direction.North => new GridLocation(x, y + 1),
                Direction.East => new GridLocation(x + 1, y),
                Direction.South => new GridLocation(x, y - 1),
                Direction.West => new GridLocation(x - 1, y),
                _ => this
            };
        }

        /// <summary>
        /// Converts this grid location into a world-space position.
        /// </summary>
        public Vector3 ToWorldPosition(float tileSize)
        {
            return new Vector3(
                x * tileSize,
                0f,
                y * tileSize
            );
        }

        #region Helpers and overrides
        public static Vector2Int ToVector2Int(GridLocation location)
        {
            return new Vector2Int(location.x, location.y);
        }

        public bool Equals(GridLocation other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridLocation other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y);
        }

        public static bool operator ==(GridLocation a, GridLocation b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(GridLocation a, GridLocation b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }
        #endregion
    }


    /// <summary>
    /// Directions used by the 2D dungeon grid.
    /// </summary>
    public enum Direction
    {
        North,
        East,
        South,
        West
    }

    /// <summary>
    /// Simple helpers for defining a direction
    /// </summary>
    public static class DirectionUtility
    {
        public static Direction Opposite(Direction direction)
        {
            // Simple map of a direction to a vector 2 (returns the opposite)
            return direction switch
            {
                Direction.North => Direction.South,
                Direction.East => Direction.West,
                Direction.South => Direction.North,
                Direction.West => Direction.East,
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };
        }

        public static Vector2Int ToOffset(Direction direction)
        {
            // Simple map of a direction to a vector 2
            return direction switch
            {
                Direction.North => new Vector2Int(0, 1),
                Direction.East => new Vector2Int(1, 0),
                Direction.South => new Vector2Int(0, -1),
                Direction.West => new Vector2Int(-1, 0),
                _ => Vector2Int.zero
            };
        }
    }
}
