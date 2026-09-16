using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/* Current debugging plan.
 * This code has a lot of un needed stuff
 * Just get a heightmap / mesh spawned at a location or look at some more tutorials
 */

public class TerrainGen : MonoBehaviour
{
    [Header("Terrain Settings")]
    [SerializeField]
    private TerrainSettings terrainSettings;

    [SerializeField]
    private Material atlasMat;

    [Header("Generation")]
    [SerializeField]
    private int islandCount = 3;

    [SerializeField]
    private float islandSpacing = 150f;

    [SerializeField]
    private Transform islandParent;

    private NoiseAlgorithm terrainNoise;

    private readonly List<GameObject> generatedIslands = new();
    private readonly List<Vector3> islandLocations = new();

    private void Start()
    {
        InitializeGenerator();
    }

    private void InitializeGenerator()
    {
        if (terrainSettings == null)
        {
            Debug.LogError(
                "TerrainGen: No TerrainSettings assigned."
            );

            return;
        }

        terrainNoise = new NoiseAlgorithm();

        terrainNoise.InitializeNoise(
            terrainSettings.width,
            terrainSettings.depth,
            terrainSettings.voronoiRandSeed
        );

        terrainNoise.InitializePerlinNoise(
            terrainSettings.frequency,
            terrainSettings.amplitude,
            terrainSettings.octaves,
            terrainSettings.lacunarity,
            terrainSettings.gain,
            terrainSettings.scale,
            terrainSettings.normalizeBias
        );

        if (islandParent == null)
        {
            GameObject parent =
                new GameObject("Generated Islands");

            islandParent = parent.transform;
        }
    }

    /// <summary>
    /// Main entry point for full terrain generation.
    /// </summary>
    public void StartFullTerrainGen()
    {
        ClearPreviousGeneration();

        GenerateIslandLocations();

        for (int i = 0; i < islandLocations.Count; i++)
        {
            SpawnIsland(
                islandLocations[i],
                terrainSettings.voronoiRandSeed + i
            );
        }
    }

    /// <summary>
    /// Creates positions for the islands.
    /// This is intentionally simple for now.
    /// Voronoi can replace this later.
    /// </summary>
    private void GenerateIslandLocations()
    {
        islandLocations.Clear();

        Random.InitState(
            terrainSettings.voronoiRandSeed
        );

        int attempts = 0;
        int maxAttempts = islandCount * 20;

        while (
            islandLocations.Count < islandCount &&
            attempts < maxAttempts)
        {
            attempts++;

            float x =
                Random.Range(
                    -terrainSettings.worldWidth * 0.5f,
                    terrainSettings.worldWidth * 0.5f
                );

            float z =
                Random.Range(
                    -terrainSettings.worldDepth * 0.5f,
                    terrainSettings.worldDepth * 0.5f
                );

            Vector3 candidate =
                new Vector3(x, 0f, z);

            bool tooClose = false;

            foreach (Vector3 existing in islandLocations)
            {
                if (Vector3.Distance(
                        candidate,
                        existing)
                    < islandSpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                islandLocations.Add(candidate);
            }
        }

        if (islandLocations.Count < islandCount)
        {
            Debug.LogWarning(
                $"Only generated {islandLocations.Count} " +
                $"of {islandCount} requested islands."
            );
        }
    }

    /// <summary>
    /// Generates one island's heightmap and mesh.
    /// </summary>
    private void SpawnIsland(
        Vector3 location,
        int seed)
    {
        Debug.Log(
            $"Generating island at {location} " +
            $"with seed {seed}"
        );

        IslandTerrainData terrainData =
            GenerateIslandTerrain(seed);

        Mesh islandMesh =
            GenerateTerrainMesh(terrainData);

        GameObject island =
            new GameObject(
                $"Island_{generatedIslands.Count}"
            );

        island.transform.SetParent(
            islandParent,
            false
        );

        island.transform.position = location;

        MeshFilter meshFilter =
            island.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer =
            island.AddComponent<MeshRenderer>();

        meshFilter.sharedMesh = islandMesh;

        if (atlasMat != null)
        {
            meshRenderer.sharedMaterial = atlasMat;
        }

        generatedIslands.Add(island);
    }

    /// <summary>
    /// Generates an island heightmap from noise
    /// and applies a radial island mask.
    /// </summary>
    private IslandTerrainData GenerateIslandTerrain(int seed)
    {
        IslandTerrainData terrainData =
            new IslandTerrainData(
                terrainSettings.width,
                terrainSettings.depth,
                terrainSettings.worldWidth,
                terrainSettings.worldDepth,
                terrainSettings.maxHeight
            );

        using NativeArray<float> noiseMap =
            new NativeArray<float>(
                terrainSettings.width *
                terrainSettings.depth,
                Allocator.TempJob
            );

        terrainNoise.InitializeNoise(
            terrainSettings.width,
            terrainSettings.depth,
            seed
        );

        terrainNoise.setNoise(
            noiseMap,
            0,
            0
        );

        for (int x = 0;
             x < terrainSettings.width;
             x++)
        {
            for (int z = 0;
                 z < terrainSettings.depth;
                 z++)
            {
                int index =
                    x * terrainSettings.depth + z;

                float noise =
                    noiseMap[index];

                float islandMask =
                    CalculateIslandMask(x, z);

                float height =
                    noise * islandMask;

                Debug.Log(x);
                Debug.Log(z);
                Debug.Log(islandMask);
                terrainData.SetIslandMask(
                    x,
                    z,
                    islandMask
                );

                terrainData.SetHeight(
                    x,
                    z,
                    height
                );
            }
        }

        return terrainData;
    }

    /// <summary>
    /// Creates a radial falloff so the noise
    /// becomes an island instead of an infinite
    /// terrain field.
    /// </summary>
    private Mesh GenerateTerrainMesh(
        IslandTerrainData terrainData)
    {
        int width = terrainData.width;
        int depth = terrainData.depth;

        Mesh mesh = new Mesh();

        Vector3[] vertices =
            new Vector3[width * depth];

        int[] triangles =
            new int[
                (width - 1) *
                (depth - 1) *
                6
            ];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int index =
                    x * depth + z;

                float normalizedX =
                    x /
                    (float)(width - 1);

                float normalizedZ =
                    z /
                    (float)(depth - 1);

                float worldX =
                    (normalizedX - 0.5f) *
                    terrainData.worldWidth;

                float worldZ =
                    (normalizedZ - 0.5f) *
                    terrainData.worldDepth;

                float worldY =
                    terrainData.heightMap[index] *
                    terrainData.maxHeight;

                vertices[index] =
                    new Vector3(
                        worldX,
                        worldY,
                        worldZ
                    );
            }
        }

        int triangleIndex = 0;

        for (int x = 0; x < width - 1; x++)
        {
            for (int z = 0; z < depth - 1; z++)
            {
                int bottomLeft =
                    x * depth + z;

                int bottomRight =
                    (x + 1) * depth + z;

                int topLeft =
                    x * depth + (z + 1);

                int topRight =
                    (x + 1) * depth + (z + 1);

                triangles[triangleIndex++] =
                    bottomLeft;

                triangles[triangleIndex++] =
                    topLeft;

                triangles[triangleIndex++] =
                    bottomRight;

                triangles[triangleIndex++] =
                    bottomRight;

                triangles[triangleIndex++] =
                    topLeft;

                triangles[triangleIndex++] =
                    topRight;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private float CalculateIslandMask(int x, int z)
    {
        float normalizedX =
            x / (float)(terrainSettings.width - 1);

        float normalizedZ =
            z / (float)(terrainSettings.depth - 1);

        float centeredX =
            normalizedX * 2f - 1f;

        float centeredZ =
            normalizedZ * 2f - 1f;

        float distance =
            new Vector2(
                centeredX,
                centeredZ
            ).magnitude;

        float radius =
            terrainSettings.islandRadius;

        if (distance >= radius)
            return 0f;

        float mask =
            1f - (distance / radius);

        // Smooth the transition.
        mask =
            mask * mask * (3f - 2f * mask);

        return mask;
    }

    #region Helpers
    private int GetOrCreateVertex(
    int x,
    int z,
    IslandTerrainData terrainData,
    List<Vector3> vertices,
    Dictionary<Vector2Int, int> vertexLookup)
    {
        Vector2Int coordinate =
            new Vector2Int(x, z);

        if (vertexLookup.TryGetValue(
            coordinate,
            out int existingIndex))
        {
            return existingIndex;
        }

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

        int vertexIndex =
            vertices.Count;

        vertices.Add(
            new Vector3(
                worldX,
                worldY,
                worldZ
            )
        );

        vertexLookup.Add(
            coordinate,
            vertexIndex
        );

        return vertexIndex;
    }

    /// <summary>
    /// Deletes the previously generated islands.
    /// </summary>
    private void ClearPreviousGeneration()
    {
        foreach (GameObject island
                 in generatedIslands)
        {
            if (island != null)
            {
                Destroy(island);
            }
        }

        generatedIslands.Clear();
        islandLocations.Clear();
    }
    #endregion
}