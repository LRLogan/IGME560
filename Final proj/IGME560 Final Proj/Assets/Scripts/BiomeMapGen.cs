using System.IO;
using UnityEngine;

/// <summary>
/// Class designated to assigning biomes for (mainly for texturing in the current scope of this project)
/// </summary>
public class BiomeMapGen
{
    private TerrainPointData[,] heightMap;
    private TerrainSettings settings;

    public float[,] grassMap;
    public float[,] rockMap;
    public float[,] dirtMap;
    public float[,] sandMap;

    private float minHeight;
    private float maxHeight;


    public BiomeMapGen(TerrainPointData[,] heightMap, TerrainSettings settings)
    {
        this.heightMap = heightMap;
        this.settings = settings;

        int w = heightMap.GetLength(0);
        int h = heightMap.GetLength(1);

        grassMap = new float[w, h];
        rockMap = new float[w, h];
        dirtMap = new float[w, h];
        sandMap = new float[w, h];
    }

    /// <summary>
    /// Main generation function / entry point
    /// </summary>
    public void Generate()
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        ComputeHeightRange();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                EvaluateCell(x, z);
            }
        }
    }

    /// <summary>
    /// Evaluates a cell to determine its suitability / designation
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    private void EvaluateCell(int x, int z)
    {
        TerrainPointData p = heightMap[x, z];

        float height01 = (p.height - minHeight) / (maxHeight - minHeight);
        float slope = p.slope;

        float noise = Mathf.PerlinNoise(
            x * settings.biomeNoiseScale,
            z * settings.biomeNoiseScale
        );

        // -------------------------
        // RAW WEIGHTS
        // -------------------------

        float grass = (1f - slope) * Mathf.Lerp(0.8f, 1.2f, noise);

        float rock = 0f;
        if (slope > settings.rockSlopeThreshold)
            rock = (slope - settings.rockSlopeThreshold) * 2f;

        if (height01 > settings.highAltitudeThreshold)
            rock += height01 * 0.5f;

        float dirt = 0f;
        if (height01 < settings.lowAltitudeThreshold)
            dirt = (1f - height01);

        dirt += (1f - grass) * 0.2f;

        float sand = 0f;
        if (height01 < settings.sandHeightThreshold)
            sand = Mathf.InverseLerp(settings.sandHeightThreshold, 0f, height01);

        // -------------------------
        // SHAPE DISTRIBUTION (BEFORE NORMALIZATION)
        // -------------------------

        grass = Mathf.Pow(grass, 1.2f);
        rock = Mathf.Pow(rock, 1.4f);
        dirt = Mathf.Pow(dirt, 1.1f);
        sand = Mathf.Pow(sand, 1.3f);

        // -------------------------
        // NORMALIZE (IMPORTANT)
        // -------------------------

        float sum = grass + rock + dirt + sand;

        if (sum > 0.0001f)
        {
            grass /= sum;
            rock /= sum;
            dirt /= sum;
            sand /= sum;
        }
        else
        {
            grass = 1f; // fallback safe state
        }

        grassMap[x, z] = grass;
        rockMap[x, z] = rock;
        dirtMap[x, z] = dirt;
        sandMap[x, z] = sand;
    }

    private void ComputeHeightRange()
    {
        minHeight = float.MaxValue;
        maxHeight = float.MinValue;

        int w = heightMap.GetLength(0);
        int h = heightMap.GetLength(1);

        for (int x = 0; x < w; x++)
        {
            for (int z = 0; z < h; z++)
            {
                float hgt = heightMap[x, z].height;

                if (hgt < minHeight) minHeight = hgt;
                if (hgt > maxHeight) maxHeight = hgt;
            }
        }
    }
}
