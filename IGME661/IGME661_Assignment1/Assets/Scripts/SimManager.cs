using UnityEngine;

/// <summary>
/// Controls overall pipeline behavior for the terrain gen sim
/// </summary>
public class SimManager : MonoBehaviour
{
    [SerializeField] private AtlasGen atlasGen;
    [SerializeField] private TerrainGen terrainGen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Generate the texture atlas if needed
        //atlasGen.GenerateTextureAtlas();
        //terrainGen.StartFullTerrainGen();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
