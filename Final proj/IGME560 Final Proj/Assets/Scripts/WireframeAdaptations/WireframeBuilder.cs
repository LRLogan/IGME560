using System.Net;
using Unity.VisualScripting;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.UIElements;

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
        for (int z = 0; z < heightmap.GetLength(1); z += pointToVertRatio)
        {
            for (int x = 0; x < heightmap.GetLength(0); x += pointToVertRatio)
            {
                // Do something at each desired point 
                vertHeightMap[c, r] = Instantiate(sphere, new Vector3(c, heightmap[x, z].height, r), Quaternion.identity, vertParent);
                c++;
            }
            r++;
            c = 0;
        }

        // Connecting the verts to create the wireframe
        for (int z = 0; z < vertHeightMap.GetLength(1); z ++)
        {
            for (int x = 0; x < vertHeightMap.GetLength(0); x ++)
            {
                if(x + 1 < vertHeightMap.GetLength(0) && z + 1 < vertHeightMap.GetLength(1))
                {
                    GameObject go1 = vertHeightMap[x, z].gameObject;
                    GameObject go2 = vertHeightMap[x+1, z].gameObject; // This is incorrect for final but can prove concept. I will need to account for all edge cases
                    GameObject tempCyl = Instantiate(cyl, 
                        new Vector3(x, go1.transform.position.y, z), 
                        Quaternion.identity);

                    Vector3 desNormal = Vector3.Cross(go2.transform.position - go1.transform.position, go1.transform.forward).normalized;
                    float halfDist = Vector3.Distance(go1.transform.position, go2.transform.position) / 2;
                    tempCyl.transform.forward = desNormal;
                    tempCyl.transform.localScale = new Vector3(0.2f, halfDist, 0.2f);

                    // Applying final pos to be in the center of the 2 verts
                    tempCyl.transform.position = (go1.transform.position + go2.transform.position) * 0.5f; 
                }
                
            }
        }
    }
}
