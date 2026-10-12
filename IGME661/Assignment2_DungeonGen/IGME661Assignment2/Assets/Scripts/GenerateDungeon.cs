using System;
using System.Collections.Generic;
using System.Text;

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

        [Header("Generation")]

        [SerializeField]
        private int seed = 12345;

        [SerializeField]
        private bool randomizeSeed = true;

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

            if (randomizeSeed)
            {
                seed = Random.Range(
                    int.MinValue,
                    int.MaxValue
                );
            }

            WaveFunctionCollapse wfc =
                new WaveFunctionCollapse(
                    mapTiles,
                    seed
                );

            bool success =
                wfc.Generate(
                    width,
                    height,
                    out generatedGrid
                );

            if (!success)
            {
                Debug.LogError(
                    "Dungeon generation failed."
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

            foreach (GridLocation location in generatedGrid.GetLocations())
            {
                WFCGrid.Cell cell = generatedGrid.GetCell(location);

                int tileIndex = -1;

                foreach (int index in cell.GetPossibleTiles())
                {
                    tileIndex = index;
                    break;
                }

                if (tileIndex < 0)
                {
                    Debug.LogError($"Cell {location} has no tile.");
                    continue;
                }

                MapTilesData.TileData tile = mapTiles.GetTile(tileIndex);

                Vector3 position =
                    location.ToWorldPosition(tileSize) + tile.prefabOffset;

                Quaternion rotation = Quaternion.Euler(tile.prefabRotation);

                Instantiate(
                    tile.prefab,
                    position,
                    rotation,
                    dungeonParent
                );

                if (!tileCounts.ContainsKey(tile.id))
                    tileCounts[tile.id] = 0;

                tileCounts[tile.id]++;
            }

            foreach (var entry in tileCounts)
            {
                Debug.Log($"Tile '{entry.Key}': {entry.Value} instances.");
            }
        }

        /*
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

            // Rotate the new tile so its connection faces the target.
            Quaternion rotationDelta = Quaternion.FromToRotation(
                newConnection.transform.forward,
                -targetConnection.transform.forward
            );

            instance.transform.rotation =
                rotationDelta * instance.transform.rotation;

            // Move the new tile until the two connection points coincide.
            instance.transform.position +=
                targetConnection.transform.position
                - newConnection.transform.position;

            return true;
        }

        private TileConnection GetConnection(
            GameObject instance,
            Direction direction)
        {
            foreach (TileConnection connection
                     in instance.GetComponentsInChildren<TileConnection>())
            {
                if (connection.direction == direction)
                    return connection;
            }

            return null;
        }
        */
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
