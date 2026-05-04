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
    public float frequencyScale = 10f;
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
    public float spikeCurvatureThreshold = 50;
    public float slopeScale = 2.0f;

    [Header("Domain Warping")]
    public bool useDomainWarping = true;
    public float warpStrength = 10f;
    public float warpScale = 1f;

    [Header("Cliff Settings")]
    public float cliffStart = 0.7f;
    public float cliffEnd = 0.8f;
    public float cliffStrength = 50f;

    [Header("Tree Placement")]
    public float treeNoiseFrequency = 0.1f;
    public int treeDensityMod = 5;

    public float maxTreeSlope = 0.4f;
    public float minTreeHeight = 550f;
    public float maxTreeHeight = 750f;

    public float minNormalY = 0.7f;
    public int treeSpacingRadius = 2;

    // This value squared will be the grid size of the tree overlay noise
    public int treeNoiseTerrainRatio = 4;

    [Header("Tree settings")]
    public float branchLength = 0.5f;
    public float baseBranchRadius = 0.2f;
    public float radiusFalloff = 0.7f;
    public Material branchMaterial;
    public int lSysIterations = 3;
    public float leafJitter = 0.3f;
    public float leafMinDistance = 1.5f;

    [Header("Texturing settings")]
    public float biomeNoiseScale = 0.05f;
    public float rockSlopeThreshold = 0.6f;
    public float lowAltitudeThreshold = 0.3f;
    public float highAltitudeThreshold = 0.25f;
    public float minGrassHeight = 0.1f;
    public float maxGrassHeight = 5f;
    public float sandHeightThreshold = 0.15f;

    [Header("Terrain Textures in order")]
    public Texture2D[] terrainTextures;
    
    public Vector2 GetDomainWarpSettings() => new Vector2(warpStrength, warpScale);
}
