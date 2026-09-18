using UnityEngine;

/// <summary>
/// Container for the various terrain generation settings.
/// </summary>
public class TerrainSettings : MonoBehaviour
{
    [Header("World / Generation")]
    public int voronoiRandSeed = 1234;
    public int regionCount = 25;

    [Header("Island Size")]
    public int worldWidth = 500;
    public int worldDepth = 500;
    public float maxHeight = 100f;

    [Header("Noise")]
    public float frequency = 1.0f;
    public float amplitude = 0.5f;
    public float lacunarity = 2.0f;
    public float gain = 0.5f;
    public int octaves = 5;
    public float scale = 0.1f;
    public float normalizeBias = 1.0f;
    public int perlinSeed = 1234;
    public float perlinMaskScale = 0.5f;

    [Header("Island Shape")]
    [Range(0f, 1f)]
    public float seaLevel = 0.05f;

    [Range(0.1f, 1f)]
    public float islandMaxRadius = 0.8f;

    [Header("Heightmap Resolution")]
    public int width = 129;
    public int depth = 129;

    [Header("Texture Atlas")]
    public Texture2D atlas;
    public int atlasSize = 2;

    public float grassHeight = 0.42f;
    public float snowHeight = 0.52f;
    public float iceHeight = 0.54f;

    [Header("Rendering")]
    public Material terrainMaterial;
}