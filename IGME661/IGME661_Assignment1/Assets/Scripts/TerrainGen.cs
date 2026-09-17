using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Assets.Scripts;

public class TerrainGen : MonoBehaviour
{
    [Header("Terrain Settings")]
    [SerializeField]
    private TerrainSettings terrainSettings;

    [Header("Island Generation")]
    [SerializeField]
    private int islandCount = 3;

    [SerializeField]
    private float islandSpacing = 600f;

    [SerializeField]
    private float spawnAreaWidth = 1800f;

    [SerializeField]
    private float spawnAreaDepth = 1800f;

    [SerializeField]
    private Transform islandParent;

    [Header("Debug")]
    [SerializeField]
    private bool generateMeshCollider = true;

    private NoiseAlgorithm terrainNoise;

    private readonly List<GameObject> generatedIslands = new();
    private readonly List<Mesh> generatedMeshes = new();
    private readonly List<Vector3> islandLocations = new();

    private bool initialized;

    private void Awake()
    {
        InitializeGenerator();
    }

    /// <summary>
    /// Initializes the noise generator and generation parent.
    /// </summary>
    private void InitializeGenerator()
    {
        if (initialized)
            return;

        if (terrainSettings == null)
        {
            Debug.LogError(
                "TerrainGen: TerrainSettings has not been assigned."
            );

            return;
        }

        if (terrainSettings.width < 2 ||
            terrainSettings.depth < 2)
        {
            Debug.LogError(
                "TerrainGen: Heightmap width and depth must be at least 2."
            );

            return;
        }

        if (terrainSettings.width != terrainSettings.depth)
        {
            Debug.LogError(
                "TerrainGen: For the current NoiseAlgorithm, " +
                "width and depth should be the same."
            );

            return;
        }

        if (terrainSettings.maxHeight <= 0f)
        {
            Debug.LogError(
                "TerrainGen: maxHeight must be greater than 0."
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

        initialized = true;
    }

    /// <summary>
    /// Main terrain generation entry point.
    /// SimManager can call this.
    /// </summary>
    public void StartFullTerrainGen()
    {
        InitializeGenerator();

        if (!initialized)
            return;

        ClearPreviousGeneration();

        GenerateIslandLocations();

        for (int i = 0; i < islandLocations.Count; i++)
        {
            int islandSeed =
                terrainSettings.voronoiRandSeed + i;

            SpawnIsland(
                islandLocations[i],
                islandSeed
            );
        }
    }

    /// <summary>
    /// Creates deterministic random positions for the islands.
    /// Voronoi can replace this later.
    /// </summary>
    private void GenerateIslandLocations()
    {
        islandLocations.Clear();

        Random.InitState(
            terrainSettings.voronoiRandSeed
        );

        int attempts = 0;
        int maxAttempts = Mathf.Max(
            islandCount * 50,
            50
        );

        while (
            islandLocations.Count < islandCount &&
            attempts < maxAttempts)
        {
            attempts++;

            float x = Random.Range(
                -spawnAreaWidth * 0.5f,
                spawnAreaWidth * 0.5f
            );

            float z = Random.Range(
                -spawnAreaDepth * 0.5f,
                spawnAreaDepth * 0.5f
            );

            Vector3 candidate =
                new Vector3(x, 0f, z);

            bool tooClose = false;

            foreach (Vector3 existing
                     in islandLocations)
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
                $"TerrainGen: Generated " +
                $"{islandLocations.Count} of " +
                $"{islandCount} requested islands. " +
                $"Increase spawn area or decrease island spacing."
            );
        }
    }

    /// <summary>
    /// Generates one island's data and creates its mesh object.
    /// </summary>
    private void SpawnIsland(
        Vector3 location,
        int seed)
    {
        IslandTerrainData terrainData =
            GenerateIslandTerrain(seed);

        if (terrainData == null)
            return;

        Mesh islandMesh =
            GenerateTerrainMesh(terrainData);

        if (islandMesh == null ||
            islandMesh.vertexCount == 0 ||
            islandMesh.triangles.Length == 0)
        {
            Debug.LogWarning(
                $"TerrainGen: Island at {location} " +
                "did not produce usable geometry."
            );

            return;
        }

        GameObject island =
            new GameObject(
                $"Island_{generatedIslands.Count}"
            );

        island.transform.SetParent(
            islandParent,
            false
        );

        island.transform.position =
            location;

        MeshFilter meshFilter =
            island.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer =
            island.AddComponent<MeshRenderer>();

        meshFilter.sharedMesh =
            islandMesh;

        if (terrainSettings.terrainMaterial != null)
        {
            meshRenderer.sharedMaterial =
                terrainSettings.terrainMaterial;
        }
        else
        {
            Debug.LogWarning(
                "TerrainGen: No terrain material assigned."
            );
        }

        if (generateMeshCollider)
        {
            MeshCollider meshCollider =
                island.AddComponent<MeshCollider>();

            meshCollider.sharedMesh =
                islandMesh;
        }

        generatedIslands.Add(island);
        generatedMeshes.Add(islandMesh);
    }

    /// <summary>
    /// Generates normalized noise, then applies an island falloff.
    /// </summary>
    private IslandTerrainData GenerateIslandTerrain(
        int seed)
    {
        IslandTerrainData terrainData =
            new IslandTerrainData(
                terrainSettings.width,
                terrainSettings.depth,
                terrainSettings.worldWidth,
                terrainSettings.worldDepth,
                terrainSettings.maxHeight
            );

        int totalSamples =
            terrainSettings.width *
            terrainSettings.depth;

        using NativeArray<float> noiseMap =
            new NativeArray<float>(
                totalSamples,
                Allocator.TempJob
            );

        // The noise generator uses the seed to create
        // a deterministic heightmap for this island.
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
                    Mathf.Clamp01(noiseMap[index]);

                float mask =
                    CalculateIslandMask(x, z);

                float height =
                    noise * mask;

                terrainData.SetIslandMask(
                    x,
                    z,
                    mask
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
    /// Generates a radial falloff.
    /// Center = 1, edge = 0.
    /// Multiplying noise by this makes the terrain become an island.
    /// </summary>
    private float CalculateIslandMask(
        int x,
        int z)
    {
        float normalizedX =
            x /
            (float)(terrainSettings.width - 1);

        float normalizedZ =
            z /
            (float)(terrainSettings.depth - 1);

        // Convert 0..1 into -1..1.
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

        // 1 at the center, 0 at the radius.
        float mask =
            1f - distance / radius;

        // Smoothstep for a softer shoreline.
        return mask * mask * (3f - 2f * mask);
    }

    /// <summary>
    /// Converts the heightmap into an island-shaped mesh.
    ///
    /// A quad is only created if its average normalized
    /// height is above sea level. This is what removes the
    /// rectangular "outside" portion of the terrain.
    /// </summary>
    private Mesh GenerateTerrainMesh(
        IslandTerrainData terrainData)
    {
        int width =
            terrainData.width;

        int depth =
            terrainData.depth;

        Mesh mesh =
            new Mesh();

        mesh.name =
            "Procedural Island";

        Vector3[] vertices =
            new Vector3[width * depth];

        // Create all possible vertex positions.
        // Some vertices will not be referenced if they
        // fall completely outside the island.
        for (int x = 0;
             x < width;
             x++)
        {
            for (int z = 0;
                 z < depth;
                 z++)
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
                    terrainData.GetHeight(x, z) *
                    terrainData.maxHeight;

                vertices[index] =
                    new Vector3(
                        worldX,
                        worldY,
                        worldZ
                    );
            }
        }

        List<int> triangles =
            new List<int>(
                (width - 1) *
                (depth - 1) *
                6
            );

        float seaLevel =
            terrainSettings.seaLevel;

        for (int x = 0;
             x < width - 1;
             x++)
        {
            for (int z = 0;
                 z < depth - 1;
                 z++)
            {
                int bottomLeft =
                    x * depth + z;

                int bottomRight =
                    (x + 1) * depth + z;

                int topLeft =
                    x * depth + (z + 1);

                int topRight =
                    (x + 1) * depth + (z + 1);

                float h00 =
                    terrainData.GetHeight(
                        x,
                        z
                    );

                float h10 =
                    terrainData.GetHeight(
                        x + 1,
                        z
                    );

                float h01 =
                    terrainData.GetHeight(
                        x,
                        z + 1
                    );

                float h11 =
                    terrainData.GetHeight(
                        x + 1,
                        z + 1
                    );

                // Use the average height of the four
                // corners to decide whether this grid cell
                // belongs to the island.
                float cellHeight =
                    (h00 + h10 + h01 + h11) * 0.25f;

                if (cellHeight <= seaLevel)
                    continue;

                // Triangle 1
                triangles.Add(bottomLeft);
                triangles.Add(topLeft);
                triangles.Add(bottomRight);

                // Triangle 2
                triangles.Add(bottomRight);
                triangles.Add(topLeft);
                triangles.Add(topRight);
            }
        }

        if (triangles.Count == 0)
        {
            Debug.LogWarning(
                "TerrainGen: Heightmap produced no " +
                "terrain above sea level."
            );

            return null;
        }

        mesh.vertices =
            vertices;

        mesh.triangles =
            triangles.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// Removes previously generated island objects.
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
        generatedMeshes.Clear();
    }
}