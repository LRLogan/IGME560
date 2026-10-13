using System;
using System.Collections.Generic;

namespace Assets.Scripts
{
    using UnityEngine;
    using UnityEngine.Serialization;

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
        [Min(0f)]
        [FormerlySerializedAs("connectionAlignmentWarningDistance")]
        private float maximumConnectionGap = 0.25f;

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
        [Min(1)]
        private int minimumConnectedTiles = 8;

        [SerializeField]
        private bool enforceClosedBorders = false;

        [Header("Generated Dungeon")]

        [SerializeField]
        private Transform dungeonParent;


        private WFCGrid generatedGrid;
        private IReadOnlyList<MapTilesData.TileData> generationTiles;

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

                generationTiles = wfc.Variants;
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
            Dictionary<GridLocation, int> placedTileIndices = new();
            List<Collider> placedFootprints = new();
            HashSet<GridLocation> blockedLocations = new();
            HashSet<GridLocation> queuedLocations = new();
            Queue<GridLocation> placementQueue = new();
            int stoppedBranches = 0;
            System.Random placementRandom = new System.Random(seed);

            GridLocation startLocation = GetStartLocation();

            if (!TryPlaceTile(
                    startLocation,
                    placedTiles,
                    placedTileIndices,
                    placedFootprints,
                    tileCounts,
                    placementRandom,
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
                placedTileIndices,
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
                        placedTileIndices,
                        placedFootprints,
                        tileCounts,
                        placementRandom,
                        out failureReason,
                        allowDisconnectedPlacement: false))
                {
                    if (string.IsNullOrEmpty(failureReason))
                    {
                        blockedLocations.Add(location);
                        stoppedBranches++;
                        continue;
                    }

                    return false;
                }

                EnqueueOpenNeighbors(
                    location,
                    placementQueue,
                    queuedLocations,
                    placedTiles,
                    placedTileIndices,
                    blockedLocations
                );
            }

            int requiredTiles = Mathf.Min(
                width * height, Mathf.Max(1, minimumConnectedTiles));

            if (placedTiles.Count < requiredTiles)
            {
                failureReason =
                    $"Only {placedTiles.Count} connected tile(s) fit; " +
                    $"at least {requiredTiles} are required.";
                return false;
            }

            Debug.Log($"Placed {placedTiles.Count} connected tiles.");

            foreach (var entry in tileCounts)
            {
                Debug.Log($"Tile '{entry.Key}': {entry.Value} instances.");
            }

            if (stoppedBranches > 0)
            {
                Debug.Log(
                    $"Stopped {stoppedBranches} branch(es) because no " +
                    "compatible tile could connect without a footprint overlap."
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

            GridLocation bestLocation = new GridLocation(0, 0);
            int bestOpenSockets = -1;
            float bestDistanceToCenter = float.MaxValue;
            bool foundStart = false;

            foreach (GridLocation location in generatedGrid.GetLocations())
            {
                int tileIndex = GetCollapsedTileIndex(location);

                if (tileIndex < 0)
                    continue;

                int openSockets = CountOpenSockets(
                    generationTiles[tileIndex]
                );

                float distanceToCenter =
                    Vector2Int.Distance(
                        GridLocation.ToVector2Int(location),
                        GridLocation.ToVector2Int(center)
                    );

                if (!foundStart ||
                    openSockets > bestOpenSockets ||
                    (openSockets == bestOpenSockets &&
                     distanceToCenter < bestDistanceToCenter))
                {
                    bestLocation = location;
                    bestOpenSockets = openSockets;
                    bestDistanceToCenter = distanceToCenter;
                    foundStart = true;
                }
            }

            return bestLocation;
        }


        private int CountOpenSockets(MapTilesData.TileData tile)
        {
            int openSockets = 0;

            foreach (Direction direction in GetPlacementDirections())
            {
                if (tile.HasOpenSocket(direction))
                    openSockets++;
            }

            return openSockets;
        }


        private bool TryPlaceTile(
            GridLocation location,
            Dictionary<GridLocation, GameObject> placedTiles,
            Dictionary<GridLocation, int> placedTileIndices,
            List<Collider> placedFootprints,
            Dictionary<string, int> tileCounts,
            System.Random placementRandom,
            out string failureReason,
            bool allowDisconnectedPlacement)
        {
            failureReason = string.Empty;

            foreach (int tileIndex in GetCandidateTileIndices(
                         location, placedTileIndices, placementRandom,
                         allowDisconnectedPlacement))
            {
                MapTilesData.TileData tile = generationTiles[tileIndex];
                GameObject instance = CreateTileInstance(location, tile);
                List<Direction> anchorDirections =
                    GetPlacedOpenNeighborDirections(
                        location, tile, placedTileIndices);

                if (allowDisconnectedPlacement)
                {
                    if (TryAcceptTilePlacement(
                            location, instance, tile, tileIndex,
                            anchorDirections, placedTiles, placedTileIndices,
                            placedFootprints, tileCounts))
                    {
                        return true;
                    }
                }
                else
                {
                    foreach (Direction direction in anchorDirections)
                    {
                        ResetTileTransform(location, tile, instance);

                        if (alignOpenConnections &&
                            !TryAlignToNeighbor(
                                location, direction, instance, placedTiles))
                        {
                            continue;
                        }

                        if (TryAcceptTilePlacement(
                                location, instance, tile, tileIndex,
                                anchorDirections, placedTiles, placedTileIndices,
                                placedFootprints, tileCounts))
                        {
                            return true;
                        }

                        if (!alignOpenConnections)
                            break;
                    }
                }

                DestroyGeneratedObject(instance);
            }

            return false;
        }


        private List<int> GetCandidateTileIndices(
            GridLocation location,
            Dictionary<GridLocation, int> placedTileIndices,
            System.Random placementRandom,
            bool allowDisconnectedPlacement)
        {
            List<int> candidates = new();

            for (int index = 0; index < generationTiles.Count; index++)
            {
                if (FitsPlacedNeighbors(
                        location, generationTiles[index], placedTileIndices,
                        allowDisconnectedPlacement))
                {
                    candidates.Add(index);
                }
            }

            List<int> ordered = new();
            int preferred = GetCollapsedTileIndex(location);

            if (candidates.Remove(preferred))
                ordered.Add(preferred);

            while (candidates.Count > 0)
            {
                float totalWeight = 0f;

                foreach (int index in candidates)
                    totalWeight += Mathf.Max(0f, generationTiles[index].weight);

                int selected = placementRandom.Next(candidates.Count);

                if (totalWeight > 0f)
                {
                    double roll = placementRandom.NextDouble() * totalWeight;

                    for (int i = 0; i < candidates.Count; i++)
                    {
                        roll -= Mathf.Max(0f,
                            generationTiles[candidates[i]].weight);

                        if (roll <= 0d)
                        {
                            selected = i;
                            break;
                        }
                    }
                }

                ordered.Add(candidates[selected]);
                candidates.RemoveAt(selected);
            }

            return ordered;
        }


        private bool FitsPlacedNeighbors(
            GridLocation location,
            MapTilesData.TileData tile,
            Dictionary<GridLocation, int> placedTileIndices,
            bool allowDisconnectedPlacement)
        {
            bool hasOpenNeighbor = false;
            int tileId = int.Parse(tile.id);

            foreach (Direction direction in GetPlacementDirections())
            {
                GridLocation neighborLocation = location.GetAdjacent(direction);

                if (!placedTileIndices.TryGetValue(
                        neighborLocation, out int neighborIndex))
                {
                    continue;
                }

                MapTilesData.TileData neighbor = generationTiles[neighborIndex];
                TileSocket socket = tile.GetSocket(direction);
                TileSocket other = neighbor.GetSocket(
                    DirectionUtility.Opposite(direction));
                bool open = socket != null && socket.IsOpen;
                bool otherOpen = other != null && other.IsOpen;

                if (open != otherOpen)
                    return false;

                if (!open)
                    continue;

                if (!socket.Allows(int.Parse(neighbor.id)) ||
                    !other.Allows(tileId))
                {
                    return false;
                }

                hasOpenNeighbor = true;
            }

            return allowDisconnectedPlacement || hasOpenNeighbor;
        }


        private GameObject CreateTileInstance(
            GridLocation location,
            MapTilesData.TileData tile)
        {
            GameObject instance = Instantiate(
                tile.prefab,
                GetInitialPosition(location, tile),
                GetInitialRotation(tile),
                dungeonParent
            );

            foreach (TileConnection connection in
                     instance.GetComponentsInChildren<TileConnection>(true))
            {
                connection.direction = (Direction)(
                    ((int)connection.direction + tile.quarterTurns) % 4);
            }

            return instance;
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
            return Quaternion.Euler(0f, tile.quarterTurns * 90f, 0f) *
                   tile.prefab.transform.rotation *
                   Quaternion.Euler(tile.prefabRotation);
        }


        private bool TryAcceptTilePlacement(
            GridLocation location,
            GameObject instance,
            MapTilesData.TileData tile,
            int tileIndex,
            List<Direction> anchorDirections,
            Dictionary<GridLocation, GameObject> placedTiles,
            Dictionary<GridLocation, int> placedTileIndices,
            List<Collider> placedFootprints,
            Dictionary<string, int> tileCounts)
        {
            if (!ConnectionsAligned(
                    location, instance, anchorDirections, placedTiles))
            {
                return false;
            }

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
            placedTileIndices[location] = tileIndex;

            if (!tileCounts.ContainsKey(tile.id))
                tileCounts[tile.id] = 0;

            tileCounts[tile.id]++;

            return true;
        }


        private bool ConnectionsAligned(
            GridLocation location,
            GameObject instance,
            List<Direction> anchorDirections,
            Dictionary<GridLocation, GameObject> placedTiles)
        {
            foreach (Direction direction in anchorDirections)
            {
                GridLocation neighborLocation = location.GetAdjacent(direction);
                TileConnection current = GetConnection(instance, direction);
                TileConnection neighbor = GetConnection(
                    placedTiles[neighborLocation],
                    DirectionUtility.Opposite(direction));

                if (current == null || neighbor == null ||
                    Vector3.Distance(
                        current.transform.position,
                        neighbor.transform.position) >
                    maximumConnectionGap)
                {
                    return false;
                }
            }

            return true;
        }


        private List<Direction> GetPlacedOpenNeighborDirections(
            GridLocation location,
            MapTilesData.TileData tile,
            Dictionary<GridLocation, int> placedTileIndices)
        {
            List<Direction> directions = new();

            foreach (Direction direction in GetPlacementDirections())
            {
                GridLocation neighborLocation =
                    location.GetAdjacent(direction);

                if (!placedTileIndices.TryGetValue(
                        neighborLocation, out int neighborIndex) ||
                    !tile.HasOpenSocket(direction) ||
                    !generationTiles[neighborIndex].HasOpenSocket(
                        DirectionUtility.Opposite(direction)))
                {
                    continue;
                }

                directions.Add(direction);
            }

            return directions;
        }


        private void EnqueueOpenNeighbors(
            GridLocation location,
            Queue<GridLocation> placementQueue,
            HashSet<GridLocation> queuedLocations,
            Dictionary<GridLocation, GameObject> placedTiles,
            Dictionary<GridLocation, int> placedTileIndices,
            HashSet<GridLocation> blockedLocations)
        {
            MapTilesData.TileData tile = generationTiles[
                placedTileIndices[location]];

            foreach (Direction direction in GetPlacementDirections())
            {
                if (!tile.HasOpenSocket(direction))
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

            // Move the new tile until the two connection points coincide.
            instance.transform.position +=
                targetConnection.transform.position
                - newConnection.transform.position;

            return true;
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
