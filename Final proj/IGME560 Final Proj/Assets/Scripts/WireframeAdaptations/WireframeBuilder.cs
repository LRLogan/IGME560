using NUnit;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Unity.VisualScripting;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.UIElements;

public struct Edge
{
    public (int, int) startIndex;
    public (int, int) endIndex;

    public Edge((int, int) start, (int, int) end)
    {
        startIndex = start;
        endIndex = end;
    }
}

/// <summary>
/// Wireframe visualizer of a heightmap written by Logan Larrondo
/// </summary>
public class WireframeBuilder : MonoBehaviour
{
    // General vars
    [SerializeField] private TerrainSettings settings;
    [SerializeField] [Tooltip("1 for a 1:1")] private int pointToVertRatio;

    // Game object oriented pipeline
    private GameObject[,] vertHeightMap;
    [SerializeField] private GameObject sphere;
    [SerializeField] private GameObject cyl; 

    // Parent organize
    [SerializeField] private Transform vertParent;
    [SerializeField] private Transform edgeParent;
    [SerializeField] private Transform wireframeParent;

    // Optional camera vars
    [Header("Custom center settings. Disreguard if using other centerpoint")]
    [SerializeField] private float camOffsetZ = 75;
    [SerializeField] private GameObject cam;

    // Vertex Edge Buffer oriented pipeline
    private Vector3[,] vertexMap;
    private List<Edge> edges = new();
    private List<Matrix4x4> edgeMatrices = new();
    [SerializeField] private Material cylMat;
    [SerializeField] private Mesh cylMesh;

    // Presets 
    private float[,] mountainPreset;
    private float[,] hillsPreset;
    private float[,] canyonPreset;
    private float[,] craterPreset;

    // Animation
    [Header("Animation")]
    [SerializeField] private float transitionDuration = 5f;
    [SerializeField] private bool animate = true;
    [SerializeField] private AnimationCurve heightCurve;
    private int currentMapIndex = 0;
    private int nextMapIndex = 1;
    private float transitionTimer = 0f;
    private List<float[,]> maps;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        /*
         * Overall pipeline
         * Generate heightmap
         * build mesh by chosing key points and connecting them
         * build animation sequence for altering the height values
         * apply the animation sequence to the mesh
         * 
         * The reason I am building a sequence and some other seemingly hard coded values
         * is to have a fine control over the final animation as it will be exported
         */

        StartAnimationPipeline();
        //StartFullWFPipeline_VEB();
        //StartFullWFPipeline_GO();
    }

    void LateUpdate()
    {
        if (!animate)
            return;

        Animate();
    }

    /// <summary>
    /// Entry point for full pipeline
    /// </summary>
    public void StartFullWFPipeline_GO()
    {
        TerrainPointData[,] heightmap = TerrainGen.GenerateTerrainHeightMap(settings);
        BuildWireframeFromHeights_GO(heightmap);
        UseHMCenter(heightmap);
    }

    public void StartFullWFPipeline_VEB()
    {
        TerrainPointData[,] heightmap = TerrainGen.GenerateTerrainHeightMap(settings);
        BuildWireframeFromHeights_VEB(heightmap);
        UseHMCenter_VEB(heightmap);
    }

    public void StartAnimationPipeline()
    {
        LoadPresetCSVFiles();
        maps = new List<float[,]>()
        {
            mountainPreset,
            canyonPreset,
            hillsPreset,
            craterPreset
        };
        BuildWireframeFromHeights_VEB(maps[0]);
    }

    #region General Helpers
    public void UseHMCenter(TerrainPointData[,] heightmap)
    {
        // Center the display on the screen 
        wireframeParent.position = new Vector3(heightmap.GetLength(0), heightmap.GetLength(0), -heightmap.GetLength(1));
        cam.transform.position = new Vector3(heightmap.GetLength(0) * 1.5f, heightmap.GetLength(0) / 2, -heightmap.GetLength(1) - camOffsetZ);
    }

    public void UseHMCenter_VEB(TerrainPointData[,] heightmap)
    {
        /* Does not work (gibberish)
        Vector3 center = Vector3.zero;

        foreach (Vector3 v in vertexMap)
        {
            center += v;
        }

        center /= vertexMap.Length;

        for (int z = 0; z < vertexMap.GetLength(1); z++)
        {
            for (int x = 0; x < vertexMap.GetLength(0); x++)
            {
                vertexMap[x, z] -= center;
            }
        }

        cam.transform.position = new Vector3(heightmap.GetLength(0) * 1.5f, heightmap.GetLength(0) / 2, -heightmap.GetLength(1) - camOffsetZ);
        */
    }

    private void LoadPresetCSVFiles()
    {
        string path =
            Path.Combine(
                Application.dataPath,
                "PresetTerrainData/mountain_128x128.csv"
            );
        mountainPreset =
            Utilitys.LoadCSVToFloatArray(path);

        path =
            Path.Combine(
                Application.dataPath,
                "PresetTerrainData/valleyHeightmap1_128x128.png"
            );
        canyonPreset =
            Utilitys.LoadHeightmapFromImage(
                path,
                0f,
                40f
            );

        path =
            Path.Combine(
                Application.dataPath,
                "PresetTerrainData/mtnHeightmap2_128x128.png"
            );
        craterPreset =
            Utilitys.LoadHeightmapFromImage(
                path,
                0f,
                40f
            );

        path =
            Path.Combine(
                Application.dataPath,
                "PresetTerrainData/mtnHeightmap1_128x128.png"
            );
        hillsPreset =
            Utilitys.LoadHeightmapFromImage(
                path,
                0f,
                40f
            );
    }
    #endregion

    #region Game Object oriented pipeline

    /// <summary>
    /// Entry point for the specific generation of the mesh
    /// </summary>
    /// <param name="heightmap"></param>
    /// <returns></returns>
    public Transform BuildWireframeFromHeights_GO(TerrainPointData[,] heightmap)
    {
        // Basic height map set up
        int vertWidth = 
            Mathf.CeilToInt(
                heightmap.GetLength(0) / (float)pointToVertRatio
            );

        int vertHeight =
            Mathf.CeilToInt(
                heightmap.GetLength(1) / (float)pointToVertRatio
            );

        vertHeightMap = new GameObject[vertWidth, vertHeight];

        // ---------------------
        // Build verts
        // --------------------- 
        // I know I can do this mathimatically but the formula escapes me atm
        int r = 0, c = 0;

        // Loop over the height map at point ratio intervals 
        for (int z = 0; z < heightmap.GetLength(1); z += pointToVertRatio)
        {
            for (int x = 0; x < heightmap.GetLength(0); x += pointToVertRatio)
            {
                // Place each vert
                vertHeightMap[c, r] = Instantiate(
                    sphere,
                    new Vector3(x, heightmap[x, z].height, z),
                    Quaternion.identity,
                    vertParent
                );
                c++;
            }
            r++;
            c = 0;
        }

        // ---------------------
        // Build connections
        // ---------------------
        int width = vertHeightMap.GetLength(0);
        int height = vertHeightMap.GetLength(1);

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject current =
                    vertHeightMap[x, z].gameObject;

                if (x + 1 < width)
                {
                    ConnectPoints_GO(
                        current,
                        vertHeightMap[x + 1, z].gameObject
                    );
                }

                if (z + 1 < height)
                {
                    ConnectPoints_GO(
                        current,
                        vertHeightMap[x, z + 1].gameObject
                    );
                }

                if (x + 1 < width &&
                    z + 1 < height)
                {
                    ConnectPoints_GO(
                        current,
                        vertHeightMap[x + 1, z + 1].gameObject
                    );
                }
            }
        }
        return wireframeParent;
    }

    /// <summary>
    /// Helper to build connection points for the grid
    /// </summary>
    /// <param name="go1"></param>
    /// <param name="go2"></param>
    private void ConnectPoints_GO(GameObject go1, GameObject go2)
    {
        GameObject tempCyl = Instantiate(cyl, edgeParent);

        float halfDist =
            Vector3.Distance(
                go1.transform.position,
                go2.transform.position
            ) * 0.5f;

        Vector3 edgeDirection =
            (go2.transform.position - go1.transform.position).normalized;

        tempCyl.transform.up = edgeDirection;

        tempCyl.transform.localScale =
            new Vector3(0.2f, halfDist, 0.2f);

        tempCyl.transform.position =
            (go1.transform.position + go2.transform.position) * 0.5f;

    }
    #endregion

    #region Vertex Edge Buffer oriented pipeline

    /// <summary>
    /// Entry point for the specific generation of the mesh
    /// </summary>
    /// <param name="heightmap"></param>
    /// <returns></returns>
    public void BuildWireframeFromHeights_VEB(TerrainPointData[,] heightmap)
    {
        // Basic height map set up
        int vertWidth =
            Mathf.CeilToInt(
                heightmap.GetLength(0) / (float)pointToVertRatio
            );

        int vertHeight =
            Mathf.CeilToInt(
                heightmap.GetLength(1) / (float)pointToVertRatio
            );

        vertexMap = new Vector3[vertWidth, vertHeight];

        BuildVerts(heightmap);
        BuildEdges();
        BuildMatrices();

    }

    /// <summary>
    /// Overload to support float array
    /// </summary>
    /// <param name="heightmap"></param>
    public void BuildWireframeFromHeights_VEB(float[,] heightmap)
    {
        // Basic height map set up
        int vertWidth =
            Mathf.CeilToInt(
                heightmap.GetLength(0) / (float)pointToVertRatio
            );

        int vertHeight =
            Mathf.CeilToInt(
                heightmap.GetLength(1) / (float)pointToVertRatio
            );

        vertexMap = new Vector3[vertWidth, vertHeight];

        BuildVerts(heightmap);
        BuildEdges();
        BuildMatrices();
        RenderWireframe();
    }

    private void BuildVerts(TerrainPointData[,] heightmap)
    {
        // ---------------------
        // Build verts
        // --------------------- 
        // I know I can do this mathimatically but the formula escapes me atm
        int r = 0, c = 0;

        // Loop over the height map at point ratio intervals 
        for (int z = 0; z < heightmap.GetLength(1); z += pointToVertRatio)
        {
            for (int x = 0; x < heightmap.GetLength(0); x += pointToVertRatio)
            {
                // Place each vert
                vertexMap[c, r] = new Vector3(
                    x,
                    heightmap[x, z].height,
                    z
                );
                c++;
            }
            r++;
            c = 0;
        }
    }

    /// <summary>
    /// Overload to support float array
    /// </summary>
    /// <param name="heightmap"></param>
    private void BuildVerts(float[,] heightmap)
    {
        // ---------------------
        // Build verts
        // --------------------- 
        // I know I can do this mathimatically but the formula escapes me atm
        int r = 0, c = 0;

        // Loop over the height map at point ratio intervals 
        for (int z = 0; z < heightmap.GetLength(1); z += pointToVertRatio)
        {
            for (int x = 0; x < heightmap.GetLength(0); x += pointToVertRatio)
            {
                // Place each vert
                vertexMap[c, r] = new Vector3(
                    x,
                    heightmap[x, z],
                    z
                );
                c++;
            }
            r++;
            c = 0;
        }
    }

    private void BuildEdges()
    {
        // ---------------------
        // Build connections
        // ---------------------
        int width = vertexMap.GetLength(0);
        int height = vertexMap.GetLength(1);

        edges.Clear();

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 current =
                    vertexMap[x, z];

                if (x + 1 < width)
                {
                    edges.Add(new Edge(
                        (x, z),
                        (x + 1, z)
                    ));
                }

                if (z + 1 < height)
                {
                    edges.Add(new Edge(
                        (x, z),
                        (x, z + 1)
                    ));
                }

                if (x + 1 < width &&
                    z + 1 < height)
                {
                    edges.Add(new Edge(
                        (x, z),
                        (x + 1, z + 1)
                    ));
                }
            }
        }
    }

    private void BuildMatrices()
    {
        // ---------------------
        // Build edgeMatrices for rendering
        // ---------------------

        edgeMatrices.Clear();

        foreach (Edge e in edges)
        {
            Vector3 edge = 
                vertexMap[e.endIndex.Item1, e.endIndex.Item2] - 
                vertexMap[e.startIndex.Item1, e.startIndex.Item2];

            float distance = edge.magnitude;
            Vector3 midpoint =
                (vertexMap[e.startIndex.Item1, e.startIndex.Item2] + 
                vertexMap[e.endIndex.Item1, e.endIndex.Item2]) * 0.5f;

            // Getting the sesired forward vector stored in a quat
            Quaternion rot =
                Quaternion.FromToRotation(
                    Vector3.up,
                    edge.normalized
                );

            Matrix4x4 matrix =
                 Matrix4x4.TRS(
                     midpoint,
                     rot,
                     new Vector3(0.2f, distance / 2, 0.2f)  // Scale
                 );

            edgeMatrices.Add(matrix);
        }
    }

    private void RenderWireframe()
    {
        const int batchSize = 1023;

        Matrix4x4[] batch = new Matrix4x4[batchSize];

        for (int i = 0; i < edgeMatrices.Count; i += batchSize)
        {
            int count =
                Mathf.Min(
                    batchSize,
                    edgeMatrices.Count - i
                );

            for (int j = 0; j < count; j++)
            {
                batch[j] = edgeMatrices[i + j];
            }

            Graphics.DrawMeshInstanced(
                cylMesh,
                0,
                cylMat,
                batch,
                count
            );
        }
    }
    #endregion

    /// <summary>
    /// Used in an update method to animate the maps in the maps list
    /// </summary>
    private void Animate()
    {
        transitionTimer += Time.deltaTime;

        // Timestamp / progress in the transition
        float progress =
            transitionTimer /
            transitionDuration;
        progress = Mathf.Clamp01(progress);

        // progress curved to the height curve
        float curvedT =
            heightCurve.Evaluate(progress);

        float[,] startMap =
            maps[currentMapIndex];

        float[,] targetMap =
            maps[nextMapIndex];

        for (int z = 0; z < vertexMap.GetLength(1); z++)
        {
            for (int x = 0; x < vertexMap.GetLength(0); x++)
            {
                float startHeight =
                    startMap[
                        x * pointToVertRatio,
                        z * pointToVertRatio
                    ];

                float targetHeight =
                    targetMap[
                        x * pointToVertRatio,
                        z * pointToVertRatio
                    ];

                vertexMap[x, z].y =
                    Mathf.Lerp(
                        startHeight,
                        targetHeight,
                        curvedT
                    );
            }
        }

        BuildMatrices();
        RenderWireframe();

        // Advance to the next map once the transition is complete
        if (progress >= 1f)
        {
            currentMapIndex = nextMapIndex;
            nextMapIndex++;

            if (nextMapIndex >= maps.Count) nextMapIndex = 0;

            transitionTimer = 0f;
        }
    }
}
