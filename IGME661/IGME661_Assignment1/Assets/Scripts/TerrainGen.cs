using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering;
using System.Linq;
using Unity.Burst;

/* Plan:
 * Use some of the FbM code from demo while adding in some tidbits like customization and domain warping from 560 final proj
 * In terms of placing assets and texturing ground I can use both slope and biome data
 * In the end for this assignment I would like to have a nice island generator
 * Maybe even if the island size premits and there is an ideal location I can add a building 
 */

/// <summary>
/// Core pipeline for generating procedural terrain
/// </summary>
public class TerrainGen : MonoBehaviour
{
    [SerializeField] Material atlasMat;
    private TerrainSettings terrainSettings;
    private List<VoronoiRegion> regions;
    private List<Vector2> islands;
    private NoiseAlgorithm terrainNoise;


    void Start()
    {
        // Field init
        UnityEngine.Random.InitState(terrainSettings.voronoiRandSeed);
        regions = new List<VoronoiRegion>(terrainSettings.regionCount);
        terrainNoise = new NoiseAlgorithm();


    }

    void Update()
    {
        
    }

    /// <summary>
    /// Main entry point for full terrain gen seq
    /// </summary>
    public void StartFullTerrainGen()
    {
        GenerateVoronoiRegions(terrainSettings.worldDepth,
            terrainSettings.worldWidth,
            terrainSettings.regionCount,
            terrainSettings.voronoiRandSeed);
    }

    private void GenerateVoronoiRegions(int depth, int width, int regionCount, 
        int seed)
    {
        // Init region center locations
        for (int i = 0; i < regionCount; i++)
        {
            
        }

    }

    /// <summary>
    /// Container step in the pipeline that when called 
    /// can spawn an island at a location
    /// </summary>
    /// <param name="location"></param>
    private void SpawnIsland(Vector3 location)
    {

    }
    /*
    /// <summary>
    /// Takes a quad from the terrain and maps it to the part of the atlas
    /// </summary>
    /// <param name="uvs">the list of uvs</param>
    /// <param name="tileX">desired texture column</param>
    /// <param name="tileY">desired texture row</param>
    private void AddAtlasUVs(List<Vector2> uvs, int tileX, int tileY)
    {
        // Finding the coordinate of the texture needed on the atlas
        // instead of using the entire texture
        float tileWidth = 1.0f / atlasSize;
        float tileHeight = 1.0f / atlasSize;

        float minX = tileX * tileWidth;
        float minY = tileY * tileHeight;

        float maxX = minX + tileWidth;
        float maxY = minY + tileHeight;

        uvs.Add(new Vector2(minX, minY));
        uvs.Add(new Vector2(minX, maxY));
        uvs.Add(new Vector2(maxX, minY));
        uvs.Add(new Vector2(maxX, maxY));
    }

    // create a new mesh with
    // perlin noise
    // makes a quad and connects it with the next quad
    // uses whatever texture the material is given
    public Mesh GenerateTerrainMesh(NativeArray<float> heightMap)
    {
        Debug.Log($"max {heightMap.Max()} min: {heightMap.Min()}");
        int width = Width, depth = Depth;
        int height = MaxHeight;
        int indicesIndex = 0;
        int vertexIndex = 0;
        int vertexMultiplier = 4; // create quads to fit uv's to so we can use more than one uv (4 vertices to a quad)

        Mesh terrainMesh = new Mesh();
        List<Vector3> vert = new List<Vector3>(width * depth * vertexMultiplier);
        List<int> indices = new List<int>(width * depth * 6);
        List<Vector2> uvs = new List<Vector2>(width * depth);
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                if (x < (width - 1) && z < (depth - 1))
                {
                    // note: since perlin goes up to 1.0 multiplying by a height will tend to set
                    // the average around maxheight/2. We remove most of that extra by subtracting maxheight/2
                    // so our ground isn't always way up in the air
                    float y = heightMap[(x) * (depth) + (z)] * height - (MaxHeight / 2.0f);
                    float useAltXPlusY = heightMap[(x + 1) * (depth) + (z)] * height - (MaxHeight / 2.0f);
                    float useAltZPlusY = heightMap[(x) * (depth) + (z + 1)] * height - (MaxHeight / 2.0f);
                    float useAltXAndZPlusY = heightMap[(x + 1) * (depth) + (z + 1)] * height - (MaxHeight / 2.0f);
                    float normalizedY = heightMap[(x) * depth + (z)]; // just the height from map

                    vert.Add(new float3(x, y, z));
                    vert.Add(new float3(x, useAltZPlusY, z + 1));
                    vert.Add(new float3(x + 1, useAltXPlusY, z));
                    vert.Add(new float3(x + 1, useAltXAndZPlusY, z + 1));

                    //Debug.Log($"ny: {normalizedY}");
                    // add uv's for texture chosen by heightmap
                    // The coordinates for the textures are hard-coded
                    if (normalizedY >= iceHeight)
                    {
                        AddAtlasUVs(uvs, 1, 0);
                    }
                    else if (normalizedY >= snowHeight)
                    {
                        AddAtlasUVs(uvs, 0, 1);
                    }
                    else if (normalizedY >= grassHeight)
                    {
                        AddAtlasUVs(uvs, 0, 0);
                    }
                    else
                    {
                        AddAtlasUVs(uvs, 1, 1);
                    }

                    // front or top face indices for a quad
                    //0,2,1,0,3,2
                    indices.Add(vertexIndex);
                    indices.Add(vertexIndex + 1);
                    indices.Add(vertexIndex + 2);
                    indices.Add(vertexIndex + 3);
                    indices.Add(vertexIndex + 2);
                    indices.Add(vertexIndex + 1);
                    indicesIndex += 6;
                    vertexIndex += vertexMultiplier;
                }
            }

        }

        // set the terrain var's for the mesh
        terrainMesh.vertices = vert.ToArray();
        terrainMesh.triangles = indices.ToArray();
        terrainMesh.SetUVs(0, uvs);

        // reset the mesh
        terrainMesh.RecalculateNormals();
        terrainMesh.RecalculateBounds();

        return terrainMesh;
    }
    */

}