using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    // editor changeable var's
    public int RandomSeed;
    public byte Width;
    public byte Depth;
    public int CharactersToSpawn = 2;
    public int ItemsToSpawn = 5;
    public Material TerrainMaterial;
    public SimCharManager characterManager;
    public Material ObstacleMaterial;
    public int NumberOfObstacles = 20;
    public GameObject itemPrefab;
    public GameObject hivePrefab;
    
    // internally created vars
    private MapData[] mMap;
    private byte mHeight = 0;
    private float mOffset = 0.5f;
    private GameObject mTerrainObject;
    private bool mIsInitialized;
    public List<GameObject> Obstacles = new List<GameObject>(10);
    public List<GameObject> Items = new List<GameObject>(10);

    // Upgrade to simulation vars
    private GameObject hive;

    
    // Set up the map and obstacles for the AStar example.
    void Start()
    {
        Random.InitState(RandomSeed);
        mIsInitialized = false;
        mMap = new MapData[(Width+1) * (Depth + 1)];
        CreateMap();
        AddObstacles(NumberOfObstacles);

        // Adding the items and hive
        for (int i = 0; i < ItemsToSpawn; i++)
        {
            Items.Add(AddItem(itemPrefab));
        }
        hive = AddItem(hivePrefab);

        characterManager.gameMan = this;
        characterManager.Items = Items;
        characterManager.SpawnRandomCharacters(CharactersToSpawn, 0, Width,
            0, Depth, mMap);
    }
    
    // Create the map data
    public void CreateMap()
    {
        // create the mesh and set it to the terrain variable
        mTerrainObject =  GameObject.CreatePrimitive(PrimitiveType.Cube);
        mTerrainObject.transform.position = new Vector3(0, 0, 0);
        MeshRenderer meshRenderer = mTerrainObject.GetComponent<MeshRenderer>();
        MeshFilter meshFilter = mTerrainObject.GetComponent<MeshFilter>();
        meshRenderer.material = TerrainMaterial;
        meshFilter.mesh = GenerateFlatTerrain(mHeight);
    }

    // Add obstacles to map locations
    public void AddObstacles(int count)
    {
        if (Obstacles != null)
        {
            Obstacles.Clear();
        }
        else
        {
            Obstacles = new List<GameObject>(count);
        }
        
        for (int i = 0; i < count; i++)
        {
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            MeshRenderer meshRenderer = tmp.GetComponent<MeshRenderer>();
            meshRenderer.material = ObstacleMaterial;
            int x = Random.Range(0, Width);
            int y = mHeight;
            int z = Random.Range(0, Depth);
            // the 0.5f is because the cube has its origin in the middle and we need to
            // move it up to be on the ground.
            tmp.transform.position = new Vector3(x,y+0.5f,z);
            Obstacles.Add(tmp);
            mMap[(int)tmp.transform.position.x * Width + (int)tmp.transform.position.z].type = Tiles.NOT_WALKABLE;
        }
    }

    /// <summary>
    /// A more generic form of AddObsticle but with option for a prefab.
    /// Also similar to SpawnRandomCharacters
    /// </summary>
    /// <param name="prefabObj"></param>
    public GameObject AddItem(GameObject prefabObj)
    {
        Vector3 randLoc = GetRandLoc();

        GameObject newObj = GameObject.Instantiate(prefabObj);
        newObj.transform.position = new Vector3(randLoc.x, randLoc.y + 0.5f, randLoc.z);
        newObj.SetActive(true);
        Debug.Log("Item of type " + prefabObj + " spawned");
        return newObj;
    }

    public Vector3 GetRandLoc()
    {
        bool done = false;
        // hardcode the number of times to try 
        // initializing the location for a spawn
        // so we're not stuck in infinity if somebody
        // fills the board up with items
        int tryInitializing = 100;
        byte x = (byte)Random.Range(0, Width);
        byte z = (byte)Random.Range(0, Depth);
        float y = mMap[x * (Width + 1) + z].height;
        while (!done && tryInitializing > 0)
        {
            if (mMap[x * Width + z].type == Tiles.NOT_WALKABLE)
            {
                // try again!
                x = (byte)Random.Range(0, Width);
                z = (byte)Random.Range(0, Depth);
            }
            else
            {
                done = true;
            }
            tryInitializing--;
            if (tryInitializing == 0)
            {
                Debug.Log("Spawning item " + " has failed. Stopping program.");
                return Vector3.zero;
            }
        }
        return new Vector3(x, y, z);
    }
    
    // Generate flat terrain that could carry a reasonable texture 
    public Mesh GenerateFlatTerrain(byte height)
    {
        int width = Width + 1, depth = Depth + 1;
        float y = height;
        byte yByte = height;
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
                mMap[(x) * (width) + (z)] = new MapData
                {
                    height = yByte,
                    type = Tiles.WALKABLE,
                    cost = 1,
                };

                if (x < width - 1 && z < depth - 1)
                {
                    // since most model origins are in the center of the model,
                    // in order to center things to look correct, the ground
                    // needs to be shifted by 0.5f
                    float realX = x - mOffset;
                    float realZ = z - mOffset;
                    vert.Add(new float3(realX, y, realZ));
                    vert.Add(new float3(realX, y, realZ + 1));
                    vert.Add(new float3(realX + 1, y, realZ));
                    vert.Add(new float3(realX + 1, y, realZ + 1));

                    // add uv's
                    // remember to give it all 4 sides of the image coords
                    uvs.Add(new Vector2(0.0f, 0.0f));
                    uvs.Add(new Vector2(0.0f, 1.0f));
                    uvs.Add(new Vector2(1.0f, 1.0f));
                    uvs.Add(new Vector2(1.0f, 0.0f));

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
}
