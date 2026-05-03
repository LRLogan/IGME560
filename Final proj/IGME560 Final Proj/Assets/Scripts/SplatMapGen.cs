using UnityEngine;

public class SplatMapGen
{
    public Texture2D GenerateSplatMap(
        TerrainPointData[,] heightMap,
        TerrainSettings settings,
        Texture2D[] terrainTextures // grass, rock, dirt...
    )
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Texture2D splatMap = new Texture2D(width, height);
        splatMap.wrapMode = TextureWrapMode.Clamp;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                TerrainPointData p = heightMap[x, z];

                float[] weights = CalculateWeights(p, settings);

                Color color = new Color(
                    weights[0], // grass
                    weights[1], // rock
                    weights[2], // dirt
                    weights[3]  // (sand, snow, etc.)
                );

                splatMap.SetPixel(x, z, color);
            }
        }

        splatMap.Apply();
        return splatMap;
    }

    /// <summary>
    /// Main biome logic to determine what texture goes where and how much of it
    /// </summary>
    /// <param name="p"></param>
    /// <param name="settings"></param>
    /// <returns></returns>
    private float[] CalculateWeights(TerrainPointData p, TerrainSettings settings)
    {
        float[] w = new float[4]; // grass, rock, dirt, sand

        // --- ROCK (steep slopes)
        float slopeFactor = Mathf.InverseLerp(
            settings.rockSlopeThreshold,
            1f,
            p.slope
        );
        w[1] = slopeFactor;

        // --- GRASS (flat + mid elevation)
        float flatness = 1f - p.slope;
        float heightFactor = Mathf.InverseLerp(
            settings.minGrassHeight,
            settings.maxGrassHeight,
            p.height
        );
        w[0] = flatness * heightFactor;

        // --- DIRT (transition areas)
        w[2] = Mathf.Clamp01(1f - w[0] - w[1]);

        // --- OPTIONAL: SAND (low elevation)
        if (p.height < settings.sandHeight)
            w[3] = 1f;

        // Normalize
        float total = w[0] + w[1] + w[2] + w[3];
        if (total > 0)
        {
            for (int i = 0; i < w.Length; i++)
                w[i] /= total;
        }

        return w;
    }
}
