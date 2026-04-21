using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// This class is a place to hold all the major terrain setting controls. This allows for faster modification and terrain tweaking 
/// </summary>
public class TerrainSettings : MonoBehaviour
{
    [Header("Dimensions and seed")]
    public int size = 64;
    public float scale = 10f;
    public Vector2 seedOffset;
    public int seed = 42;

    [Header("fBm Settings")]
    // Higher persistence leads to more frequency contribution (more detail)
    [Range(0f, 1f)] public float persistence = 0.6f;

    [Header("Fractal Settings")]
    public int maxOctaves = 10;
    public float lacunarity = 2;

    // Controls which octaves are used 
    public List<int> activeOctaves = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9};

    [Header("Height Modifiers")]
    public float heightMultiplier = 100f;
    public AnimationCurve heightCurve;

    [Header("Domain Warping")]
    public bool useDomainWarping = true;
    public float warpStrength = 10f;

    [Header("Cliff Settings")]
    public float cliffStart = 0.7f;
    public float cliffEnd = 0.8f;
    public float cliffStrength = 50f;

}
