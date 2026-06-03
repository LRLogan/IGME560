using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.UIElements;

public class WireframeBuilder : MonoBehaviour
{
    [SerializeField] private TerrainSettings settings;
    [SerializeField] [Tooltip("1 for a 1:1")] private int pointToVertRatio;
    [SerializeField] private GameObject sphere;
    [SerializeField] private GameObject emptyGO;
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
        BuildWireframe();
    }

    private void BuildWireframe()
    {
        TerrainPointData[,] heightmap = TerrainGen.GenerateTerrainHeightMap(settings);
        vertHeightMap = new GameObject[heightmap.GetLength(0) / pointToVertRatio, 
            heightmap.GetLength(1) / pointToVertRatio];

        // I know I can do this mathimatically but the formula escapes me atm
        int r = 0, c = 0;

        // Loop over the height map at point ratio intervals
        for (int z = 0; z < heightmap.GetLength(0); z += pointToVertRatio)
        {
            for (int x = 0; x < heightmap.GetLength(1); x += pointToVertRatio)
            {
                // Do something at each desired point 
                vertHeightMap[c, r] = Instantiate(sphere, new Vector3(c, heightmap[x, z].height, r), Quaternion.identity, vertParent);
                c++;
            }
            r++;
            c = 0;
        }

        // Connecting the verts to create the wireframe
        for (int z = 0; z < vertHeightMap.GetLength(0); z ++)
        {
            for (int x = 0; x < vertHeightMap.GetLength(1); x ++)
            {
                GameObject lrObj = Instantiate(emptyGO, new Vector3(0,0,0), Quaternion.identity);
                LineRenderer lineRenderer = lrObj.AddComponent<LineRenderer>();
                lineRenderer.material = lineMat;

                // Configure thickness and color properties
                lineRenderer.startWidth = 1.0f;
                lineRenderer.endWidth = 1.0f;
                lineRenderer.startColor = Color.red;
                lineRenderer.endColor = Color.cyan;

                // Allocate the size of the point array for each line
                lineRenderer.positionCount = 2;

                // Setting coords for start finish
                if(x + 1 < vertHeightMap.GetLength(1) && z + 1 < vertHeightMap.GetLength(0))
                {
                    lineRenderer.SetPosition(0, new Vector3(x, vertHeightMap[x, z].gameObject.transform.position.y, z));
                    lineRenderer.SetPosition(1, new Vector3(x + 1, vertHeightMap[x, z].gameObject.transform.position.y, z + 1));
                }
                
            }
        }
    }
}
