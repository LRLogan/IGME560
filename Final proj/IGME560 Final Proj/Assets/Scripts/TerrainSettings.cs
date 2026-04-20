using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// This class is a place to hold all the major terrain setting controls. This allows for faster modification and terrain tweaking 
/// </summary>
public class TerrainSettings : MonoBehaviour
{
    [Header("Dimensions")]
    public int width = 256;
    public int height = 256;
    public float scale = 50f;

    [Header("fBm Settings")]
    public int octaves = 5;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Fractal Settings")]
    public int maxOctaves = 8;

    // Controls which octaves are used 
    public List<int> activeOctaves = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7 };

    // Domain/Range scaling
    public float heightScale = 100;
    public float heightOffset = 0;

    [Header("Height Modifiers")]
    public float heightMultiplier = 10f;
    public AnimationCurve heightCurve;
    public float additionalCliffHeight = 90;

    [Header("Domain Warping")]
    public bool useDomainWarping = true;
    public float warpStrength = 10f;

    [Header("Seed")]
    public int seed = 42;
}
