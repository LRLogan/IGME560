using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    [Serializable]
    public class TileSocket
    {
        [Tooltip("Tile IDs that may connect to this side. Leave empty for a closed wall.")]
        public List<int> compatibleTiles = new();

        public bool IsOpen =>
            compatibleTiles != null &&
            compatibleTiles.Count > 0;

        public bool Allows(int tileId)
        {
            return compatibleTiles != null &&
                   compatibleTiles.Contains(tileId);
        }
    }

    [CreateAssetMenu(
        fileName = "MapTilesData",
        menuName = "Dungeon/WFC/Map Tiles Data"
    )]
    public class MapTilesData : ScriptableObject
    {
        [Serializable]
        public class TileData
        {
            [Tooltip("Unique identifier for this tile.")]
            public string id;

            [Tooltip("Prefab spawned when this tile is selected.")]
            public GameObject prefab;

            [Tooltip("Higher weight = more likely to be selected.")]
            [Min(0f)]
            public float weight = 1f;

            [Header("Connection constraints")]

            /*
             * Neighboring sockets must match.
             *
             * For example:
             *
             * Hallway NS:
             * North = Door
             * South = Door
             * East  = Wall
             * West  = Wall
             *
             * Small Room:
             * North = Door
             * South = Door
             * East  = Door
             * West  = Door
             */

            public TileSocket north;
            public TileSocket east;
            public TileSocket south;
            public TileSocket west;

            [Header("Placement")]
            public Vector3 prefabOffset;
            public Vector3 prefabRotation;

            // Optional: grid footprint for multi-cell tiles.
            public Vector2Int gridSize = Vector2Int.one;

            public TileSocket GetSocket(Direction direction)
            {
                return direction switch
                {
                    Direction.North => north,
                    Direction.East => east,
                    Direction.South => south,
                    Direction.West => west,
                    _ => throw new ArgumentOutOfRangeException(nameof(direction))
                };
            }

            public bool HasOpenSocket(Direction direction)
            {
                TileSocket socket = GetSocket(direction);

                return socket != null && socket.IsOpen;
            }
        }

        [SerializeField]
        private List<TileData> tiles = new();

        public IReadOnlyList<TileData> Tiles => tiles;

        public int Count => tiles.Count;

        public TileData GetTile(int index)
        {
            return tiles[index];
        }

        /// <summary>
        /// Validates the almanac before WFC begins.
        /// </summary>
        public bool Validate(out string error)
        {
            error = string.Empty;

            if (tiles == null || tiles.Count == 0)
            {
                error = "MapTilesData contains no tiles.";
                return false;
            }

            HashSet<string> ids = new();
            HashSet<int> numericIds = new();

            for (int i = 0; i < tiles.Count; i++)
            {
                TileData tile = tiles[i];

                if (tile == null)
                {
                    error = $"Tile at index {i} is null.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(tile.id))
                {
                    error = $"Tile at index {i} has no ID.";
                    return false;
                }

                if (!ids.Add(tile.id))
                {
                    error = $"Duplicate tile ID: {tile.id}";
                    return false;
                }

                if (tile.prefab == null)
                {
                    error = $"Tile '{tile.id}' has no prefab.";
                    return false;
                }

                if (tile.weight < 0f)
                {
                    error = $"Tile '{tile.id}' has a negative weight.";
                    return false;
                }

                if (!int.TryParse(tile.id, out int numericId))
                {
                    error = $"Tile ID '{tile.id}' must be a numeric string.";
                    return false;
                }

                numericIds.Add(numericId);
            }

            for (int i = 0; i < tiles.Count; i++)
            {
                TileData tile = tiles[i];

                foreach (Direction direction in Enum.GetValues(typeof(Direction)))
                {
                    TileSocket socket = tile.GetSocket(direction);

                    if (socket?.compatibleTiles == null)
                        continue;

                    foreach (int compatibleId in socket.compatibleTiles)
                    {
                        if (!numericIds.Contains(compatibleId))
                        {
                            error =
                                $"Tile '{tile.id}' has an unknown {direction} " +
                                $"compatible tile ID: {compatibleId}.";

                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public int GetTileIndexById(int id)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (int.TryParse(tiles[i].id, out int tileId)
                    && tileId == id)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
