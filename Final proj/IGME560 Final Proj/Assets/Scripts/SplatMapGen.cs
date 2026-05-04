using UnityEngine;

public class SplatMapGen
{
    public Texture2D GenerateSplatMap(
        BiomeMapGen biome,
        TerrainSettings settings
    )
    {
        int width = biome.grassMap.GetLength(0);
        int height = biome.grassMap.GetLength(1);
        //int scale = 4;

        Texture2D splatMap = new Texture2D(width, height, TextureFormat.RGBA32, false);
        splatMap.wrapMode = TextureWrapMode.Clamp;
        splatMap.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float grass = biome.grassMap[x, z];
                float rock = biome.rockMap[x, z];
                float dirt = biome.dirtMap[x, z];
                float sand = biome.sandMap[x, z];

                Color color = new Color(grass, rock, dirt, sand);

                splatMap.SetPixel(x, z, color);
            }
        }

        splatMap.wrapMode = TextureWrapMode.Clamp;
        splatMap.filterMode= FilterMode.Point;
        splatMap.Apply(false, false);
        return splatMap;
    }
}