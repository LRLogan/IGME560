using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering;
using System.Linq;

public class TerrainGeneration : MonoBehaviour
{
    public int RandomSeed;
    public int Width; 
    public int Depth;
    public int MaxHeight;
    public Material TerrainMaterial;
    public float Frequency = 1.0f;
    public float Amplitude = 0.5f;
    public float Lacunarity = 2.0f;
    public float Gain = 0.5f;
    public int Octaves = 8;
    public float Scale = 0.01f;
    public float NormalizeBias = 1.0f;

    private GameObject mRealTerrain;
    private NoiseAlgorithm mTerrainNoise;
    private GameObject mLight;

    // Texture atlas settings
    private Texture2D atlas;
    private int atlasSize = 2;
    private float grassHeight = 0.42f;
    private float snowHeight = 0.52f;
    private float iceHeight = 0.54f;


    // code to get rid of fog from: https://forum.unity.com/threads/how-do-i-turn-off-fog-on-a-specific-camera-using-urp.1373826/
    // Unity calls this method automatically when it enables this component
    private void OnEnable()
    {
        // Add WriteLogMessage as a delegate of the RenderPipelineManager.beginCameraRendering event
        RenderPipelineManager.beginCameraRendering += BeginRender;
        RenderPipelineManager.endCameraRendering += EndRender;
    }
 
    // Unity calls this method automatically when it disables this component
    private void OnDisable()
    {
        // Remove WriteLogMessage as a delegate of the  RenderPipelineManager.beginCameraRendering event
        RenderPipelineManager.beginCameraRendering -= BeginRender;
        RenderPipelineManager.endCameraRendering -= EndRender;
    }
 
    // When this method is a delegate of RenderPipeline.beginCameraRendering event, Unity calls this method every time it raises the beginCameraRendering event
    void BeginRender(ScriptableRenderContext context, Camera camera)
    {
        if(camera.name == "Main Camera No Fog")
        {
            //Debug.Log("Turn fog off");
            RenderSettings.fog = false;
        }
         
    }
 
    void EndRender(ScriptableRenderContext context, Camera camera)
    {
        if (camera.name == "Main Camera No Fog")
        {
            //Debug.Log("Turn fog on");
            RenderSettings.fog = true;
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
		// since we're drawing quads we need an extra dimension of variables to account for
		// the end of the quad square. You can't make a quad if x or y are 1 otherwise and
		// you will always draw one less than the size put in for width and depth
		Width = Width + 1;
		Depth = Depth + 1;
        // create a height map using perlin noise and fractal brownian motion
        mTerrainNoise = new NoiseAlgorithm();
        mTerrainNoise.InitializeNoise(Width, Depth, RandomSeed);
        mTerrainNoise.InitializePerlinNoise(Frequency, Amplitude, Octaves, 
            Lacunarity, Gain, Scale, NormalizeBias);
        NativeArray<float> terrainHeightMap = new NativeArray<float>((Width) * (Depth), Allocator.Persistent);
        mTerrainNoise.setNoise(terrainHeightMap, 0, 0);
        
        // create the mesh and set it to the terrain variable
        mRealTerrain = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mRealTerrain.transform.position = new Vector3(0, 0, 0);
        MeshRenderer meshRenderer = mRealTerrain.GetComponent<MeshRenderer>();
        MeshFilter meshFilter = mRealTerrain.GetComponent<MeshFilter>();
        meshRenderer.material = TerrainMaterial;

        Material chosenMap = null;
        meshFilter.mesh = GenerateTerrainMesh(terrainHeightMap);
        terrainHeightMap.Dispose();
        NoiseAlgorithm.OnExit();
    }

    private void Update()
    {
      	// you can change where the code happens to recreate things from start to update
		// remember to get rid of the terrain height map and recreate if the size changes or you will have
		// a memory leak
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
                    float y = heightMap[(x) * (depth) + (z)] * height - (MaxHeight/2.0f);
                    float useAltXPlusY = heightMap[(x + 1) * (depth) + (z)] * height - (MaxHeight/2.0f);
                    float useAltZPlusY = heightMap[(x) * (depth) + (z + 1)] * height- (MaxHeight/2.0f);
                    float useAltXAndZPlusY = heightMap[(x + 1) * (depth) + (z + 1)] * height- (MaxHeight/2.0f);
                    float normalizedY = heightMap[(x) * depth + (z)]; // just the height from map

                    vert.Add(new float3(x, y, z));
                    vert.Add(new float3(x, useAltZPlusY, z + 1)); 
                    vert.Add(new float3(x + 1, useAltXPlusY, z));  
                    vert.Add(new float3(x + 1, useAltXAndZPlusY, z + 1));

                    //Debug.Log($"ny: {normalizedY}");
                    // add uv's for texture chosen by heightmap
                    // The coordinates for the textures are hard-coded
                    if(normalizedY >= iceHeight)
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

    /// <summary>
    /// Takes a quad from the terrain and maps it to the part of the atlas
    /// </summary>
    /// <param name="uvs">the list of uvs</param>
    /// <param name="tileX">desired texture column</param>
    /// <param name="tileY">desired texture row</param>
    private void AddAtlasUVs(List<Vector2> uvs, int tileX, int tileY)
    {
        // Finding the coordinate of the texture needed on the atlas instead of using the entire texture
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


}
