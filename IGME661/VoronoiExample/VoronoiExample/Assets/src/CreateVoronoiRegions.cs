using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CreateVoronoiRegions : MonoBehaviour
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
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize our random seed
        Random.InitState(RandomSeed);
       
        // come up with a bunch of random colors to use for regions
        for (int i = 0; i < RegionNumber; i++)
        {
            mColors.Add(new Color(Random.value, Random.value, Random.value));
        }
        
        // come up with a certain number of regions and give them random
        // center locations
        for (int i = 0; i < RegionNumber; i++)
        {
            mRegions.Add(new Vector2Int(Random.Range(0,Width), Random.Range(0,Height)));
        }

       
        mTexture = new Texture2D(Width, Height);
        for (int i = 0; i < Width; i++)
        {
            for (int j = 0; j < Height; j++)
            {
                int region = FindClosestRegion(new Vector2Int(i, j));
                Vector3 start = new Vector3(i, 0, j);
                Vector3 end = new Vector3(i - 1, 0, j);
                if (DrawDebug)
                {
                    Debug.DrawLine(start, end, mColors[region], 1000);
                }
                else
                {
                    mTexture.SetPixel(i, j, mColors[region]);
                }
            }
        }

        if (!DrawDebug)
        {
            mTexture.alphaIsTransparency = true;
            mTexture.Apply();
            RegionImage.texture = mTexture;
        }
        
    }

    // a brute force routine to find the closest region to a single point in our region
    public int FindClosestRegion(Vector2Int point)
    {
        int closest = 0;

        for (int i = 0; i < mRegions.Count; i++)
        {
            float distance1 = Math.Abs(Vector2.Distance(mRegions[closest],point));
            float distance2 = Math.Abs(Vector2.Distance(mRegions[i],point));
            if (distance2 < distance1)
            {
                closest = i;
            } 
        }
        
        return closest;
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
