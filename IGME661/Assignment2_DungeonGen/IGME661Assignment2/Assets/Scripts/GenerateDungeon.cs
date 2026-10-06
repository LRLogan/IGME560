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

        [SerializeField]
        private bool generateOnStart = true;

        [Header("Generated Dungeon")]

        [SerializeField]
        private Transform dungeonParent;


        private WFCGrid generatedGrid;


        private void Start()
        {
            if (generateOnStart)
            {
                Generate();
            }
        }


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
            {
                dungeonParent = transform;
            }

            foreach (GridLocation location
                     in generatedGrid.GetLocations())
            {
                WFCGrid.Cell cell =
                    generatedGrid.GetCell(location);

                int tileIndex = -1;

                foreach (int index in cell.GetPossibleTiles())
                {
                    tileIndex = index;
                    break;
                }

                if (tileIndex < 0)
                {
                    Debug.LogError(
                        $"Cell {location} has no tile."
                    );

                    continue;
                }

                MapTilesData.TileData tile =
                    mapTiles.GetTile(tileIndex);

                Vector3 position =
                    location.ToWorldPosition(tileSize);

                Instantiate(
                    tile.prefab,
                    position,
                    Quaternion.identity,
                    dungeonParent
                );
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
                DestroyImmediate(
                    dungeonParent.GetChild(i).gameObject
                );
            }
        }
    }
}
