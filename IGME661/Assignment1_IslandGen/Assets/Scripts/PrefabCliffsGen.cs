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

    [Header("Cliff Prefab Settings")]
    [SerializeField] private float cliffPieceLength = 5f;

    [SerializeField] private float cornerAngle = 25f;

    [Header("Prefab Rotation")]
    [SerializeField]
    private Vector3 rotationOffset =
        new Vector3(0f, 90f, 0f);

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

        if (cliffPieceLength <= 0f)
        {
            Debug.LogWarning(
                "PrefabCliffsGen: Cliff piece length must be greater than zero."
            );

            return;
        }

        List<CliffEdge> cliffEdges =
            FindCliffEdges(terrainData);

        if (cliffEdges.Count == 0)
        {
            Debug.Log(
                "PrefabCliffsGen: No cliff edges found."
            );

            return;
        }

        List<CliffChain> cliffChains =
            ConnectCliffEdges(cliffEdges);

        List<CliffPiece> cliffPieces =
            CreateCliffPieces(cliffChains);

        SpawnCliffPieces(
            terrainData,
            cliffPieces
        );
    }

    /// <summary>
    /// Finds the points where the island mask changes between
    /// land and water.
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

        // Check horizontal grid connections.
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

                CreateCliffEdge(
                    cliffEdges,
                    pointA,
                    pointB,
                    heightA,
                    heightB,
                    new Vector2Int(x, z),
                    new Vector2Int(x + 1, z),
                    landA,
                    gridSizeX
                );
            }
        }

        // Check vertical grid connections.
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

                CreateCliffEdge(
                    cliffEdges,
                    pointA,
                    pointB,
                    heightA,
                    heightB,
                    new Vector2Int(x, z),
                    new Vector2Int(x, z + 1),
                    landA,
                    gridSizeZ
                );
            }
        }

        return cliffEdges;
    }

    /// <summary>
    /// Creates a cliff boundary point if the terrain drop
    /// is large and steep enough.
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

        float heightDifference =
            landHeight - waterHeight;

        if (heightDifference < cliffHeight)
        {
            return;
        }

        float slope =
            heightDifference /
            horizontalDistance;

        if (slope < cliffSlope)
        {
            return;
        }

        /*
         * The coastline is approximately halfway between
         * the land and water samples.
         */
        Vector3 midpoint =
            (landPoint + waterPoint) * 0.5f;

        /*
         * Direction from land toward water.
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
         * The coastline runs perpendicular to the
         * land-to-water direction.
         */
        Vector3 tangent =
            new Vector3(
                -outward.z,
                0f,
                outward.x
            ).normalized;

        /*
         * Keep all cliff points facing consistently.
         */
        Vector3 originalDirection =
            pointB - pointA;

        originalDirection.y = 0f;

        if (Vector3.Dot(
            tangent,
            originalDirection
        ) < 0f)
        {
            tangent = -tangent;
        }

        cliffEdges.Add(
            new CliffEdge
            {
                midpoint = midpoint,
                tangent = tangent,
                outward = outward,

                height = heightDifference,
                slope = slope,

                landGrid = landGrid,
                waterGrid = waterGrid
            }
        );
    }

    /// <summary>
    /// Connects nearby cliff boundary points into ordered
    /// sections following the island coastline.
    /// </summary>
    private List<CliffChain> ConnectCliffEdges(
        List<CliffEdge> cliffEdges)
    {
        List<CliffChain> chains =
            new List<CliffChain>();

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

            int currentIndex = i;

            visited.Add(currentIndex);

            chain.edges.Add(
                cliffEdges[currentIndex]
            );

            while (true)
            {
                CliffEdge current =
                    cliffEdges[currentIndex];

                int nextIndex = -1;
                float bestScore = float.MinValue;

                for (int j = 0; j < cliffEdges.Count; j++)
                {
                    if (visited.Contains(j))
                    {
                        continue;
                    }

                    CliffEdge candidate =
                        cliffEdges[j];

                    float distance =
                        Vector3.Distance(
                            current.midpoint,
                            candidate.midpoint
                        );

                    float maximumDistance =
                        GetConnectionDistance(
                            current,
                            candidate
                        );

                    if (distance > maximumDistance)
                    {
                        continue;
                    }

                    float directionScore =
                        Mathf.Abs(
                            Vector3.Dot(
                                current.tangent,
                                candidate.tangent
                            )
                        );

                    if (directionScore < 0.5f)
                    {
                        continue;
                    }

                    float score =
                        directionScore -
                        distance * 0.01f;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        nextIndex = j;
                    }
                }

                if (nextIndex == -1)
                {
                    break;
                }

                visited.Add(nextIndex);

                chain.edges.Add(
                    cliffEdges[nextIndex]
                );

                currentIndex = nextIndex;
            }

            if (chain.edges.Count > 0)
            {
                chains.Add(chain);
            }
        }

        return chains;
    }

    /// <summary>
    /// Determines how close two boundary points must be
    /// to belong to the same coastline section.
    /// </summary>
    private float GetConnectionDistance(
        CliffEdge first,
        CliffEdge second)
    {
        float firstGridDistance =
            Vector3.Distance(
                first.midpoint,
                first.midpoint + first.tangent
            );

        float secondGridDistance =
            Vector3.Distance(
                second.midpoint,
                second.midpoint + second.tangent
            );

        return Mathf.Max(
            1f,
            firstGridDistance,
            secondGridDistance
        ) * 1.75f;
    }

    /// <summary>
    /// Converts ordered cliff chains into actual prefab pieces.
    /// </summary>
    private List<CliffPiece> CreateCliffPieces(
        List<CliffChain> cliffChains)
    {
        List<CliffPiece> pieces =
            new List<CliffPiece>();

        foreach (CliffChain chain in cliffChains)
        {
            if (chain.edges.Count == 0)
            {
                continue;
            }

            if (chain.edges.Count == 1)
            {
                pieces.Add(
                    CreateStraightPiece(
                        chain.edges[0]
                    )
                );

                continue;
            }

            CreateChainPieces(
                pieces,
                chain
            );
        }

        return pieces;
    }

    /// <summary>
    /// Places prefab pieces along one continuous cliff chain.
    /// </summary>
    private void CreateChainPieces(
        List<CliffPiece> pieces,
        CliffChain chain)
    {
        float accumulatedDistance = 0f;

        CliffEdge previous =
            chain.edges[0];

        float nextPieceDistance =
            cliffPieceLength * 0.5f;

        for (int i = 1; i < chain.edges.Count; i++)
        {
            CliffEdge current =
                chain.edges[i];

            float segmentLength =
                Vector3.Distance(
                    previous.midpoint,
                    current.midpoint
                );

            if (segmentLength <= 0.001f)
            {
                previous = current;
                continue;
            }

            Vector3 segmentDirection =
                (
                    current.midpoint -
                    previous.midpoint
                ).normalized;

            float turnAngle =
                Vector3.Angle(
                    previous.tangent,
                    current.tangent
                );

            /*
             * A sharp change in direction is treated as
             * a corner instead of another straight section.
             */
            if (turnAngle >= cornerAngle)
            {
                AddCornerPiece(
                    pieces,
                    previous,
                    current
                );

                accumulatedDistance = 0f;

                nextPieceDistance =
                    cliffPieceLength * 0.5f;
            }
            else
            {
                accumulatedDistance +=
                    segmentLength;

                while (
                    accumulatedDistance >=
                    nextPieceDistance
                )
                {
                    float overshoot =
                        accumulatedDistance -
                        nextPieceDistance;

                    float interpolation =
                        1f -
                        overshoot /
                        segmentLength;

                    interpolation =
                        Mathf.Clamp01(
                            interpolation
                        );

                    Vector3 position =
                        Vector3.Lerp(
                            previous.midpoint,
                            current.midpoint,
                            interpolation
                        );

                    Vector3 tangent =
                        Vector3.Slerp(
                            previous.tangent,
                            current.tangent,
                            interpolation
                        ).normalized;

                    Vector3 outward =
                        Vector3.Slerp(
                            previous.outward,
                            current.outward,
                            interpolation
                        ).normalized;

                    pieces.Add(
                        new CliffPiece
                        {
                            type =
                                CliffPieceType.Straight,

                            position =
                                position,

                            tangent =
                                tangent,

                            outward =
                                outward,

                            height =
                                Mathf.Lerp(
                                    previous.height,
                                    current.height,
                                    interpolation
                                )
                        }
                    );

                    nextPieceDistance +=
                        cliffPieceLength;
                }
            }

            previous = current;
        }

        /*
         * If the chain was too short to place a piece
         * at the normal interval, place one in the center.
         */
        if (pieces.Count == 0)
        {
            pieces.Add(
                CreateStraightPiece(
                    chain.edges[
                        chain.edges.Count / 2
                    ]
                )
            );
        }
    }

    /// <summary>
    /// Creates a straight cliff piece.
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
    /// Creates a corner prefab at a change in coastline direction.
    /// </summary>
    private void AddCornerPiece(
        List<CliffPiece> pieces,
        CliffEdge previous,
        CliffEdge current)
    {
        Vector3 tangent =
            (
                previous.tangent +
                current.tangent
            ).normalized;

        Vector3 outward =
            (
                previous.outward +
                current.outward
            ).normalized;

        Vector3 position =
            (
                previous.midpoint +
                current.midpoint
            ) * 0.5f;

        float turn =
            Vector3.SignedAngle(
                previous.tangent,
                current.tangent,
                Vector3.up
            );

        CliffPieceType type;

        if (turn > 0f)
        {
            type =
                CliffPieceType.OutsideCorner;
        }
        else
        {
            type =
                CliffPieceType.InsideCorner;
        }

        pieces.Add(
            new CliffPiece
            {
                type =
                    type,

                position =
                    position,

                tangent =
                    tangent,

                outward =
                    outward,

                height =
                    (
                        previous.height +
                        current.height
                    ) * 0.5f
            }
        );
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
            x /
            (float)(
                terrainData.width - 1
            );

        float normalizedZ =
            z /
            (float)(
                terrainData.depth - 1
            );

        float worldX =
            (
                normalizedX -
                0.5f
            ) *
            terrainData.worldWidth;

        float worldZ =
            (
                normalizedZ -
                0.5f
            ) *
            terrainData.worldDepth;

        float worldY =
            terrainData.GetHeight(
                x,
                z
            ) *
            terrainData.maxHeight;

        return new Vector3(
            worldX,
            worldY,
            worldZ
        );
    }

    /// <summary>
    /// Spawns the generated cliff prefabs.
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
            new GameObject(
                "cliffPrefabs"
            );

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

            instance.transform.localPosition =
                piece.position +
                piece.outward *
                cliffOffset;

            if (piece.tangent.sqrMagnitude >
                0.0001f)
            {
                instance.transform.localRotation =
                    Quaternion.LookRotation(
                        piece.tangent,
                        Vector3.up
                    ) *
                    Quaternion.Euler(
                        rotationOffset
                    );
            }
        }
    }

    /// <summary>
    /// Gets the prefab associated with a cliff piece type.
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
    /// Stores one valid point along the cliff boundary.
    /// </summary>
    private class CliffEdge
    {
        public Vector3 midpoint;
        public Vector3 tangent;
        public Vector3 outward;

        public float height;
        public float slope;

        public Vector2Int landGrid;
        public Vector2Int waterGrid;
    }

    /// <summary>
    /// Stores an ordered section of coastline.
    /// </summary>
    private class CliffChain
    {
        public List<CliffEdge> edges =
            new List<CliffEdge>();
    }

    /// <summary>
    /// Stores the information needed to spawn one prefab.
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