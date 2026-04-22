using System;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainGen : MonoBehaviour
{
    [SerializeField] private TerrainSettings settings;
    public Vector2[,] heightMap = null;         
    private Mesh terrainMesh;
    private float minHeight = float.MaxValue;
    private float maxHeight = float.MinValue;

    private void Start()
    {
        // Placing terrain in center
        this.gameObject.transform.position = new Vector3(-settings.size / 2, 0, -settings.size / 2);

        // Generate the height map
        heightMap = GenerateTerrainHeightMap(settings);

        // --------------------------------------------------
        // Debug for height map
        for (int x = 0; x < settings.size; x++)
        {
            for (int z = 0; z < settings.size; z++)
            {
                float h = heightMap[x, z].x;
                if (h < minHeight) minHeight = h;
                if (h > maxHeight) maxHeight = h;
            }
        }
        settings.seedOffset = new Vector2(
            Mathf.Sin(settings.seed) * 1000f,
            Mathf.Cos(settings.seed) * 1000f
        );
        Debug.Log($"HEIGHT RANGE: {minHeight} -> {maxHeight}");
        // --------------------------------------------------

        // Generate the terrain mesh
        terrainMesh = GenerateTerrainMesh(heightMap, minHeight, maxHeight, settings);        
        GetComponent<MeshFilter>().mesh = terrainMesh;

        Debug.Log("Finished Terrain set up");
    }

    #region Noise gen 
    
    /// <summary>
    /// Entry point that constructs the height map
    /// </summary>
    /// <param name="size"></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    public static Vector2[,] GenerateTerrainHeightMap(TerrainSettings settings)
    {
        int size = settings.size;
        Vector2[,] map = new Vector2[size, size];

        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 samplePos = new Vector2(x , z) + settings.seedOffset;

                map[x, z] = TerrainMap(samplePos, settings);
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
    // Hash function
    // --------------------------------------------------

    static float Hash1(Vector2 p)
    {
        p = 50.0f * Fract(p * 0.3183099f);
        return Fract(p.x * p.y * (p.x + p.y));
    }

    // --------------------------------------------------
    // Mat utilities 
    // --------------------------------------------------

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

    static float Fract(float x) => x - Mathf.Floor(x);
    static Vector2 Fract(Vector2 v) => new Vector2(Fract(v.x), Fract(v.y));

    /// <summary>
    /// Smoothstep function that returns a snoothed value and its derivative 
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="x"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Simple vector multply math helper
    /// </summary>
    /// <param name="m"></param>
    /// <param name="v"></param>
    /// <returns></returns>
    public static Vector2 Multiply(Matrix4x4 m, Vector2 v)
    {
        return new Vector2(
            m.m00 * v.x + m.m01 * v.y,
            m.m10 * v.x + m.m11 * v.y
        );
    }

    /// <summary>
    /// Scales a matrix uniformly (equivalent to GLSL scalar-matrix multiplication)
    /// </summary>
    /// <param name="m"></param>
    /// <param name="s"></param>
    /// <returns></returns>
    public static Matrix4x4 ScaleMatrix(Matrix4x4 m, float s)
    {
        for (int i = 0; i < 4; i++)
        {
            m.SetRow(i, m.GetRow(i) * s);
        }
        return m;
    }

    // --------------------------------------------------
    // Noise generation
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

    /// <summary>
    /// Returns noise with derivatives
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
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
    // Fractial Brownian Motion generation
    // --------------------------------------------------

    /// <summary>
    /// Generates fbm 
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    static float generateFBM(Vector2 x, int iterations)
    {
        float f = 1.9f;
        float s = 0.55f;
        float a = 0.0f;
        float b = 0.5f;

        for (int i = 0; i < iterations; i++)
        {
            float n = Noise(x);
            a += b * n;
            b *= s;

            x = Multiply(m2, x) * f;
        }

        return a;
    }

    /// <summary>
    /// Generated fbm with a derivative 
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    static Vector3 generateFBMWithD(Vector2 x, int iterations)
    {
        float f = 1.9f;
        float s = 0.55f;
        float a = 0.0f;
        float b = 0.5f;

        Vector2 d = Vector2.zero;
        Matrix4x4 m = Matrix4x4.identity;

        for (int i = 0; i < iterations; i++)
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

    // --------------------------------------------------
    // Terrain
    // --------------------------------------------------

    /// <summary>
    /// Returns the height and slope of a single givin point
    /// This defines f(x,z) of a point
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public static Vector2 TerrainMap(Vector2 p, TerrainSettings settings)
    {
        float e;
        Vector2 pScaled = p * settings.frequencyScale;

        // If domain warping should be used
        if (settings.useDomainWarping)
        {
            // Applying a domain warp before sampeling the noise
            Vector2 warped = DomainWarp(pScaled);

            e = generateFBM(warped / 2000.0f + new Vector2(1.0f, -2.0f), 6);
        }
        else
        {
            e = generateFBM(pScaled / 2000.0f + new Vector2(1.0f, -2.0f), 6);
        }


        float a = 1.0f - Mathf.SmoothStep(0.12f, 0.13f, Mathf.Abs(e + 0.12f));

        e = 600.0f * e + 600.0f;

        // cliff shaping
        e += 90.0f * Mathf.SmoothStep(552.0f, 594.0f, e);

        return new Vector2(e, a);
    }

    /// <summary>
    /// Not used at the moment but can give back a derrivative along with the height info
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public static Vector4 TerrainMapD(Vector2 p)
    {
        Vector3 e = generateFBMWithD(p / 2000.0f + new Vector2(1.0f, -2.0f), 9);

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

    /// <summary>
    /// Applys a warp to the position vector
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private static Vector2 DomainWarp(Vector2 pos)
    {
        Vector2 warpSettings = FindAnyObjectByType<TerrainSettings>()
            .GetDomainWarpSettings();
        float warpScale = warpSettings.y;
        float warpStrength = warpSettings.x;

        // First warp layer
        Vector2 warp1 = new Vector2(
            generateFBM(pos * warpScale + new Vector2(5.2f, 1.3f), 4),
            generateFBM(pos * warpScale + new Vector2(9.1f, 7.4f), 4)
        );

        Vector2 p2 = pos + warp1 * warpStrength;

        // Second warp layer (smaller, sharper detail)
        Vector2 warp2 = new Vector2(
            generateFBM(p2 * (warpScale * 2.0f) + new Vector2(3.1f, 4.7f), 3),
            generateFBM(p2 * (warpScale * 2.0f) + new Vector2(8.3f, 2.8f), 3)
        );

        return p2 + warp2 * (warpStrength * 0.5f);
    }

    #endregion

    #region Mesh gen
    /// <summary>
    /// Converts a heightmap into a mesh
    /// </summary>
    /// <param name="heightMap"></param>
    /// <returns></returns>
    public Mesh GenerateTerrainMesh(Vector2[,] heightMap, float minHeight, float maxHeight, TerrainSettings settings)
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

                float rawHeight = heightMap[x, z].x;

                // Attempting to check the mesh for sharp changes in height to ge trid of noise / cliff artifacts
                float sum = 0f;
                int count = 0;

                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0) continue;

                        int nx = x + dx;
                        int nz = z + dz;

                        if (nx >= 0 && nx < width && nz >= 0 && nz < height)
                        {
                            sum += heightMap[nx, nz].x;
                            count++;
                        }
                    }
                }

                float avg = sum / count;
                float curvature = Mathf.Abs(rawHeight - avg);

                if (curvature > settings.spikeCurvatureThreshold)
                {
                    rawHeight = Mathf.Lerp(rawHeight, avg, 0.8f);
                }

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
    #endregion

}