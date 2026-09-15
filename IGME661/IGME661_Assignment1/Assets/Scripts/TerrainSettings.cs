using UnityEngine;

/// <summary>
/// Container for the various terrain settings
/// </summary>
public class TerrainSettings : MonoBehaviour
{
    [Header("Voronoi and world")] 
    public int voronoiRandSeed = 1;
    public int regionCount = 25;
    public int worldWidth = 100;
    public int worldDepth = 100;

    [Header("Terrain")]
    public int MaxHeight;
    public Material TerrainMaterial;
    public float Frequency = 1.0f;
    public float Amplitude = 0.5f;
    public float Lacunarity = 2.0f;
    public float Gain = 0.5f;
    public int Octaves = 8;
    public float Scale = 0.01f;
    public float NormalizeBias = 1.0f;

    [Header("Texyure atlas")]
    private Texture2D atlas;
    private int atlasSize = 2;
    private float grassHeight = 0.42f;
    private float snowHeight = 0.52f;
    private float iceHeight = 0.54f;
}
