using UnityEngine;

/// <summary>
/// Container for the various terrain generation settings.
/// </summary>
public class TerrainSettings : MonoBehaviour
{
    [Header("World / Generation")]
    public int voronoiRandSeed = 1234;
    public int regionCount = 25;
    public float seaLevel = 0.05f;
    [Tooltip("Sea level is where the islands go down to but use this to raise the water above the bottom")]
    public float waterHeightMod = 10.0f;


    [Header("Heightmap Resolution")]
    public int width = 129;
    public int depth = 129;

    [Header("Noise")]
    public float frequency = 1.0f;
    public float amplitude = 0.5f;
    public float lacunarity = 2.0f;
    public float gain = 0.5f;
    public int octaves = 5;
    public float scale = 0.1f;
    public float normalizeBias = 1.0f;
    public int perlinSeed = 1234;


    [Header("Island Size")]
    public int worldWidth = 500;
    public int worldDepth = 500;
    public float maxHeight = 100f;

    [Range(0.1f, 1f)]
    public float islandMaxRadius = 0.8f;
    public float shapeScaleX = 1.2f;
    public float shapeScaleZ = 1.0f;
    [Range(0f, 0.5f)]
    public float shapeNoiseStrength = 1.0f;
    [Tooltip("How many large noise features exist across the island map.")]
    public float perlinMaskScale = 2.5f;
    [Range(0f, 0.25f)]
    [Tooltip("Softens the threshold boundary.")]
    public float perlinShapeSoftness = 0.05f;
    [Range(0f, 1f)]
    [Tooltip("Only noise values above this become land.")]
    public float perlinShapeThreshold = 0.55f;


    [Header("Island Height")]
    public float perlinHeightScale = 1.25f;

    [Range(0f, 1f)]
    public float heightMaskStrength = 0.25f;

    public float underwaterDepth = 0.01f;

    [Header("Rendering and prefabs")]
    public Material terrainMaterial;
    public Material waterMaterial;
    public Mesh waterMesh;
    public float sandHeight = 0.15f;
    public int sandDistance = 5;
    public float grass2Height = 0.6f;
    public float rockSlope = 0.65f;

    public GameObject treePrefab;
    public GameObject rockPrefab;
    public GameObject grassPrefab;
    
    public float treeSpawnChance = 0.03f;
    public float rockSpawnChance = 0.03f;
    public float grassSpawnChance = 0.75f;
}