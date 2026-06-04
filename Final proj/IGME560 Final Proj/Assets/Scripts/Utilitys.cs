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
}
