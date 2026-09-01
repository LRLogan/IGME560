using UnityEngine;


public class CreateTextureAtlas : MonoBehaviour
{
    // Name of directory to get files from
    public string DirectoryName = "blocks";
    public string OutputFileName = "../atlas.png";
    public int ImageSize = 64;
    public void Start()
    {

        UnityEngine.Debug.Log("Starting");
        
        TextureAtlas.instance.CreateAtlasComponentData(DirectoryName, OutputFileName, ImageSize);

        UnityEngine.Debug.Log("Done with creation of texture atlas.");
    }

    
}
