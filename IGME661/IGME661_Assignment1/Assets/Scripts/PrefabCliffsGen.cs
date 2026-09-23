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
            Debug.LogWarning(
                "PrefabCliffsGen: Terrain data is null."
            );

            return;
        }

        if (terrainData.islandRef == null)
        {
            Debug.LogWarning(
                "PrefabCliffsGen: Island reference is null."
            );

            return;
        }

        // Find all potential cliff edges around the island
        List<CliffEdge> cliffEdges =
            FindCliffEdges(terrainData);

        if (cliffEdges.Count == 0)
        {
            Debug.Log(
                "PrefabCliffsGen: No cliff edges found."
            );

            return;
        }

        // Connect individual edges into continuous cliff sections
        List<CliffChain> cliffChains =
            ConnectCliffEdges(cliffEdges);

        // Classify the connected edges into prefab pieces
        List<CliffPiece> cliffPieces =
            ClassifyCliffPieces(cliffChains);

        // Create the cliff prefabs
        SpawnCliffPieces(
            terrainData,
            cliffPieces
        );
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

        float gridSizeX =
            terrainData.worldWidth /
            (width - 1);

        float gridSizeZ =
            terrainData.worldDepth /
            (depth - 1);

        /*
         * Check horizontal terrain grid edges.
         *
         * These edges connect:
         *
         * (x, z) -> (x + 1, z)
         *
         * A transition between land and water represents a
         * potential boundary point.
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

                Vector2Int gridA =
                    new Vector2Int(
                        x,
                        z
                    );

                Vector2Int gridB =
                    new Vector2Int(
                        x + 1,
                        z
                    );

                CreateCliffEdge(
                    cliffEdges,
                    pointA,
                    pointB,
                    heightA,
                    heightB,
                    gridA,
                    gridB,
                    landA,
                    gridSizeX
                );
            }
        }

        /*
         * Check vertical terrain grid edges.
         *
         * These edges connect:
         *
         * (x, z) -> (x, z + 1)
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

                Vector2Int gridA =
                    new Vector2Int(
                        x,
                        z
                    );

                Vector2Int gridB =
                    new Vector2Int(
                        x,
                        z + 1
                    );

                CreateCliffEdge(
                    cliffEdges,
                    pointA,
                    pointB,
                    heightA,
                    heightB,
                    gridA,
                    gridB,
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
        Vector2Int gridA,
        Vector2Int gridB,
        bool pointALand,
        float horizontalDistance)
    {
        Vector3 landPoint;
        Vector3 waterPoint;

        float landHeight;
        float waterHeight;

        Vector2Int landGrid;
        Vector2Int waterGrid;

        if (pointALand)
        {
            landPoint = pointA;
            waterPoint = pointB;

            landHeight = heightA;
            waterHeight = heightB;

            landGrid = gridA;
            waterGrid = gridB;
        }
        else
        {
            landPoint = pointB;
            waterPoint = pointA;

            landHeight = heightB;
            waterHeight = heightA;

            landGrid = gridB;
            waterGrid = gridA;
        }

        /*
         * Calculate the vertical distance from the land surface
         * down to the terrain on the water side.
         */
        float heightDifference =
            landHeight - waterHeight;

        if (heightDifference < cliffHeight)
        {
            return;
        }

        /*
         * Calculate the steepness of the drop.
         */
        float slope =
            heightDifference /
            horizontalDistance;

        if (slope < cliffSlope)
        {
            return;
        }

        /*
         * The actual coastline boundary occurs halfway between
         * the land and water sample points.
         */
        Vector3 boundaryPoint =
            (landPoint + waterPoint) * 0.5f;

        /*
         * Determine the direction from land toward water.
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
         * The cliff runs perpendicular to the direction toward
         * the water.
         */
        Vector3 tangent =
            new Vector3(
                -outward.z,
                0f,
                outward.x
            );

        /*
         * Create a segment centered around the boundary point.
         *
         * The segment length is based on the terrain grid spacing.
         */
        float segmentLength =
            horizontalDistance;

        Vector3 halfSegment =
            tangent *
            (segmentLength * 0.5f);

        Vector3 start =
            boundaryPoint -
            halfSegment;

        Vector3 end =
            boundaryPoint +
            halfSegment;

        CliffEdge edge =
            new CliffEdge
            {
                start = start,
                end = end,
                midpoint = boundaryPoint,

                tangent = tangent,
                outward = outward,

                height = heightDifference,
                slope = slope,

                landGrid = landGrid,
                waterGrid = waterGrid
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

        if (cliffEdges.Count == 0)
        {
            return cliffChains;
        }

        /*
         * Each edge has two grid points associated with it.
         *
         * We use the land grid coordinate as the connection point.
         * Edges that share nearby terrain samples are therefore
         * treated as part of the same cliff boundary.
         */
        Dictionary<Vector2Int, List<int>> edgeLookup =
            new Dictionary<Vector2Int, List<int>>();

        for (int i = 0; i < cliffEdges.Count; i++)
        {
            CliffEdge edge =
                cliffEdges[i];

            if (!edgeLookup.ContainsKey(edge.landGrid))
            {
                edgeLookup.Add(
                    edge.landGrid,
                    new List<int>()
                );
            }

            edgeLookup[edge.landGrid].Add(i);
        }

        HashSet<int> visited =
            new HashSet<int>();

        for (int i = 0; i < cliffEdges.Count; i++)
        {
            if (visited.Contains(i))
            {
                continue;
            }

            CliffChain chain =
                new CliffChain();

            Queue<int> edgeQueue =
                new Queue<int>();

            edgeQueue.Enqueue(i);
            visited.Add(i);

            while (edgeQueue.Count > 0)
            {
                int currentIndex =
                    edgeQueue.Dequeue();

                CliffEdge currentEdge =
                    cliffEdges[currentIndex];

                chain.edges.Add(
                    currentEdge
                );

                /*
                 * Find other cliff edges connected to the same
                 * land-side grid point.
                 */
                if (!edgeLookup.TryGetValue(
                    currentEdge.landGrid,
                    out List<int> connectedEdges))
                {
                    continue;
                }

                foreach (int connectedIndex in connectedEdges)
                {
                    if (visited.Contains(
                        connectedIndex))
                    {
                        continue;
                    }

                    visited.Add(
                        connectedIndex
                    );

                    edgeQueue.Enqueue(
                        connectedIndex
                    );
                }
            }

            if (chain.edges.Count > 0)
            {
                cliffChains.Add(
                    chain
                );
            }
        }

        return cliffChains;
    }

    /// <summary>
    /// Determines which prefab type is required for each portion
    /// of the connected cliff chains.
    /// </summary>
    private List<CliffPiece> ClassifyCliffPieces(
        List<CliffChain> cliffChains)
    {
        List<CliffPiece> cliffPieces =
            new List<CliffPiece>();

        foreach (CliffChain chain in cliffChains)
        {
            if (chain.edges.Count == 0)
            {
                continue;
            }

            /*
             * A chain containing a single edge is simply a straight
             * cliff section.
             */
            if (chain.edges.Count == 1)
            {
                CliffEdge edge =
                    chain.edges[0];

                cliffPieces.Add(
                    CreateStraightPiece(
                        edge
                    )
                );

                continue;
            }

            for (int i = 0;
                 i < chain.edges.Count;
                 i++)
            {
                CliffEdge current =
                    chain.edges[i];

                /*
                 * The first and last edges of an open chain are
                 * currently treated as straight pieces.
                 */
                if (i == 0 ||
                    i == chain.edges.Count - 1)
                {
                    cliffPieces.Add(
                        CreateStraightPiece(
                            current
                        )
                    );

                    continue;
                }

                CliffEdge previous =
                    chain.edges[i - 1];

                CliffEdge next =
                    chain.edges[i + 1];

                Vector3 previousTangent =
                    previous.tangent.normalized;

                Vector3 nextTangent =
                    next.tangent.normalized;

                float turn =
                    Vector3.Cross(
                        previousTangent,
                        nextTangent
                    ).y;

                /*
                 * If the tangent does not meaningfully change,
                 * this is part of a straight section.
                 */
                if (Mathf.Abs(turn) < 0.01f)
                {
                    cliffPieces.Add(
                        CreateStraightPiece(
                            current
                        )
                    );

                    continue;
                }

                /*
                 * Compare the turn direction against the outward
                 * direction to determine which side of the island
                 * the corner occupies.
                 */
                Vector3 averageOutward =
                    (
                        previous.outward +
                        current.outward +
                        next.outward
                    ).normalized;

                float cornerDirection =
                    Vector3.Cross(
                        previousTangent,
                        nextTangent
                    ).y;

                float outwardDirection =
                    Vector3.Dot(
                        averageOutward,
                        Vector3.right
                    );

                CliffPieceType type;

                /*
                 * The actual inside/outside classification will
                 * depend on the orientation of the boundary chain.
                 *
                 * For now, use the relationship between the tangent
                 * turn and outward direction to classify the corner.
                 */
                if (cornerDirection > 0f)
                {
                    type =
                        CliffPieceType.OutsideCorner;
                }
                else
                {
                    type =
                        CliffPieceType.InsideCorner;
                }

                CliffPiece piece =
                    new CliffPiece
                    {
                        type = type,

                        position =
                            current.midpoint,

                        tangent =
                            current.tangent,

                        outward =
                            current.outward,

                        height =
                            current.height
                    };

                cliffPieces.Add(
                    piece
                );
            }
        }

        return cliffPieces;
    }

    /// <summary>
    /// Creates a straight cliff piece description.
    /// </summary>
    private CliffPiece CreateStraightPiece(
        CliffEdge edge)
    {
        return new CliffPiece
        {
            type =
                CliffPieceType.Straight,

            position =
                edge.midpoint,

            tangent =
                edge.tangent,

            outward =
                edge.outward,

            height =
                edge.height
        };
    }

    /// <summary>
    /// Creates the appropriate cliff prefab for each classified
    /// cliff piece.
    /// </summary>
    private void SpawnCliffPieces(
        IslandTerrainData terrainData,
        List<CliffPiece> cliffPieces)
    {
        if (cliffPieces.Count == 0)
        {
            return;
        }

        GameObject cliffParent =
            new GameObject("cliffPrefabs");

        cliffParent.transform.SetParent(
            terrainData.islandRef.transform,
            false
        );

        cliffParent.transform.localPosition =
            Vector3.zero;

        cliffParent.transform.localRotation =
            Quaternion.identity;

        cliffParent.transform.localScale =
            Vector3.one;

        foreach (CliffPiece piece in cliffPieces)
        {
            GameObject prefab =
                GetPrefabForPiece(
                    piece.type
                );

            if (prefab == null)
            {
                continue;
            }

            GameObject instance =
                Instantiate(
                    prefab,
                    cliffParent.transform
                );

            /*
             * Move the prefab slightly toward the water.
             *
             * This keeps the prefab from sitting directly on top
             * of the terrain surface and helps prevent z-fighting.
             */
            Vector3 position =
                piece.position +
                piece.outward *
                cliffOffset;

            instance.transform.localPosition =
                position;

            /*
             * The prefab's forward direction is assumed to represent
             * the direction along the cliff.
             */
            if (piece.tangent.sqrMagnitude >
                0.0001f)
            {
                instance.transform.localRotation =
                    Quaternion.LookRotation(
                        piece.tangent,
                        Vector3.up
                    );
            }
        }
    }

    /// <summary>
    /// Gets the prefab associated with a classified cliff piece.
    /// </summary>
    private GameObject GetPrefabForPiece(
        CliffPieceType type)
    {
        switch (type)
        {
            case CliffPieceType.Straight:
                return cliffStraightPrefab;

            case CliffPieceType.InsideCorner:
                return cliffInsideCornerPrefab;

            case CliffPieceType.OutsideCorner:
                return cliffOutsideCornerPrefab;

            default:
                return null;
        }
    }

    private enum CliffPieceType
    {
        Straight,
        InsideCorner,
        OutsideCorner
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

        public Vector2Int landGrid;
        public Vector2Int waterGrid;
    }

    /// <summary>
    /// Stores a connected series of cliff edges.
    /// </summary>
    private class CliffChain
    {
        public List<CliffEdge> edges =
            new List<CliffEdge>();
    }

    /// <summary>
    /// Stores the information required to spawn one cliff prefab.
    /// </summary>
    private class CliffPiece
    {
        public CliffPieceType type;

        public Vector3 position;
        public Vector3 tangent;
        public Vector3 outward;

        public float height;
    }
}