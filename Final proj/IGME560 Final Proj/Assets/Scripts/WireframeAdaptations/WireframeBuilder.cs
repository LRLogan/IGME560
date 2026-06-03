using System.Net;
using Unity.VisualScripting;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Wireframe visualizer of a heightmap written by Logan Larrondo
/// </summary>
public class WireframeBuilder : MonoBehaviour
{
    [SerializeField] private TerrainSettings settings;
    [SerializeField] [Tooltip("1 for a 1:1")] private int pointToVertRatio;
    [SerializeField] private GameObject sphere;
    [SerializeField] private GameObject cyl;
    [SerializeField] private Transform vertParent;
    [SerializeField] private Material lineMat;
    private GameObject[,] vertHeightMap;

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
        StartWireframePipeline();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartWireframePipeline()
    {
        TerrainPointData[,] heightmap = TerrainGen.GenerateTerrainHeightMap(settings);
        BuildWireframeFromHeights(heightmap);
    }

    public void BuildWireframeFromHeights(TerrainPointData[,] heightmap)
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
                    ConnectPoints(
                        current,
                        vertHeightMap[x + 1, z].gameObject
                    );
                }

                if (z + 1 < height)
                {
                    ConnectPoints(
                        current,
                        vertHeightMap[x, z + 1].gameObject
                    );
                }

                if (x + 1 < width &&
                    z + 1 < height)
                {
                    ConnectPoints(
                        current,
                        vertHeightMap[x + 1, z + 1].gameObject
                    );
                }
            }
        }
    }

    /// <summary>
    /// Helper to build connection points for the grid
    /// </summary>
    /// <param name="go1"></param>
    /// <param name="go2"></param>
    private void ConnectPoints(GameObject go1, GameObject go2)
    {
        GameObject tempCyl = Instantiate(cyl);

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
}
