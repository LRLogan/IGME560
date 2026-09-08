using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering;
using System.Linq;

/// <summary>
/// Revamped from CreateVoronoi
/// </summary>
public class CloudGen : MonoBehaviour
{
    // used to demonstrate how to use debug drawline
    public bool DrawDebug = true;

    // random number seed
    public int RandomSeed;

    // number of regions we want
    public int RegionNumber;

    // size of region map
    public int Width;
    public int Height;

    // image for us to store the regions to
    public RawImage RegionImage;

    // internal info
    List<Color> mColors = new List<Color>();
    List<Vector2Int> mRegions = new List<Vector2Int>();
    private Texture2D mTexture;


    // Heightmap gen
    private NativeArray<float> heightMap;
    private NoiseAlgorithm mTerrainNoise;
    public int MaxHeight;
    public int Depth;
    public float Frequency = 1.0f;
    public float Amplitude = 0.5f;
    public float Lacunarity = 2.0f;
    public float Gain = 0.5f;
    public int Octaves = 8;
    public float Scale = 0.01f;
    public float NormalizeBias = 1.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize our random seed
        Random.InitState(RandomSeed);

        // Heightmap gen
        // create a height map using perlin noise and fractal brownian motion
        mTerrainNoise = new NoiseAlgorithm();
        mTerrainNoise.InitializeNoise(Width, Height, RandomSeed);
        mTerrainNoise.InitializePerlinNoise(Frequency, Amplitude, Octaves,
            Lacunarity, Gain, Scale, NormalizeBias);
        heightMap = new NativeArray<float>((Width) * (Height), Allocator.Persistent);
        mTerrainNoise.setNoise(heightMap, 0, 0);

        // Get min and max height 
        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;
        for (int i = 0; i < heightMap.Length; i++)
        {
            if(heightMap[i] > maxHeight) maxHeight = heightMap[i];
            if(heightMap[i] < minHeight) minHeight = heightMap[i];
        }

        // normalize between 0 and 1
        for (int i = 0; i < heightMap.Length; i++)
        {
            heightMap[i] = (heightMap[i] - minHeight) /
            (maxHeight - minHeight);
        }

        mTexture = new Texture2D(Width, Height);
        int index = 0;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                float colorAmount = heightMap[index];
                Color sky = Color.blue * colorAmount + Color.white *
                (1.0f - colorAmount);
                mTexture.SetPixel(x, y, sky);
                index++;
            }
        }


        mTexture.alphaIsTransparency = true;
        mTexture.Apply();
        RegionImage.texture = mTexture;

    }



    // Update is called once per frame
    void Update()
    {

    }
}
