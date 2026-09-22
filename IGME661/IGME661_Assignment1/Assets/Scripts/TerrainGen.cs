using Assets.Scripts;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class TerrainGen : MonoBehaviour
{
    [Header("Terrain Settings")]
    [SerializeField]
    private TerrainSettings terrainSettings;

    [Header("Island Generation")]
    [SerializeField]
    private int islandCount = 3;

    [SerializeField]
    private float islandSpacing = 500f;

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
        #region Initial dependency checks
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
        #endregion

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

        // Spawn water plane
        GameObject water = new GameObject("water");
        MeshFilter mFilter = water.AddComponent<MeshFilter>();
        MeshRenderer mRend = water.AddComponent<MeshRenderer>();
        mFilter.mesh = terrainSettings.waterMesh;
        mRend.material = terrainSettings.waterMaterial;
        water.transform.position = new Vector3(0, terrainSettings.seaLevel + terrainSettings.waterHeightMod, 0);
        water.transform.localScale = new Vector3(terrainSettings.worldWidth, 1, terrainSettings.worldDepth);
        Instantiate(water);
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
    private void SpawnIsland(Vector3 location, int seed)
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

        int totalSamples =
            terrainSettings.width *
            terrainSettings.depth;

        using NativeArray<float> noiseMap =
            new NativeArray<float>(
                totalSamples,
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


        // ---------------------------------------------
        // SHAPE MASK
        // ---------------------------------------------

        float[] shapeMask =
            GenerateIslandShapeMask(seed);


        // ---------------------------------------------
        // HEIGHT + SHAPE
        // ---------------------------------------------

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

                float shape =
                    shapeMask[index];


                // -----------------------------------------
                // Main terrain noise
                // -----------------------------------------

                float terrainNoiseValue =
                    Mathf.Clamp01(
                        noiseMap[index]
                    );


                // -----------------------------------------
                // Independent height mask
                // -----------------------------------------

                float heightX =
                    terrainSettings.perlinSeed +
                    seed * 37.71f +
                    (float)x /
                    (terrainSettings.width - 1) *
                    terrainSettings.perlinHeightScale;

                float heightZ =
                    terrainSettings.perlinSeed +
                    seed * 37.71f +
                    (float)z /
                    (terrainSettings.depth - 1) *
                    terrainSettings.perlinHeightScale;

                float heightMask =
                    Mathf.PerlinNoise(
                        heightX,
                        heightZ
                    );


                // -----------------------------------------
                // Calculate land elevation
                // -----------------------------------------

                float landHeight =
                    terrainNoiseValue;

                float heightModifier =
                    Mathf.Lerp(
                        1f -
                        terrainSettings.heightMaskStrength,

                        1f +
                        terrainSettings.heightMaskStrength,

                        heightMask
                    );

                landHeight *=
                    heightModifier;

                landHeight =
                    Mathf.Clamp01(
                        landHeight
                    );


                // -----------------------------------------
                // Keep the outside of the island
                // slightly below sea level
                // -----------------------------------------

                float underwaterHeight =
                    Mathf.Min(
                        0f,
                        terrainSettings.seaLevel -
                        terrainSettings.underwaterDepth
                    );


                // Shape mask controls whether we use
                // underwater terrain or land terrain.
                float finalHeight =
                    Mathf.Lerp(
                        underwaterHeight,
                        landHeight,
                        shape
                    );


                terrainData.SetIslandMask(
                    x,
                    z,
                    shape
                );

                terrainData.SetHeight(
                    x,
                    z,
                    finalHeight
                );
            }
        }

        return terrainData;
    }

    /// <summary>
    /// Generates the island's actual footprint from a 2D noise field.
    ///
    /// Noise values below the threshold are considered water.
    /// Values above the threshold are considered land.
    ///
    /// A connected-region pass ensures that we get one island
    /// instead of several unrelated blobs of land.
    /// </summary>
    private float[] GenerateIslandShapeMask(int seed)
    {
        int width = terrainSettings.width;
        int depth = terrainSettings.depth;

        int totalSamples = width * depth;

        float[] mask = new float[totalSamples];

        // Stores whether each sample is above the
        // shape-noise threshold.
        bool[] landCandidates =
            new bool[totalSamples];

        // ---------------------------------------------
        // 1. Generate the raw thresholded noise field
        // ---------------------------------------------

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int index =
                    x * depth + z;

                float normalizedX =
                    x / (float)(width - 1);

                float normalizedZ =
                    z / (float)(depth - 1);

                // Convert to coordinates centered around the island.
                float centeredX =
                    normalizedX * 2f - 1f;

                float centeredZ =
                    normalizedZ * 2f - 1f;

                // Aspect ratio of the area being sampled.
                centeredX /= terrainSettings.shapeScaleX;
                centeredZ /= terrainSettings.shapeScaleZ;

                // Convert back into a convenient 0-1 space.
                float sampleX =
                    (centeredX + 1f) * 0.5f;

                float sampleZ =
                    (centeredZ + 1f) * 0.5f;

                float noiseX =
                    terrainSettings.perlinSeed +
                    seed * 13.17f +
                    sampleX *
                    terrainSettings.perlinMaskScale;

                float noiseZ =
                    terrainSettings.perlinSeed +
                    seed * 13.17f +
                    sampleZ *
                    terrainSettings.perlinMaskScale;

                float noise =
                    Mathf.PerlinNoise(
                        noiseX,
                        noiseZ
                    );

                // Outside the maximum allowed island area.
                float distance =
                    new Vector2(
                        centeredX,
                        centeredZ
                    ).magnitude;

                if (distance >
                    terrainSettings.islandMaxRadius)
                {
                    landCandidates[index] = false;
                    continue;
                }

                // The noise itself determines whether this
                // location belongs to the island.
                landCandidates[index] =
                    noise >=
                    terrainSettings.perlinShapeThreshold;
            }
        }


        // ---------------------------------------------
        // 2. Find a starting land point
        // ---------------------------------------------

        int startIndex =
            FindIslandStart(
                landCandidates,
                width,
                depth
            );

        if (startIndex == -1)
        {
            Debug.LogWarning(
                "Island generation failed: " +
                "no noise region was above the shape threshold."
            );

            return mask;
        }


        // ---------------------------------------------
        // 3. Flood-fill the connected region
        // ---------------------------------------------

        bool[] connected =
            new bool[totalSamples];

        Queue<int> queue =
            new Queue<int>();

        queue.Enqueue(startIndex);
        connected[startIndex] = true;

        while (queue.Count > 0)
        {
            int current =
                queue.Dequeue();

            int x =
                current / depth;

            int z =
                current % depth;

            // 8-neighbor search allows diagonal
            // pixels to belong to the same landmass.
            for (int offsetX = -1;
                 offsetX <= 1;
                 offsetX++)
            {
                for (int offsetZ = -1;
                     offsetZ <= 1;
                     offsetZ++)
                {
                    if (offsetX == 0 &&
                        offsetZ == 0)
                    {
                        continue;
                    }

                    int neighborX =
                        x + offsetX;

                    int neighborZ =
                        z + offsetZ;

                    if (neighborX < 0 ||
                        neighborX >= width ||
                        neighborZ < 0 ||
                        neighborZ >= depth)
                    {
                        continue;
                    }

                    int neighborIndex =
                        neighborX * depth + neighborZ;

                    if (!landCandidates[neighborIndex] ||
                        connected[neighborIndex])
                    {
                        continue;
                    }

                    connected[neighborIndex] = true;

                    queue.Enqueue(
                        neighborIndex
                    );
                }
            }
        }


        // ---------------------------------------------
        // 4. Convert the connected region into
        //    the final island mask
        // ---------------------------------------------

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int index =
                    x * depth + z;

                if (!connected[index])
                {
                    mask[index] = 0f;
                    continue;
                }

                // Hard threshold by default.
                mask[index] = 1f;
            }
        }

        return mask;
    }

    /// <summary>
    /// Finds the nearest valid land sample to the center
    /// of the heightmap.
    ///
    /// This gives the flood-fill a deterministic starting
    /// point for the main island.
    /// </summary>
    private int FindIslandStart(bool[] landCandidates, int width, int depth)
    {
        int centerX =
            width / 2;

        int centerZ =
            depth / 2;

        int maxSearchRadius =
            Mathf.Max(width, depth);

        for (int radius = 0;
             radius < maxSearchRadius;
             radius++)
        {
            for (int x = centerX - radius;
                 x <= centerX + radius;
                 x++)
            {
                for (int z = centerZ - radius;
                     z <= centerZ + radius;
                     z++)
                {
                    if (x < 0 ||
                        x >= width ||
                        z < 0 ||
                        z >= depth)
                    {
                        continue;
                    }

                    int index =
                        x * depth + z;

                    if (landCandidates[index])
                    {
                        return index;
                    }
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Converts the heightmap into an island-shaped mesh.
    ///
    /// A quad is only created if its average normalized
    /// height is above sea level. This is what removes the
    /// rectangular "outside" portion of the terrain.
    /// </summary>
    private Mesh GenerateTerrainMesh(IslandTerrainData terrainData)
    {
        int width = terrainData.width;
        int depth = terrainData.depth;
        Mesh mesh = new Mesh();

        mesh.name = "Procedural Island";

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        // Create Tris
        List<int> triangles = new List<int>(
            (width - 1) * (depth - 1) * 6);

        // Create the mesh
        for (int x = 0; x < width - 1; x++)
        {
            for (int z = 0; z < depth - 1; z++)
            {
                // Finding if the quad is part of the island shape from a mask
                float mask00 = terrainData.GetIslandMask(x, z);
                float mask10 = terrainData.GetIslandMask(x + 1, z);
                float mask01 = terrainData.GetIslandMask(x, z + 1);
                float mask11 = terrainData.GetIslandMask(x + 1, z + 1);

                // If all four corners are outside the island,
                // don't create this quad.
                if (mask00 <= 0f &&
                    mask10 <= 0f &&
                    mask01 <= 0f &&
                    mask11 <= 0f)
                {
                    continue;
                }

                // Create the four vertices for this quad
                float normalizedX = x / (float)(width - 1);
                float normalizedZ = z / (float)(depth - 1);
                float normalizedXNext = (x + 1) / (float)(width - 1);
                float normalizedZNext = (z + 1) / (float)(depth - 1);

                float worldX = (normalizedX - 0.5f) * terrainData.worldWidth;
                float worldZ = (normalizedZ - 0.5f) * terrainData.worldDepth;
                float worldXNext = (normalizedXNext - 0.5f) * terrainData.worldWidth;
                float worldZNext = (normalizedZNext - 0.5f) * terrainData.worldDepth;

                float height00 = terrainData.GetHeight(x, z) * terrainData.maxHeight;
                float height10 = terrainData.GetHeight(x + 1, z) * terrainData.maxHeight;
                float height01 = terrainData.GetHeight(x, z + 1) * terrainData.maxHeight;
                float height11 = terrainData.GetHeight(x + 1, z + 1) * terrainData.maxHeight;

                Vector3 bottomLeft = new Vector3(
                    worldX,
                    height00,
                    worldZ
                );

                Vector3 bottomRight = new Vector3(
                    worldXNext,
                    height10,
                    worldZ
                );

                Vector3 topLeft = new Vector3(
                    worldX,
                    height01,
                    worldZNext
                );

                Vector3 topRight = new Vector3(
                    worldXNext,
                    height11,
                    worldZNext
                );

                // Determine texture for this quad
                // Find the deciding factors 
                float averageHeight = (
                    terrainData.GetHeight(x, z) +
                    terrainData.GetHeight(x + 1, z) +
                    terrainData.GetHeight(x, z + 1) +
                    terrainData.GetHeight(x + 1, z + 1)) / 4f;

                Vector3 normal = Vector3.Cross(
                    topLeft - bottomLeft,
                    bottomRight - bottomLeft
                ).normalized;

                float slope = 1f - normal.y;
                int tileX = 0;
                int tileY = 0;

                bool isCoastal = IsCoastalQuad(mask00, mask10, mask01, mask11);

                // Determine the UVs on the atlas and add them
                // Sand
                if (averageHeight < terrainSettings.sandHeight || isCoastal)
                {
                    tileX = 4;
                    tileY = 0;
                }
                // Rock
                else if (slope > terrainSettings.rockSlope)
                {
                    tileX = 4;
                    tileY = 0;
                }
                // Grass 1
                else if (averageHeight < terrainSettings.grass2Height)
                {
                    tileX = 0;
                    tileY = 2;
                }
                // Grass 2
                else
                {
                    tileX = 2;
                    tileY = 2;
                }

                // Add the four vertices for this quad
                int vertexIndex =
                    vertices.Count;

                vertices.Add(bottomLeft);
                vertices.Add(topLeft);
                vertices.Add(bottomRight);
                vertices.Add(topRight);

                // Triangle 1
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);

                // Triangle 2
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);

                // Add UVs for this quad
                AddAtlasUVs(
                    uvs,
                    tileX,
                    tileY,
                    5
                );
            }
        }

        if (triangles.Count == 0)
        {
            Debug.LogWarning(
                "TerrainGen: No island geometry was generated."
            );

            return null;
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }


    /// <summary>
    /// Takes a quad from the terrain and maps it to the part of the atlas
    /// </summary>
    /// <param name="uvs">the list of uvs</param>
    /// <param name="tileX">desired texture column</param>
    /// <param name="tileY">desired texture row</param>
    private void AddAtlasUVs(List<Vector2> uvs, int tileX, int tileY, int atlasSize)
    {
        // Finding the coordinate of the texture needed on the atlas instead of using the entire texture
        float tileWidth = 1.0f / atlasSize;
        float tileHeight = 1.0f / atlasSize;

        float minX = tileX * tileWidth + 0.20f;
        float minY = tileY * tileHeight + 0.20f;

        float maxX = minX + tileWidth - 0.20f;
        float maxY = minY + tileHeight - 0.20f;

        uvs.Add(new Vector2(minX, minY));
        uvs.Add(new Vector2(minX, maxY));
        uvs.Add(new Vector2(maxX, minY));
        uvs.Add(new Vector2(maxX, maxY));
    }


    /// <summary>
    /// Creates a separate low-frequency noise field used
    /// to modify terrain elevation without changing the
    /// island's overall silhouette.
    /// </summary>
    private float CalculateHeightMask(
        int x,
        int z,
        int seed)
    {
        float normalizedX =
            x /
            (float)(terrainSettings.width - 1);

        float normalizedZ =
            z /
            (float)(terrainSettings.depth - 1);

        // Offset the height noise differently from
        // the shape noise so the two patterns remain independent.
        float seedOffset =
            seed * 31.739f;

        float heightX =
            terrainSettings.perlinSeed +
            seedOffset +
            normalizedX *
            terrainSettings.perlinHeightScale;

        float heightZ =
            terrainSettings.perlinSeed +
            seedOffset +
            normalizedZ *
            terrainSettings.perlinHeightScale;

        return Mathf.PerlinNoise(
            heightX,
            heightZ
        );
    }

    /// <summary>
    /// Determines if a terrain quad is close to the water.
    /// </summary>
    private bool IsCoastalQuad( float mask00, float mask10, float mask01, float mask11)
    {
        bool hasLand =
            mask00 > 0f ||
            mask10 > 0f ||
            mask01 > 0f ||
            mask11 > 0f;
    
        bool hasWater =
            mask00 <= 0f ||
            mask10 <= 0f ||
            mask01 <= 0f ||
            mask11 <= 0f;
    
        return hasLand && hasWater;
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