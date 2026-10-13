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

        [Header("Footprint Collision")]

        [SerializeField]
        private bool preventFootprintOverlaps = true;

        [SerializeField]
        [Min(0f)]
        private float footprintOverlapTolerance = 0.05f;

        [SerializeField]
        private string footprintObjectName = "Footprint";

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
            string lastFailure = string.Empty;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                ClearDungeon();

                seed = firstSeed + attempt;

                WaveFunctionCollapse wfc =
                    new WaveFunctionCollapse(
                        mapTiles,
                        seed,
                        enforceClosedBorders
                    );

                bool wfcSucceeded = wfc.Generate(
                    width,
                    height,
                    out generatedGrid
                );

                if (!wfcSucceeded)
                {
                    lastFailure = "WFC constraints could not be solved.";
                    continue;
                }

                success = InstantiateDungeon(out lastFailure);

                if (success)
                    break;
            }

            if (!success)
            {
                ClearDungeon();

                Debug.LogError(
                    $"Dungeon generation failed after {attempts} " +
                    $"attempts. First seed: {firstSeed}. " +
                    $"Last failure: {lastFailure}"
                );

                return;
            }

            Debug.Log(
                $"Dungeon generated successfully. Seed: {seed}"
            );
        }


        private bool InstantiateDungeon(out string failureReason)
        {
            failureReason = string.Empty;

            if (dungeonParent == null)
                dungeonParent = transform;

            Dictionary<string, int> tileCounts = new();
            Dictionary<GridLocation, GameObject> placedTiles = new();
            List<Collider> placedFootprints = new();
            HashSet<GridLocation> blockedLocations = new();
            HashSet<GridLocation> queuedLocations = new();
            Queue<GridLocation> placementQueue = new();
            int skippedOverlaps = 0;

            GridLocation startLocation = GetStartLocation();

            if (!TryPlaceTile(
                    startLocation,
                    placedTiles,
                    placedFootprints,
                    tileCounts,
                    out failureReason,
                    allowDisconnectedPlacement: true))
            {
                return false;
            }

            EnqueueOpenNeighbors(
                startLocation,
                placementQueue,
                queuedLocations,
                placedTiles,
                blockedLocations
            );

            while (placementQueue.Count > 0)
            {
                GridLocation location = placementQueue.Dequeue();

                if (placedTiles.ContainsKey(location) ||
                    blockedLocations.Contains(location))
                {
                    continue;
                }

                if (!TryPlaceTile(
                        location,
                        placedTiles,
                        placedFootprints,
                        tileCounts,
                        out failureReason,
                        allowDisconnectedPlacement: false))
                {
                    if (string.IsNullOrEmpty(failureReason))
                    {
                        blockedLocations.Add(location);
                        skippedOverlaps++;
                        continue;
                    }

                    return false;
                }

                EnqueueOpenNeighbors(
                    location,
                    placementQueue,
                    queuedLocations,
                    placedTiles,
                    blockedLocations
                );
            }

            foreach (var entry in tileCounts)
            {
                Debug.Log($"Tile '{entry.Key}': {entry.Value} instances.");
            }

            if (skippedOverlaps > 0)
            {
                Debug.Log(
                    $"Stopped {skippedOverlaps} branch(es) because their " +
                    "footprints would overlap existing dungeon geometry."
                );
            }

            return true;
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


        private GridLocation GetStartLocation()
        {
            GridLocation center =
                new GridLocation(width / 2, height / 2);

            if (generatedGrid.IsInside(center) &&
                GetCollapsedTileIndex(center) >= 0)
            {
                return center;
            }

            foreach (GridLocation location in generatedGrid.GetLocations())
            {
                if (GetCollapsedTileIndex(location) >= 0)
                    return location;
            }

            return new GridLocation(0, 0);
        }


        private bool TryPlaceTile(
            GridLocation location,
            Dictionary<GridLocation, GameObject> placedTiles,
            List<Collider> placedFootprints,
            Dictionary<string, int> tileCounts,
            out string failureReason,
            bool allowDisconnectedPlacement)
        {
            failureReason = string.Empty;

            int tileIndex = GetCollapsedTileIndex(location);

            if (tileIndex < 0)
            {
                failureReason = $"Cell {location} has no tile.";
                return false;
            }

            MapTilesData.TileData tile = mapTiles.GetTile(tileIndex);
            GameObject instance = CreateTileInstance(location, tile);

            List<Direction> anchorDirections =
                GetPlacedOpenNeighborDirections(location, placedTiles);

            if (!allowDisconnectedPlacement &&
                anchorDirections.Count == 0)
            {
                DestroyGeneratedObject(instance);
                return false;
            }

            if (allowDisconnectedPlacement)
            {
                if (TryAcceptTilePlacement(
                        location,
                        instance,
                        tile,
                        placedTiles,
                        placedFootprints,
                        tileCounts))
                {
                    return true;
                }

                DestroyGeneratedObject(instance);
                return false;
            }

            if (!alignOpenConnections)
            {
                if (TryAcceptTilePlacement(
                        location,
                        instance,
                        tile,
                        placedTiles,
                        placedFootprints,
                        tileCounts))
                {
                    return true;
                }

                DestroyGeneratedObject(instance);
                return false;
            }

            foreach (Direction direction in anchorDirections)
            {
                ResetTileTransform(location, tile, instance);

                if (!TryAlignToNeighbor(
                        location,
                        direction,
                        instance,
                        placedTiles))
                {
                    continue;
                }

                if (TryAcceptTilePlacement(
                        location,
                        instance,
                        tile,
                        placedTiles,
                        placedFootprints,
                        tileCounts))
                {
                    return true;
                }
            }

            DestroyGeneratedObject(instance);
            return false;
        }


        private GameObject CreateTileInstance(
            GridLocation location,
            MapTilesData.TileData tile)
        {
            return Instantiate(
                tile.prefab,
                GetInitialPosition(location, tile),
                GetInitialRotation(tile),
                dungeonParent
            );
        }


        private void ResetTileTransform(
            GridLocation location,
            MapTilesData.TileData tile,
            GameObject instance)
        {
            instance.transform.SetPositionAndRotation(
                GetInitialPosition(location, tile),
                GetInitialRotation(tile)
            );
        }


        private Vector3 GetInitialPosition(
            GridLocation location,
            MapTilesData.TileData tile)
        {
            return location.ToWorldPosition(tileSize) + tile.prefabOffset;
        }


        private Quaternion GetInitialRotation(MapTilesData.TileData tile)
        {
            return Quaternion.Euler(tile.prefabRotation);
        }


        private bool TryAcceptTilePlacement(
            GridLocation location,
            GameObject instance,
            MapTilesData.TileData tile,
            Dictionary<GridLocation, GameObject> placedTiles,
            List<Collider> placedFootprints,
            Dictionary<string, int> tileCounts)
        {
            if (preventFootprintOverlaps &&
                HasBlockingFootprintOverlap(
                    location,
                    instance,
                    placedFootprints,
                    out _))
            {
                return false;
            }

            AddFootprints(instance, placedFootprints);
            placedTiles[location] = instance;

            if (!tileCounts.ContainsKey(tile.id))
                tileCounts[tile.id] = 0;

            tileCounts[tile.id]++;

            if (alignOpenConnections)
            {
                WarnAboutMisalignedConnections(
                    location,
                    instance,
                    placedTiles
                );
            }

            return true;
        }


        private List<Direction> GetPlacedOpenNeighborDirections(
            GridLocation location,
            Dictionary<GridLocation, GameObject> placedTiles)
        {
            List<Direction> directions = new();

            foreach (Direction direction in GetPlacementDirections())
            {
                GridLocation neighborLocation =
                    location.GetAdjacent(direction);

                if (!placedTiles.ContainsKey(neighborLocation))
                    continue;

                if (!HasOpenConnection(location, direction))
                    continue;

                directions.Add(direction);
            }

            return directions;
        }


        private void EnqueueOpenNeighbors(
            GridLocation location,
            Queue<GridLocation> placementQueue,
            HashSet<GridLocation> queuedLocations,
            Dictionary<GridLocation, GameObject> placedTiles,
            HashSet<GridLocation> blockedLocations)
        {
            foreach (Direction direction in GetPlacementDirections())
            {
                if (!HasOpenConnection(location, direction))
                    continue;

                GridLocation neighborLocation =
                    location.GetAdjacent(direction);

                if (!generatedGrid.IsInside(neighborLocation) ||
                    placedTiles.ContainsKey(neighborLocation) ||
                    blockedLocations.Contains(neighborLocation))
                {
                    continue;
                }

                if (queuedLocations.Add(neighborLocation))
                    placementQueue.Enqueue(neighborLocation);
            }
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


        private bool HasBlockingFootprintOverlap(
            GridLocation location,
            GameObject instance,
            List<Collider> placedFootprints,
            out string failureReason)
        {
            failureReason = string.Empty;

            List<Collider> instanceFootprints =
                GetFootprintColliders(instance);

            if (instanceFootprints.Count == 0)
                return false;

            Physics.SyncTransforms();

            foreach (Collider instanceFootprint in instanceFootprints)
            {
                foreach (Collider placedFootprint in placedFootprints)
                {
                    if (instanceFootprint == null ||
                        placedFootprint == null)
                    {
                        continue;
                    }

                    if (!Physics.ComputePenetration(
                            instanceFootprint,
                            instanceFootprint.transform.position,
                            instanceFootprint.transform.rotation,
                            placedFootprint,
                            placedFootprint.transform.position,
                            placedFootprint.transform.rotation,
                            out _,
                            out float overlapDistance))
                    {
                        continue;
                    }

                    if (overlapDistance <= footprintOverlapTolerance)
                        continue;

                    failureReason =
                        $"Footprint overlap at {location}: " +
                        $"'{instance.name}' overlaps " +
                        $"'{GetPlacedTileName(placedFootprint)}' by " +
                        $"{overlapDistance:0.###} units.";

                    return true;
                }
            }

            return false;
        }


        private void AddFootprints(
            GameObject instance,
            List<Collider> placedFootprints)
        {
            foreach (Collider footprint in GetFootprintColliders(instance))
            {
                placedFootprints.Add(footprint);
            }
        }


        private List<Collider> GetFootprintColliders(
            GameObject instance)
        {
            List<Collider> footprints = new();

            foreach (Collider collider
                     in instance.GetComponentsInChildren<Collider>(true))
            {
                if (!collider.enabled)
                    continue;

                if (!IsFootprintCollider(
                        collider,
                        instance.transform))
                {
                    continue;
                }

                footprints.Add(collider);
            }

            return footprints;
        }


        private bool IsFootprintCollider(
            Collider collider,
            Transform instanceRoot)
        {
            if (string.IsNullOrWhiteSpace(footprintObjectName))
                return false;

            Transform current = collider.transform;

            while (current != null)
            {
                if (string.Equals(
                        current.name,
                        footprintObjectName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (current == instanceRoot)
                    break;

                current = current.parent;
            }

            return false;
        }


        private string GetPlacedTileName(Collider footprint)
        {
            Transform current = footprint.transform;
            Transform childOfDungeonParent = current;

            while (current != null && current != dungeonParent)
            {
                childOfDungeonParent = current;
                current = current.parent;
            }

            return childOfDungeonParent != null
                ? childOfDungeonParent.name
                : footprint.name;
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


        private void DestroyGeneratedObject(GameObject instance)
        {
            if (instance == null)
                return;

            if (Application.isPlaying)
            {
                instance.SetActive(false);

                if (instance.transform.parent == dungeonParent)
                    instance.transform.SetParent(null, false);

                Destroy(instance);
            }
            else
            {
                DestroyImmediate(instance);
            }
        }


        private void ClearDungeon()
        {
            if (dungeonParent == null)
                dungeonParent = transform;

            for (int i = dungeonParent.childCount - 1;
                 i >= 0;
                 i--)
            {
                DestroyGeneratedObject(
                    dungeonParent.GetChild(i).gameObject
                );
            }
        }
    }
}
