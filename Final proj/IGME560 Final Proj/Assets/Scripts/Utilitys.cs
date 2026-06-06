using System.Globalization;
using System.IO;
using UnityEngine;

public static class Utilitys
{
    /// <summary>
    /// Loads a CSV file containing only numeric values
    /// and returns a 2D float array.
    /// </summary>
    /// <param name="filePath">
    /// Absolute path or Application.dataPath-relative path.
    /// </param>
    public static float[,] LoadCSVToFloatArray(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);

        if (lines.Length == 0)
        {
            Debug.LogError("CSV file is empty.");
            return null;
        }

        int height = lines.Length;
        int width = lines[0].Split(',').Length;

        float[,] data = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            string[] values = lines[y].Split(',');

            if (values.Length != width)
            {
                Debug.LogError(
                    $"Row {y} contains {values.Length} columns. Expected {width}."
                );
                return null;
            }

            for (int x = 0; x < width; x++)
            {
                if (!float.TryParse(
                    values[x],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float result))
                {
                    Debug.LogError(
                        $"Failed to parse value '{values[x]}' at ({x},{y})"
                    );
                    return null;
                }

                data[x, y] = result;
            }
        }

        return data;
    }


    /// <summary>
    /// Converts a grayscale image into a heightmap.
    /// Black = minHeight
    /// White = maxHeight
    /// </summary>
    /// <param name="heightmapImage">Source image</param>
    /// <param name="minHeight">Minimum terrain height</param>
    /// <param name="maxHeight">Maximum terrain height</param>
    /// <returns>float[,] heightmap</returns>
    public static float[,] TextureToHeightmap(
        Texture2D heightmapImage,
        float minHeight,
        float maxHeight)
    {
        int width = heightmapImage.width;
        int height = heightmapImage.height;

        float[,] heightmap = new float[width, height];

        Color[] pixels =
            heightmapImage.GetPixels();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel =
                    pixels[y * width + x];

                // Convert RGB to grayscale
                float grayscale =
                    pixel.grayscale;

                heightmap[x, y] =
                    Mathf.Lerp(
                        minHeight,
                        maxHeight,
                        grayscale
                    );
            }
        }

        return heightmap;
    }

    /// <summary>
    /// Can load a heightmap from an image by converting it to a texture
    /// </summary>
    /// <param name="path"></param>
    /// <param name="minHeight"></param>
    /// <param name="maxHeight"></param>
    /// <returns></returns>
    public static float[,] LoadHeightmapFromImage(
    string path,
    float minHeight,
    float maxHeight)
    {
        byte[] bytes =
            System.IO.File.ReadAllBytes(path);

        Texture2D texture =
            new Texture2D(2, 2);

        texture.LoadImage(bytes);

        return TextureToHeightmap(
            texture,
            minHeight,
            maxHeight
        );
    }
}
