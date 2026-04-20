using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainGen : MonoBehaviour
{
    [SerializeField] private TerrainSettings settings;
    public float[,] heightMap = null;           // Exposing heightmap to program
    private Mesh terrainMesh;
    //private MeshData meshData;
    //private MeshFilter mFilter;
    //private MeshRenderer mRenderer;
    private float minHeight = float.MaxValue;
    private float maxHeight = float.MinValue;

    private void Start()
    {
        heightMap = GenerateHeightMap(settings.width, settings.height);

        for (int x = 0; x < settings.width; x++)
        {
            for (int z = 0; z < settings.height; z++)
            {
                float h = heightMap[x, z];
                if (h < minHeight) minHeight = h;
                if (h > maxHeight) maxHeight = h;
            }
        }

        settings.seedOffset = new Vector2(
            Mathf.Sin(settings.seed) * 1000f,
            Mathf.Cos(settings.seed) * 1000f
        );

        Debug.Log($"HEIGHT RANGE: {minHeight} -> {maxHeight}");

        terrainMesh = GenerateTerrainMesh(heightMap, minHeight, maxHeight);

        GetComponent<MeshFilter>().mesh = terrainMesh;

        //mFilter = GetComponent<MeshFilter>();
        //mRenderer = GetComponent<MeshRenderer>();
        //meshData = GenerateTerrainMesh2(heightMap);
        //DrawMesh(meshData);
        Debug.Log("Finished Terrain set up");
    }

    #region Noise gen 

    /// <summary>
    /// Generates a heightmap using fBm (Fractal Brownian Motion)
    /// Supports optional domain warping for more natural variation
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public float[,] GenerateHeightMap(int width, int height)
    {
        float[,] map = new float[width, height];

        // Main generation loop
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2 p = (new Vector2(x, z) + settings.seedOffset) / settings.scale;

                float heightValue = FractalNoise(p, settings);
                
                map[x, z] = heightValue;
            }
        }

        return map;
    }

    /// <summary>
    /// Summation of the fractial part of the noise
    /// </summary>
    /// <param name="p"></param>
    /// <param name="settings"></param>
    /// <returns></returns>
    private float FractalNoise(Vector2 p, TerrainSettings settings)
    {
        float sum = 0f;

        for (int n = 0; n < settings.maxOctaves; n++)
        {
            // Optional octave filtering (N set concept)
            if (!settings.activeOctaves.Contains(n))
                continue;

            float frequency = Mathf.Pow(settings.lacunarity, n);
            float amplitude = Mathf.Pow(settings.persistence, n);

            Vector2 transformed = ApplyRotation(p * frequency, n);

            sum += amplitude * GetValueNoise(transformed);
        }

        return sum;
    }

    /// <summary>
    /// Applys a rotation matrix to each noise value 
    /// </summary>
    /// <param name="p"></param>
    /// <param name="iteration"></param>
    /// <returns></returns>
    private Vector2 ApplyRotation(Vector2 p, int iteration)
    {
        /*
         * Using a fixed pseudo-rotation:
         * cos ~ 4/5, sin ~ 3/5
         * 
         * [ 4/5  -3/5 ]
         * [ 3/5   4/5 ]
         */

        float cos = 0.8f;
        float sin = 0.6f;

        // Apply rotation multiple times (M^k)
        for (int i = 0; i < iteration; i++)
        {
            p = new Vector2(
                cos * p.x - sin * p.y,
                sin * p.x + cos * p.y
            );
        }

        return p;
    }

    /// <summary>
    /// Gets a noise value at a givin coord in space
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    private float GetValueNoise(Vector2 p)
    {
        int i = Mathf.FloorToInt(p.x);
        int j = Mathf.FloorToInt(p.y);

        float x = p.x - i;
        float z = p.y - j;

        // Corner values
        float a = RandomValue(i, j);
        float b = RandomValue(i + 1, j);
        float c = RandomValue(i, j + 1);
        float d = RandomValue(i + 1, j + 1);

        // Smoothstep weights
        float sx = Smooth(x);
        float sz = Smooth(z);

        /*
         * Exact interpolation from notes:
         * f_ij(x,z) =
         * a +
         * (b - a)S(x) +
         * (c - a)S(z) +
         * (a - b - c + d)S(x)S(z)
         */

        float value =
            a +
            (b - a) * sx +
            (c - a) * sz +
            (a - b - c + d) * sx * sz;

        return value;
    }


    /// <summary>
    /// Lambda version of Smoothstep function used in the equations
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    private float Smooth(float t)
    {
        /* 
         * Lambda version based smoothstep function from my notes
         * S(lambda) = 3(lambda)^2 - 2(lambda)^3
         */
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// General Smoothstep function used in the equations
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    private float Smooth(float a, float b, float x)
    {
        /*
         * This is the smoothstep equation from my notes in a generalized form
         * S(a,b,x)
         */
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// Custom random num generator
    /// </summary>
    /// <param name="i"></param>
    /// <param name="j"></param>
    /// <returns></returns>
    private float RandomValue(int i, int j)
    {
        /*
         * In my notes:
         * (u,v) = 50 { (i,j) / pi }
         * a_ij = 2{uv(u+v)} - 1
         * 
         * This is approximated with a hash function
         */
        float x = Mathf.Sin(i * 12.9898f + 
            j * 78.233f) * 43758.5453f;
        return (x - Mathf.Floor(x)) * 2f - 1f;
    }
    #endregion

    #region Mesh gen
    /// <summary>
    /// Converts a heightmap into a mesh
    /// </summary>
    /// <param name="heightMap"></param>
    /// <returns></returns>
    public Mesh GenerateTerrainMesh(float[,] heightMap, float minHeight, float maxHeight)
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

                float rawHeight = heightMap[x, z];

                // Normalize based on ACTUAL data range
                float normalizedHeight = Mathf.InverseLerp(minHeight, maxHeight, rawHeight);

                // Apply curve shaping
                float shapedHeight = settings.heightCurve.Evaluate(normalizedHeight);

                // Final scaling
                float finalHeight = shapedHeight * settings.heightMultiplier;

                // Cliff scaling
                float cliffMask = Smooth(settings.cliffStart, settings.cliffEnd, normalizedHeight);
                finalHeight += cliffMask * settings.cliffStrength;

                vertices[i] = new Vector3(x, finalHeight, z);
                uvs[i] = new Vector2(x / (float)width, z / (float)height);

                if (x < width - 1 && z < height - 1)
                {
                    triangles[triIndex + 0] = i;
                    triangles[triIndex + 1] = i + width;
                    triangles[triIndex + 2] = i + width + 1;

                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + width + 1;
                    triangles[triIndex + 5] = i + 1;

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

    private MeshData GenerateTerrainMesh2(float[,] heightmap)
    {
        int width = heightmap.GetLength(0);
        int height = heightmap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        MeshData meshData = new MeshData(width, height);
        int vertIndex = 0;

        for(int y = 0; y < height; y++)
        {
            for(int x = 0; x < width; x++)
            {
                meshData.verts[vertIndex] = new Vector3(topLeftX + x, heightMap[x,y], topLeftZ - y);
                meshData.uvs[vertIndex] = new Vector2(x / (float)width, y / (float)height);

                if (x < width - 1 && y < height - 1)
                {
                    meshData.AddTri(vertIndex, vertIndex + width + 1, vertIndex + width);
                    meshData.AddTri(vertIndex + width + 1, vertIndex, vertIndex + 1);
                }

                vertIndex++;
            }
        }
        return meshData;
    }

    public void DrawMesh(MeshData meshData)
    {
        //mFilter.mesh = meshData.CreateMesh();
        //mRenderer.material.mainTexture = texture;
    }
    #endregion

    #region Terrain gen helpers

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

/// <summary>
/// Mesh data class to handle mesh creation for ater possible optimization like threading 
/// </summary>
public class MeshData
{
    public Vector3[] verts;
    public int[] triangles;
    public Vector2[] uvs;
    private int triIndex;

    public MeshData(int meshWidth, int meshHeight)
    {
        verts = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
    }

    public void AddTri(int a, int b, int c)
    {
        triangles[triIndex] = a;
        triangles[triIndex + 1] = b;
        triangles[triIndex + 2] = c;
        triIndex += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.triangles = triangles;
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}
