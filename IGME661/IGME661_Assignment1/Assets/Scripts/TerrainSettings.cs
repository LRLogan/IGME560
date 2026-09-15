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
    public int maxHeight;
    public Material terrainMaterial;
    public float frequency = 1.0f;
    public float amplitude = 0.5f;
    public float lacunarity = 2.0f;
    public float gain = 0.5f;
    public int octaves = 8;
    public float scale = 0.01f;
    public float normalizeBias = 1.0f;

    [Header("Texyure atlas")]
    public Texture2D atlas;
    public int atlasSize = 2;
    public float grassHeight = 0.42f;
    public float snowHeight = 0.52f;
    public float iceHeight = 0.54f;
}
