using Assets.Scripts;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates and places cliff prefabs around the edges of a procedural island.
/// </summary>
public class PrefabCliffsGen : MonoBehaviour
{
    [Header("Cliff Prefabs")]
    [SerializeField] private GameObject cliffStraightPrefab;
    [SerializeField] private GameObject cliffInsideCornerPrefab;
    [SerializeField] private GameObject cliffOutsideCornerPrefab;

    [Header("Cliff Settings")]
    [SerializeField] private float cliffSlope = 0.7f;
    [SerializeField] private float cliffHeight = 5f;
    [SerializeField] private float cliffOffset = 0.05f;

    /// <summary>
    /// Entry point used by TerrainGen to generate the cliff prefabs
    /// around the island.
    /// </summary>
    public void CreateCliffs(
        IslandTerrainData terrainData)
    {
        if (terrainData == null)
        {
            Debug.LogWarning("PrefabCliffsGen: Terrain data is null.");
            return;
        }

        if (terrainData.islandRef == null)
        {
            Debug.LogWarning("PrefabCliffsGen: Island reference is null.");
            return;
        }

        // Find all potential cliff edges around the island
        List<CliffEdge> cliffEdges = FindCliffEdges(terrainData);

        if (cliffEdges.Count == 0)
        {
            Debug.Log("PrefabCliffsGen: No cliff edges found.");
            return;
        }

        // Connect individual edges into continuous cliff sections
        List<CliffChain> cliffChains = ConnectCliffEdges(cliffEdges);

        // Create the cliff prefabs from the connected sections
        SpawnCliffPieces(terrainData, cliffChains);
    }

    /// <summary>
    /// Finds terrain edges where land meets water and determines
    /// which of those edges qualify as cliffs.
    /// </summary>
    private List<CliffEdge> FindCliffEdges(
        IslandTerrainData terrainData)
    {
        List<CliffEdge> cliffEdges =
            new List<CliffEdge>();

        int width = terrainData.width;
        int depth = terrainData.depth;

        /*
         * Calculate the size of one terrain grid step in world space.
         *
         * The terrain data is stored as a rectangular grid, so the
         * horizontal and vertical grid spacing can be calculated from
         * the total world dimensions.
         */
        float gridSizeX =
            terrainData.worldWidth /
            (width - 1);

        float gridSizeZ =
            terrainData.worldDepth /
            (depth - 1);

        /*
         * Check horizontal grid edges.
         *
         * These edges connect:
         *
         * (x, z) -> (x + 1, z)
         *
         * If one point is land and the other is water, this is part
         * of the island boundary.
         */
        for (int x = 0; x < width - 1; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float maskA =
                    terrainData.GetIslandMask(
                        x,
                        z
                    );

                float maskB =
                    terrainData.GetIslandMask(
                        x + 1,
                        z
                    );

                bool landA =
                    maskA > 0f;

                bool landB =
                    maskB > 0f;

                // Ignore edges where both points are the same type.
                if (landA == landB)
                {
                    continue;
                }

                Vector3 pointA =
                    GetWorldPosition(
                        terrainData,
                        x,
                        z
                    );

                Vector3 pointB =
                    GetWorldPosition(
                        terrainData,
                        x + 1,
                        z
                    );

                float heightA =
                    terrainData.GetHeight(
                        x,
                        z
                    ) * terrainData.maxHeight;

                float heightB =
                    terrainData.GetHeight(
                        x + 1,
                        z
                    ) * terrainData.maxHeight;

                CreateCliffEdge(
                    cliffEdges,
                    pointA,
                    pointB,
                    heightA,
                    heightB,
                    landA,
                    gridSizeX
                );
            }
        }

        /*
         * Check vertical grid edges.
         *
         * These edges connect:
         *
         * (x, z) -> (x, z + 1)
         *
         * Again, a boundary exists when one point is land and the
         * other point is water.
         */
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth - 1; z++)
            {
                float maskA =
                    terrainData.GetIslandMask(
                        x,
                        z
                    );

                float maskB =
                    terrainData.GetIslandMask(
                        x,
                        z + 1
                    );

                bool landA =
                    maskA > 0f;

                bool landB =
                    maskB > 0f;

                // Ignore edges where both points are the same type.
                if (landA == landB)
                {
                    continue;
                }

                Vector3 pointA =
                    GetWorldPosition(
                        terrainData,
                        x,
                        z
                    );

                Vector3 pointB =
                    GetWorldPosition(
                        terrainData,
                        x,
                        z + 1
                    );

                float heightA =
                    terrainData.GetHeight(
                        x,
                        z
                    ) * terrainData.maxHeight;

                float heightB =
                    terrainData.GetHeight(
                        x,
                        z + 1
                    ) * terrainData.maxHeight;

                CreateCliffEdge(
                    cliffEdges,
                    pointA,
                    pointB,
                    heightA,
                    heightB,
                    landA,
                    gridSizeZ
                );
            }
        }

        return cliffEdges;
    }

    /// <summary>
    /// Creates a cliff edge if the boundary between the two terrain
    /// points is tall and steep enough to qualify as a cliff.
    /// </summary>
    private void CreateCliffEdge(
        List<CliffEdge> cliffEdges,
        Vector3 pointA,
        Vector3 pointB,
        float heightA,
        float heightB,
        bool pointALand,
        float horizontalDistance)
    {
        Vector3 landPoint;
        Vector3 waterPoint;

        float landHeight;
        float waterHeight;

        /*
         * Determine which side of the boundary is land and which
         * side is water.
         */
        if (pointALand)
        {
            landPoint = pointA;
            waterPoint = pointB;

            landHeight = heightA;
            waterHeight = heightB;
        }
        else
        {
            landPoint = pointB;
            waterPoint = pointA;

            landHeight = heightB;
            waterHeight = heightA;
        }

        /*
         * Calculate the vertical distance from the land surface
         * down to the terrain on the water side.
         */
        float heightDifference =
            landHeight - waterHeight;

        /*
         * If the water-side terrain happens to be higher than the
         * land-side terrain, this cannot be treated as a downward
         * cliff.
         */
        if (heightDifference < cliffHeight)
        {
            return;
        }

        /*
         * Calculate the steepness of the drop.
         *
         * This is rise / run rather than the normal-based slope
         * value used elsewhere in TerrainGen.
         */
        float slope =
            heightDifference /
            horizontalDistance;

        if (slope < cliffSlope)
        {
            return;
        }

        /*
         * The midpoint represents where the cliff prefab will
         * eventually be positioned.
         */
        Vector3 midpoint =
            (landPoint + waterPoint) * 0.5f;

        /*
         * The direction from land toward water gives us the
         * outward-facing direction of the cliff.
         *
         * This is more reliable than trying to derive the
         * coastline direction from a terrain normal.
         */
        Vector3 outward =
            waterPoint - landPoint;

        outward.y = 0f;

        if (outward.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        outward.Normalize();

        /*
         * The cliff itself runs perpendicular to the outward
         * direction.
         */
        Vector3 tangent =
            new Vector3(
                -outward.z,
                0f,
                outward.x
            );

        CliffEdge edge =
            new CliffEdge
            {
                start = landPoint,
                end = waterPoint,
                midpoint = midpoint,
                tangent = tangent,
                outward = outward,
                height = heightDifference,
                slope = slope
            };

        cliffEdges.Add(edge);
    }

    /// <summary>
    /// Converts a terrain grid coordinate into island-local world space.
    /// </summary>
    private Vector3 GetWorldPosition(
        IslandTerrainData terrainData,
        int x,
        int z)
    {
        float normalizedX =
            x / (float)(terrainData.width - 1);

        float normalizedZ =
            z / (float)(terrainData.depth - 1);

        float worldX =
            (normalizedX - 0.5f) *
            terrainData.worldWidth;

        float worldZ =
            (normalizedZ - 0.5f) *
            terrainData.worldDepth;

        float worldY =
            terrainData.GetHeight(x, z) *
            terrainData.maxHeight;

        return new Vector3(
            worldX,
            worldY,
            worldZ
        );
    }

    /// <summary>
    /// Connects individual cliff edges into continuous
    /// sections following the shape of the island.
    /// </summary>
    private List<CliffChain> ConnectCliffEdges(
        List<CliffEdge> cliffEdges)
    {
        List<CliffChain> cliffChains =
            new List<CliffChain>();

        // Edge connection logic will go here.

        return cliffChains;
    }

    /// <summary>
    /// Places the appropriate cliff prefabs along each
    /// connected cliff section.
    /// </summary>
    private void SpawnCliffPieces(
        IslandTerrainData terrainData,
        List<CliffChain> cliffChains)
    {
        // Prefab placement logic will go here.
    }

    /// <summary>
    /// Stores information about a single cliff boundary edge.
    /// </summary>
    private class CliffEdge
    {
        public Vector3 start;
        public Vector3 end;
        public Vector3 midpoint;

        public Vector3 tangent;
        public Vector3 outward;

        public float height;
        public float slope;
    }

    /// <summary>
    /// Stores a connected series of cliff edges.
    /// </summary>
    private class CliffChain
    {
        public List<CliffEdge> edges =
            new List<CliffEdge>();
    }
}