using UnityEngine;
/// <summary>
/// This class is a place to hold all the major terrain setting controls. This allows for faster modification and terrain tweaking 
/// </summary>
[CreateAssetMenu(fileName = "TerrainSettings", menuName = "Scriptable Objects/TerrainSettings")]
public class TerrainSettings : ScriptableObject
{
    [Header("Dimensions")]
    public int width = 256;
    public int height = 256;
    public float scale = 50f;

    [Header("fBm Settings")]
    public int octaves = 5;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Height Modifiers")]
    public float heightMultiplier = 10f;
    public AnimationCurve heightCurve;

    [Header("Domain Warping")]
    public bool useDomainWarping = false;
    public float warpStrength = 10f;

    [Header("Seed")]
    public int seed = 42;

    /*
     * Resources:
     * - Sebastian Lague (YouTube): Procedural Landmass Generation
     * - The Book of Shaders (Noise + fBm): https://thebookofshaders.com/
     */
}
