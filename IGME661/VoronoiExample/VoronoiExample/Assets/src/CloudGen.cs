using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generates a procedural terrain heightmap using NoiseAlgorithm
/// and displays the resulting heightmap in a UI RawImage.
///
/// Current purpose:
/// - Generate a 2D noise-based heightmap
/// - Normalize the values to the range 0-1
/// - Convert each height value into a texture pixel
/// - Display the texture for debugging
///
/// The heightmap itself is stored as a NativeArray<float>, where
/// each element represents the height of one terrain point.
///
/// Array indexing:
///     index = y * Width + x
/// </summary>
public class CloudGen : MonoBehaviour
{
    [Header("Debug Display")]

    // When enabled, the generated heightmap is displayed in RegionImage.
    [SerializeField] private bool drawDebug = true;

    // UI RawImage used to display the generated heightmap.
    [SerializeField] private RawImage regionImage;

    [Header("Heightmap Resolution")]

    // Number of pixels/height samples along the X axis.
    // 512 is a reasonable starting point for testing.
    [SerializeField] private int width = 512;

    // Number of pixels/height samples along the Z/Y map axis.
    // This is the vertical dimension of the heightmap image.
    [SerializeField] private int height = 512;

    [Header("Randomization")]

    // Same seed produces the same terrain.
    [SerializeField] private int randomSeed = 12345;

    [Header("Terrain Height")]

    // Maximum intended world-space terrain height used when normalizing
    [SerializeField] private int maxHeight = 100;

    [Header("Perlin / FBM Noise")]

    // Base frequency of the noise
    [SerializeField] private float frequency = 1.0f;

    // Strength of the first noise octave.
    [SerializeField] private float amplitude = 0.5f;

    // Controls how much frequency increases between octaves.
    [SerializeField] private float lacunarity = 2.0f;

    // Controls how much amplitude decreases between octaves.
    [SerializeField] private float gain = 0.5f;

    [SerializeField] [Range(1, 12)] private int octaves = 8;
    [SerializeField] private float scale = 0.01f;

    // Bias used by NoiseAlgorithm during normalization.
    // 1.0 means no additional bias is being intentionally applied.
    [SerializeField] private float normalizeBias = 1.0f;

    [SerializeField][Range(0, 1)] private float cloudThreshold = 0.5f;
    [SerializeField][Range(1, 4)] private float sharpness = 2.33f;

    // Internal noise gen values
    private NativeArray<float> heightMap;
    private NoiseAlgorithm terrainNoise;
    private Texture2D heightMapTexture;
    private Texture2D skyTexture;

    private void Start()
    {
        GenerateHeightMap();
        CreateHeightMapTexture();

        if (drawDebug)
        {
            DisplayHeightMap();
        }
    }


    /// <summary>
    /// Entry point for generating the normalized heightmap.
    /// </summary>
    private void GenerateHeightMap()
    {
        // Validate dimensions before allocating memory.
        if (width <= 0 || height <= 0)
        {
            Debug.LogError(
                $"Invalid heightmap dimensions: {width}x{height}. " +
                "Width and Height must both be greater than zero."
            );

            return;
        }

        terrainNoise = new NoiseAlgorithm();

        terrainNoise.InitializeNoise(
            width,
            height,
            randomSeed
        );

        terrainNoise.InitializePerlinNoise(
            frequency,
            amplitude,
            octaves,
            lacunarity,
            gain,
            scale,
            normalizeBias
        );


        // --------------------------------------------------------
        // Allocate the heightmap.
        //
        // Width * Height gives us one float for every terrain
        // sample/pixel.
        //
        // Allocator.Persistent is appropriate if the NativeArray
        // needs to remain alive beyond the current method.
        // We therefore MUST dispose it later in OnDestroy().
        // --------------------------------------------------------

        heightMap = new NativeArray<float>(
            width * height,
            Allocator.Persistent
        );

        terrainNoise.setNoise(
            heightMap,
            0,
            0
        );

        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;
        for (int i = 0; i < heightMap.Length; i++)
        {
            if (heightMap[i] < minHeight) minHeight = heightMap[i];
            if (heightMap[i] > maxHeight) maxHeight = heightMap[i];
        }

        float heightRange = maxHeight - minHeight;

        if (heightRange <= Mathf.Epsilon)
        {
            Debug.LogWarning(
                "Heightmap contains no height variation. " +
                "All height values are identical."
            );

            // Since every value is the same, there is no meaningful
            // relative height. Set everything to zero.
            for (int i = 0; i < heightMap.Length; i++)
            {
                heightMap[i] = 0.0f;
            }

            return;
        }

        // Normalizing values in height map
        for (int i = 0; i < heightMap.Length; i++)
        {
            heightMap[i] =
                (heightMap[i] - minHeight) /
                heightRange;
        }
    }


    /// <summary>
    /// Converts the normalized heightmap into a Texture2D.
    /// </summary>
    private void CreateHeightMapTexture()
    {
        heightMapTexture = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            false
        );
        heightMapTexture.filterMode = FilterMode.Point;
        heightMapTexture.wrapMode = TextureWrapMode.Clamp;

        skyTexture = new Texture2D(width, height);

        // Main double loop from before to convert pixels
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float normalizedHeight = heightMap[index];

                float alpha = Mathf.Max(0, heightMap[index] - cloudThreshold) /
                    (1.0f - cloudThreshold);
                //Color thresholdGray = new Color(1, 1, 1, alpha);

                float withAlpha = Mathf.Pow(alpha, sharpness);
                Color cloudColor = new Color(1, 1, 1, withAlpha);

                /* Dual color transition (no alpha)
                Color pixel = Color.Lerp(
                    Color.white,
                    Color.blue,
                    normalizedHeight
                );
                */

                // setting the background colors
                float verticalPosition = y / (float)(height - 1);
                skyTexture.SetPixel(y, x, Color.magenta);
                Color skyColor = Color.Lerp(
                    new Color(1.0f, 0.5f, 0.0f),   // Orange
                    Color.blue,                     
                    verticalPosition
                );

                // Blend the clouds over the sky.
                Color finalColor = Color.Lerp(
                    skyColor,
                    cloudColor,
                    withAlpha
                );
                heightMapTexture.SetPixel(
                    x,
                    y,
                    finalColor /*thresholdGray*/ /*pixel*/
                );

            }
        }

        // Upload the CPU-side texture data to the GPU.
        heightMapTexture.Apply();
    }


    /// <summary>
    /// Displays the generated heightmap in the UI.
    /// </summary>
    private void DisplayHeightMap()
    {
        if (regionImage == null)
        {
            Debug.LogWarning(
                "RegionImage is not assigned. " +
                "The heightmap was generated, but cannot be displayed."
            );

            return;
        }

        regionImage.texture = heightMapTexture;
    }

    /// <summary>
    /// Cleanup
    /// </summary>
    private void OnDestroy()
    {
        // --------------------------------------------------------
        // NativeArray memory must be explicitly released.
        //
        // Without this, every time the object is destroyed/recreated
        // you risk leaking native memory.
        // --------------------------------------------------------

        if (heightMap.IsCreated)
        {
            heightMap.Dispose();
        }

        //The texture2D obj can also be destroyed
        if (heightMapTexture != null)
        {
            Destroy(heightMapTexture);
        }
    }
}