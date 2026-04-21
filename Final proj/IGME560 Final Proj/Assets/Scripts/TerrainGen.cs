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
    private float Smoothstep(float a, float b, float x)
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
                float cliffMask = Smoothstep(settings.cliffStart, settings.cliffEnd, normalizedHeight);
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

    #region alternate workflow testing
    // Entry point
    public static Vector2[,] GenerateTerrainMap(int size, float scale)
    {
        Vector2[,] map = new Vector2[size, size];

        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 samplePos = new Vector2(x * scale, z * scale);

                map[x, z] = TerrainMap(samplePos);
            }
        }

        return map;
    }

    // --------------------------------------------------
    // Constants
    // --------------------------------------------------

    static readonly Matrix4x4 m2 = new Matrix4x4(
        new Vector4(0.80f, 0.60f, 0f, 0f),
        new Vector4(-0.60f, 0.80f, 0f, 0f),
        new Vector4(0f, 0f, 1f, 0f),
        new Vector4(0f, 0f, 0f, 1f)
    );

    static readonly Matrix4x4 m2i = new Matrix4x4(
        new Vector4(0.80f, -0.60f, 0f, 0f),
        new Vector4(0.60f, 0.80f, 0f, 0f),
        new Vector4(0f, 0f, 1f, 0f),
        new Vector4(0f, 0f, 0f, 1f)
    );

    // --------------------------------------------------
    // Hash functions
    // --------------------------------------------------

    static float Hash1(Vector2 p)
    {
        p = 50.0f * Fract(p * 0.3183099f);
        return Fract(p.x * p.y * (p.x + p.y));
    }

    static Vector2 Hash2(Vector2 p)
    {
        Vector2 k = new Vector2(0.3183099f, 0.3678794f);
        float n = 111.0f * p.x + 113.0f * p.y;
        return Fract(n * Fract(k * n));
    }

    // --------------------------------------------------
    // Utility math
    // --------------------------------------------------

    static float Fract(float x) => x - Mathf.Floor(x);
    static Vector2 Fract(Vector2 v) => new Vector2(Fract(v.x), Fract(v.y));

    static Vector2 SmoothstepD(float a, float b, float x)
    {
        if (x < a) return new Vector2(0f, 0f);
        if (x > b) return new Vector2(1f, 0f);

        float ir = 1.0f / (b - a);
        x = (x - a) * ir;

        float value = x * x * (3.0f - 2.0f * x);
        float derivative = 6.0f * x * (1.0f - x) * ir;

        return new Vector2(value, derivative);
    }

    // --------------------------------------------------
    // Noise (2D value noise)
    // --------------------------------------------------

    static float Noise(Vector2 x)
    {
        Vector2 p = new Vector2(Mathf.Floor(x.x), Mathf.Floor(x.y));
        Vector2 w = Fract(x);
        Vector2 w2 = w * w;
        Vector2 w3 = w2 * w;

        // u = w^3 * (w*(6w - 15) + 10)
        Vector2 u = new Vector2(
            w3.x * (w.x * (6f * w.x - 15f) + 10f),
            w3.y * (w.y * (6f * w.y - 15f) + 10f)
        );

        float a = Hash1(p + new Vector2(0, 0));
        float b = Hash1(p + new Vector2(1, 0));
        float c = Hash1(p + new Vector2(0, 1));
        float d = Hash1(p + new Vector2(1, 1));

        return -1.0f + 2.0f * (a + (b - a) * u.x + (c - a) * u.y + (a - b - c + d) * u.x * u.y);
    }

    // Noise with derivatives
    static Vector3 NoiseD(Vector2 x)
    {
        Vector2 p = new Vector2(Mathf.Floor(x.x), Mathf.Floor(x.y));
        Vector2 w = Fract(x);

        Vector2 w2 = w * w;
        Vector2 w3 = w2 * w;

        // u = w^3 * (w*(6w - 15) + 10)
        Vector2 u = new Vector2(
            w3.x * (w.x * (6f * w.x - 15f) + 10f),
            w3.y * (w.y * (6f * w.y - 15f) + 10f)
        );

        // du = 30*w^2*(w*(w-2)+1)
        Vector2 du = new Vector2(
            30f * w2.x * (w.x * (w.x - 2f) + 1f),
            30f * w2.y * (w.y * (w.y - 2f) + 1f)
        );

        float a = Hash1(p + new Vector2(0, 0));
        float b = Hash1(p + new Vector2(1, 0));
        float c = Hash1(p + new Vector2(0, 1));
        float d = Hash1(p + new Vector2(1, 1));

        float k0 = a;
        float k1 = b - a;
        float k2 = c - a;
        float k4 = a - b - c + d;

        float value = -1.0f + 2.0f * (k0 + k1 * u.x + k2 * u.y + k4 * u.x * u.y);

        Vector2 deriv = 2.0f * new Vector2(
            du.x * (k1 + k4 * u.y),
            du.y * (k2 + k4 * u.x)
        );

        return new Vector3(value, deriv.x, deriv.y);
    }

    // --------------------------------------------------
    // FBM
    // --------------------------------------------------

    static float FBM9(Vector2 x)
    {
        float f = 1.9f;
        float s = 0.55f;
        float a = 0.0f;
        float b = 0.5f;

        for (int i = 0; i < 9; i++)
        {
            float n = Noise(x);
            a += b * n;
            b *= s;

            x = Multiply(m2, x) * f;
        }

        return a;
    }

    static Vector3 FBMD9(Vector2 x)
    {
        float f = 1.9f;
        float s = 0.55f;
        float a = 0.0f;
        float b = 0.5f;

        Vector2 d = Vector2.zero;
        Matrix4x4 m = Matrix4x4.identity;

        for (int i = 0; i < 9; i++)
        {
            Vector3 n = NoiseD(x);

            a += b * n.x;
            Vector2 grad = Multiply(m, new Vector2(n.y, n.z));
            d += b * grad;

            b *= s;
            x = Multiply(m2, x) * f;

            // matrix update = (m2i * m) scaled by f
            m = ScaleMatrix(m2i * m, f);
        }

        return new Vector3(a, d.x, d.y);
    }

    public static Vector2 Multiply(Matrix4x4 m, Vector2 v)
    {
        return new Vector2(
            m.m00 * v.x + m.m01 * v.y,
            m.m10 * v.x + m.m11 * v.y
        );
    }

    // Scales a matrix uniformly (equivalent to GLSL scalar-matrix multiplication)
    public static Matrix4x4 ScaleMatrix(Matrix4x4 m, float s)
    {
        for (int i = 0; i < 4; i++)
        {
            m.SetRow(i, m.GetRow(i) * s);
        }
        return m;
    }

    // --------------------------------------------------
    // Terrain
    // --------------------------------------------------

    // Returns height and slope mask
    // This is the main entry point that defins f(x,z)
    public static Vector2 TerrainMap(Vector2 p)
    {
        float e = FBM9(p / 2000.0f + new Vector2(1.0f, -2.0f));

        float a = 1.0f - Mathf.SmoothStep(0.12f, 0.13f, Mathf.Abs(e + 0.12f));

        e = 600.0f * e + 600.0f;

        // cliff shaping
        e += 90.0f * Mathf.SmoothStep(552.0f, 594.0f, e);

        return new Vector2(e, a);
    }

    // Returns height and normal
    public static Vector4 TerrainMapD(Vector2 p)
    {
        Vector3 e = FBMD9(p / 2000.0f + new Vector2(1.0f, -2.0f));

        e.x = 600.0f * e.x + 600.0f;
        e.y *= 600.0f;
        e.z *= 600.0f;

        Vector2 c = SmoothstepD(550.0f, 600.0f, e.x);

        e.x += 90.0f * c.x;
        e.y += 90.0f * c.y * e.y;
        e.z += 90.0f * c.y * e.z;

        e.y /= 2000.0f;
        e.z /= 2000.0f;

        Vector3 normal = new Vector3(-e.y, 1.0f, -e.z).normalized;

        return new Vector4(e.x, normal.x, normal.y, normal.z);
    }

    // Get terrain normal only
    public static Vector3 TerrainNormal(Vector2 pos)
    {
        Vector4 data = TerrainMapD(pos);
        return new Vector3(data.y, data.z, data.w);
    }

    // Convenience function for height only
    public static float GetHeight(Vector2 pos)
    {
        return TerrainMap(pos).x;
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
