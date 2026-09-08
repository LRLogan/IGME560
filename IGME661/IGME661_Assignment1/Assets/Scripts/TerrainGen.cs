using UnityEngine;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Main entry point for full terrain gen seq
    /// </summary>
    public void StartFullTerrainGen()
    {

    }
}
