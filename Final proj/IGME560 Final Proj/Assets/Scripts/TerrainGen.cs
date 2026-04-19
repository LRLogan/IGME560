using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainGen : MonoBehaviour
{
    [SerializeField] private TerrainSettings settings;
    public float[,] heightMap = null;           // Exposing heightmap to program

    private void Start()
    {
        heightMap = GenerateHeightMap(settings.width, settings.height);
    }

    #region Noise gen 

    /// <summary>
    /// Generates a heightmap using fBm (Fractal Brownian Motion)
    /// Supports optional domain warping for more natural variation</summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public float[,] GenerateHeightMap(int width, int height)
    {
        float[,] map = new float[width, height];

        System.Random prng = new System.Random(settings.seed);

        // Random offsets per octave to avoid tiling artifacts
        Vector2[] octaveOffsets = new Vector2[settings.octaves];
        for (int i = 0; i < settings.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                float sampleX = x / settings.scale;
                float sampleZ = z / settings.scale;

                // DOMAIN WARPING (optional but high impact)
                if (settings.useDomainWarping)
                {
                    float warpX = Mathf.PerlinNoise(sampleX, sampleZ) * settings.warpStrength;
                    float warpZ = Mathf.PerlinNoise(sampleZ, sampleX) * settings.warpStrength;

                    sampleX += warpX;
                    sampleZ += warpZ;
                }

                for (int i = 0; i < settings.octaves; i++)
                {
                    float xCoord = sampleX * frequency + octaveOffsets[i].x;
                    float zCoord = sampleZ * frequency + octaveOffsets[i].y;

                    float perlinValue = Mathf.PerlinNoise(xCoord, zCoord) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= settings.persistence;
                    frequency *= settings.lacunarity;
                }

                map[x, z] = noiseHeight;
            }
        }

        return map;
    }
    #endregion

    #region Mesh gen
    /*
     * Converts a heightmap into a mesh
     * This is what actually renders your terrain
     */
    public Mesh GenerateTerrainMesh(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Vector3[] vertices = new Vector3[width * height];
        int[] triangles = new int[(width - 1) * (height - 1) * 6];
        Vector2[] uvs = new Vector2[vertices.Length];

        int triIndex = 0;

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = z * width + x;

                float heightValue = settings.heightCurve.Evaluate(heightMap[x, z]) * settings.heightMultiplier;

                vertices[i] = new Vector3(x, heightValue, z);
                uvs[i] = new Vector2(x / (float)width, z / (float)height);

                if (x < width - 1 && z < height - 1)
                {
                    triangles[triIndex + 0] = i;
                    triangles[triIndex + 1] = i + width + 1;
                    triangles[triIndex + 2] = i + width;

                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + 1;
                    triangles[triIndex + 5] = i + width + 1;

                    triIndex += 6;
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        return mesh;
    }
    #endregion

    #region Terrain gen
    /*
     * Blocked out due to lack of noise reference / different archatecture
    public void Generate()
    {
        // Generate height data
        heightMap = Noise.GenerateHeightMap(settings.width, settings.height, settings);

        // Convert to mesh
        Mesh mesh = MeshGenerator.GenerateTerrainMesh(heightMap, settings);

        GetComponent<MeshFilter>().mesh = mesh;
    }
    */

    /*
     * This function will be VERY important later
     * It allows your ecosystem system to query terrain data
     */
    public float GetHeight(int x, int z)
    {
        return heightMap[x, z];
    }

    /*
     * Computes slope using neighboring height differences
     * Useful for:
     * - Tree placement
     * - Rock placement
     */
    public float GetSlope(int x, int z)
    {
        float h = GetHeight(x, z);
        float hx = GetHeight(Mathf.Clamp(x + 1, 0, settings.width - 1), z);
        float hz = GetHeight(x, Mathf.Clamp(z + 1, 0, settings.height - 1));

        float dx = Mathf.Abs(h - hx);
        float dz = Mathf.Abs(h - hz);

        return dx + dz;
    }
    #endregion
}
