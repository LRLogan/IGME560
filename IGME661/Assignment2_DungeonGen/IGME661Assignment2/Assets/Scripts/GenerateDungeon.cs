using System;
using System.Collections.Generic;

namespace Assets.Scripts
{
    using UnityEngine;

    /// <summary>
    /// Unity-facing dungeon generation controller.
    ///
    /// Responsible for:
    ///     - configuring the generation
    ///     - running WFC
    ///     - converting the result into GameObjects
    ///
    /// It does NOT contain the WFC algorithm itself.
    /// </summary>
    public class GenerateDungeon : MonoBehaviour
    {
        [Header("Dungeon Size")]

        [SerializeField]
        private int width = 20;

        [SerializeField]
        private int height = 20;

        [Header("Tile Settings")]

        [SerializeField]
        private float tileSize = 10f;

        [SerializeField]
        private MapTilesData mapTiles;

        [SerializeField]
        private bool alignOpenConnections = true;

        [SerializeField]
        private bool rotateTilesToMatchConnectionPoints = false;

        [SerializeField]
        private float connectionAlignmentWarningDistance = 0.25f;

        [Header("Generation")]

        [SerializeField]
        private int seed = 12345;

        [SerializeField]
        private bool randomizeSeed = true;

        [SerializeField]
        private int maxGenerationAttempts = 25;

        [SerializeField]
        private bool enforceClosedBorders = false;

        [Header("Generated Dungeon")]

        [SerializeField]
        private Transform dungeonParent;


        private WFCGrid generatedGrid;

        private void Start()
        {
            Generate();
        }

        /// <summary>
        /// Main entry point
        /// </summary>
        [ContextMenu("Generate Dungeon")]
        public void Generate()
        {
            ClearDungeon();

            if (mapTiles == null)
            {
                Debug.LogError(
                    "Dungeon generation failed: MapTilesData is not assigned."
                );

                return;
            }

            if (width <= 0 || height <= 0)
            {
                Debug.LogError(
                    "Dungeon generation failed: width and height must be positive."
                );

                return;
            }

            int firstSeed = randomizeSeed
                ? Random.Range(int.MinValue, int.MaxValue)
                : seed;

            int attempts =
                Mathf.Max(1, maxGenerationAttempts);

            bool success = false;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                seed = firstSeed + attempt;

                WaveFunctionCollapse wfc =
                    new WaveFunctionCollapse(
                        mapTiles,
                        seed,
                        enforceClosedBorders
                    );

                success = wfc.Generate(
                    width,
                    height,
                    out generatedGrid
                );

                if (success)
                    break;
            }

            if (!success)
            {
                Debug.LogError(
                    $"Dungeon generation failed after {attempts} " +
                    $"attempts. First seed: {firstSeed}."
                );

                return;
            }

            InstantiateDungeon();

            Debug.Log(
                $"Dungeon generated successfully. Seed: {seed}"
            );
        }


        private void InstantiateDungeon()
        {
            if (dungeonParent == null)
                dungeonParent = transform;

            Dictionary<string, int> tileCounts = new();
            Dictionary<GridLocation, GameObject> placedTiles = new();

            foreach (GridLocation location in generatedGrid.GetLocations())
            {
                int tileIndex = GetCollapsedTileIndex(location);

                if (tileIndex < 0)
                {
                    Debug.LogError($"Cell {location} has no tile.");
                    continue;
                }

                MapTilesData.TileData tile = mapTiles.GetTile(tileIndex);

                Vector3 position =
                    location.ToWorldPosition(tileSize) + tile.prefabOffset;

                Quaternion rotation = Quaternion.Euler(tile.prefabRotation);

                GameObject instance = Instantiate(
                    tile.prefab,
                    position,
                    rotation,
                    dungeonParent
                );

                if (alignOpenConnections)
                {
                    TryAlignToPlacedNeighbor(
                        location,
                        instance,
                        placedTiles
                    );

                    WarnAboutMisalignedConnections(
                        location,
                        instance,
                        placedTiles
                    );
                }

                placedTiles[location] = instance;

                if (!tileCounts.ContainsKey(tile.id))
                    tileCounts[tile.id] = 0;

                tileCounts[tile.id]++;
            }

            foreach (var entry in tileCounts)
            {
                Debug.Log($"Tile '{entry.Key}': {entry.Value} instances.");
            }
        }


        private int GetCollapsedTileIndex(GridLocation location)
        {
            WFCGrid.Cell cell = generatedGrid.GetCell(location);

            if (!cell.IsCollapsed)
                return -1;

            foreach (int index in cell.GetPossibleTiles())
                return index;

            return -1;
        }


        private bool TryAlignToPlacedNeighbor(
            GridLocation location,
            GameObject instance,
            Dictionary<GridLocation, GameObject> placedTiles)
        {
            foreach (Direction direction in GetPlacementDirections())
            {
                if (!HasOpenConnection(location, direction))
                    continue;

                if (TryAlignToNeighbor(
                        location,
                        direction,
                        instance,
                        placedTiles))
                {
                    return true;
                }
            }

            return false;
        }


        private IEnumerable<Direction> GetPlacementDirections()
        {
            yield return Direction.West;
            yield return Direction.South;
            yield return Direction.East;
            yield return Direction.North;
        }


        private bool HasOpenConnection(
            GridLocation location,
            Direction direction)
        {
            GridLocation neighborLocation =
                location.GetAdjacent(direction);

            if (!generatedGrid.IsInside(neighborLocation))
                return false;

            int tileIndex = GetCollapsedTileIndex(location);
            int neighborTileIndex =
                GetCollapsedTileIndex(neighborLocation);

            if (tileIndex < 0 || neighborTileIndex < 0)
                return false;

            MapTilesData.TileData tile =
                mapTiles.GetTile(tileIndex);

            MapTilesData.TileData neighborTile =
                mapTiles.GetTile(neighborTileIndex);

            return tile.HasOpenSocket(direction) &&
                   neighborTile.HasOpenSocket(
                       DirectionUtility.Opposite(direction)
                   );
        }


        private bool TryAlignToNeighbor(
            GridLocation location,
            Direction directionToNeighbor,
            GameObject instance,
            Dictionary<GridLocation, GameObject> placedTiles)
        {
            GridLocation neighborLocation =
                location.GetAdjacent(directionToNeighbor);

            if (!placedTiles.TryGetValue(
                    neighborLocation, out GameObject neighbor))
            {
                return false;
            }

            TileConnection targetConnection = GetConnection(
                neighbor,
                DirectionUtility.Opposite(directionToNeighbor)
            );

            TileConnection newConnection = GetConnection(
                instance,
                directionToNeighbor
            );

            if (targetConnection == null || newConnection == null)
            {
                Debug.LogWarning(
                    $"Missing connection point between {location} " +
                    $"and {neighborLocation}."
                );

                return false;
            }

            if (rotateTilesToMatchConnectionPoints)
            {
                Quaternion rotationDelta = Quaternion.FromToRotation(
                    newConnection.transform.forward,
                    -targetConnection.transform.forward
                );

                instance.transform.rotation =
                    rotationDelta * instance.transform.rotation;
            }

            // Move the new tile until the two connection points coincide.
            instance.transform.position +=
                targetConnection.transform.position
                - newConnection.transform.position;

            return true;
        }


        private void WarnAboutMisalignedConnections(
            GridLocation location,
            GameObject instance,
            Dictionary<GridLocation, GameObject> placedTiles)
        {
            foreach (Direction direction in GetPlacementDirections())
            {
                if (!HasOpenConnection(location, direction))
                    continue;

                GridLocation neighborLocation =
                    location.GetAdjacent(direction);

                if (!placedTiles.TryGetValue(
                        neighborLocation,
                        out GameObject neighbor))
                {
                    continue;
                }

                TileConnection currentConnection =
                    GetConnection(instance, direction);

                TileConnection neighborConnection =
                    GetConnection(
                        neighbor,
                        DirectionUtility.Opposite(direction)
                    );

                if (currentConnection == null ||
                    neighborConnection == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    currentConnection.transform.position,
                    neighborConnection.transform.position
                );

                if (distance <= connectionAlignmentWarningDistance)
                    continue;

                Debug.LogWarning(
                    $"Open connection between {location} and " +
                    $"{neighborLocation} is {distance:0.###} units apart."
                );
            }
        }


        private TileConnection GetConnection(
            GameObject instance,
            Direction direction)
        {
            foreach (TileConnection connection
                     in instance.GetComponentsInChildren<TileConnection>(true))
            {
                if (connection.direction == direction)
                    return connection;
            }

            return null;
        }


        private void ClearDungeon()
        {
            if (dungeonParent == null)
                dungeonParent = transform;

            for (int i = dungeonParent.childCount - 1;
                 i >= 0;
                 i--)
            {
                DestroyImmediate(
                    dungeonParent.GetChild(i).gameObject
                );
            }
        }
    }
}
