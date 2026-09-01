using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Tool to create a texture atlas based off of given textures
/// </summary>
public class AtlasGen : MonoBehaviour
{
    public List<Texture> textures;
    public int imgSize = 64;
    public static int atlasHeight = 0;
    public static int atlasWidth = 0;
    public static Texture2D atlas;

    private int pixelWidth = 64;
    private int pixelHeight = 64;
    private static List<TextureUV> textureUVs;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="outputDirName"></param>
    /// <param name="outputFileName"></param>
    /// <param name="imageSize"></param>
    /// <returns>-1 error, 0 already made, 1 Atlas made</returns>
    public int GenerateTextureAtlas(string outputDirName, string outputFileName, int imageSize)
    {
        // Early exits
        if (textures.Count == 0) return -1;
        if (File.Exists(Path.Combine(outputDirName, outputFileName))) return 0;

        #region Raw atlas file setup
        // Take the image size into the function
        // Assume all images are a power of 2 and square
        pixelWidth = imageSize;
        pixelHeight = imageSize;

        // Make the list of uvs
        textureUVs = new List<TextureUV>(textures.Count);

        // We're going to assume our images are a power of 2 so we just
        // need to get the sqrt of the number of images and round up
        int squareRoot = Mathf.CeilToInt(Mathf.Sqrt(textures.Count));
        int squareRootH = squareRoot;
        atlasWidth = squareRoot * pixelWidth;
        atlasHeight = squareRootH * pixelHeight;

        if (squareRoot * (squareRoot - 1) > textures.Count)
        {
            squareRootH = squareRootH - 1;
            atlasHeight = squareRootH * pixelHeight;
        }

        // allocate space for the atlas and file data
        atlas = new Texture2D(atlasWidth, atlasHeight);
        byte[][] fileData = new byte[textures.Count][];
        #endregion

        #region Adding textures to the Atlas
        // read the file data in parallel
        Parallel.For(0, textures.Count,
            index => { fileData[index] = File.ReadAllBytes(textures[index].name); });
        

        // Put all the images into the image file and write
        // all the texture data to the texture uv map list.
        int x1 = 0;
        int y1 = 0;
        Texture2D temp = new Texture2D(pixelWidth, pixelHeight);
        float pWidth = (float)pixelWidth;
        float pHeight = (float)pixelHeight;
        float aWidth = (float)atlas.width;
        float aHeight = (float)atlas.height;

        for (int i = 0; i < textures.Count; i++)
        {
            // Assigning start / end pixeld while accounting for anti-ailiasing
            float pixelStartX = ((x1 * pWidth) + 1) / aWidth + 1;
            float pixelStartY = ((y1 * pHeight) + 1) / aHeight + 1;
            float pixelEndX = ((x1 + 1) * pWidth - 1) / aWidth - 1;
            float pixelEndY = ((y1 + 1) * pHeight - 1) / aHeight - 1;
            TextureUV currentUVInfo = new TextureUV
            {
                ID = i,
                pixelStartX = pixelStartX,
                pixelStartY = pixelStartY,
                pixelEndY = pixelEndY,
                pixelEndX = pixelEndX,
            };
            textureUVs.Add(currentUVInfo);

            temp.LoadImage(fileData[i]);
            atlas.SetPixels(x1 * pixelWidth, y1 * pixelHeight, pixelWidth, pixelHeight, temp.GetPixels());

            x1 = (x1 + 1) % squareRoot;
            if (x1 == 0)
            {
                y1++;
            }


        }

        atlas.alphaIsTransparency = true;
        atlas.Apply();

        // write the atlas out to a file
        // note that the default dir is usually the upper level project dir
        File.WriteAllBytes(outputFileName, atlas.EncodeToPNG());
        #endregion
        // Atlas made
        return 1;
    }
}
