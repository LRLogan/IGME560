using Assets.Scripts;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Tool to create a texture atlas based off of given textures
/// </summary>
public class AtlasGen : MonoBehaviour
{
    public List<Texture2D> textures;
    public List<Texture2D> atlassesToUnpack;
    public int imgSize = 16;
    public static int atlasHeight = 0;
    public static int atlasWidth = 0;
    public static Texture2D atlas;

    private int pixelWidth = 64;
    private int pixelHeight = 64;
    private static List<TextureUV> textureUVs;

    [SerializeField] private string outputDirName;
    [SerializeField] private string outputFileName;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="outputDirName"></param>
    /// <param name="outputFileName"></param>
    /// <param name="imageSize"></param>
    /// <returns>-1 error, 0 already made, 1 Atlas made</returns>
    public int GenerateTextureAtlas()
    {
        // Early exits
        if (textures == null)
        {
            textures = new List<Texture2D>();
        }

        // Adding any textures from existing atlasses
        if (atlassesToUnpack != null && atlassesToUnpack.Count > 0)
        {
            foreach (Texture2D atlas in atlassesToUnpack)
            {
                /*nums are hard coded here because I know what atlas I am unpacking*/
                textures.AddRange(UtilsC.UnpackAtlas(atlas, 16, 16, 4));
                Debug.Log($"Added textures");
            }
        }

        // Check this AFTER unpacking any existing atlases
        if (textures.Count == 0) return -1;

        string outputPath =
            Path.Combine(
                outputDirName,
                outputFileName
            );

        if (File.Exists(outputPath)) return 0;

        #region Raw atlas file setup
        // Take the image size into the function
        // Assume all images are a power of 2 and square
        pixelWidth = imgSize;
        pixelHeight = imgSize;

        // Make the list of uvs
        textureUVs = new List<TextureUV>(textures.Count);

        // We're going to assume our images are a power of 2 so we just
        // need to get the sqrt of the number of images and round up
        int squareRoot = Mathf.CeilToInt(Mathf.Sqrt(textures.Count));

        int squareRootH =
            Mathf.CeilToInt(
                textures.Count /
                (float)squareRoot
            );

        atlasWidth = squareRoot * pixelWidth;
        atlasHeight = squareRootH * pixelHeight;

        // allocate space for the atlas
        atlas = new Texture2D(atlasWidth, atlasHeight);
        #endregion

        #region Adding textures to the Atlas
        // Put all the images into the image file and write
        // all the texture data to the texture uv map list.
        int x1 = 0;
        int y1 = 0;

        float pWidth = (float)pixelWidth;
        float pHeight = (float)pixelHeight;
        float aWidth = (float)atlas.width;
        float aHeight = (float)atlas.height;

        for (int i = 0; i < textures.Count; i++)
        {
            // Assigning start / end pixels while accounting for anti-aliasing
            float pixelStartX = (x1 * pWidth) / aWidth;
            float pixelStartY = (y1 * pHeight) / aHeight;
            float pixelEndX = (x1 + 1) * pWidth / aWidth;
            float pixelEndY = (y1 + 1) * pHeight / aHeight;

            TextureUV currentUVInfo = new TextureUV
            {
                ID = i,
                pixelStartX = pixelStartX,
                pixelStartY = pixelStartY,
                pixelEndY = pixelEndY,
                pixelEndX = pixelEndX,
            };

            textureUVs.Add(currentUVInfo);

            Texture2D temp = textures[i];

            if (temp == null)
            {
                Debug.LogWarning(
                    $"Texture at index {i} is null. Skipping."
                );

                continue;
            }

            atlas.SetPixels(
                x1 * pixelWidth,
                y1 * pixelHeight,
                pixelWidth,
                pixelHeight,
                temp.GetPixels()
            );

            x1 = (x1 + 1) % squareRoot;

            if (x1 == 0)
            {
                y1++;
            }
        }

        atlas.alphaIsTransparency = true;
        atlas.Apply();

        // Make sure the output directory exists
        if (!Directory.Exists(outputDirName))
        {
            Directory.CreateDirectory(outputDirName);
        }

        // write the atlas out to a file
        File.WriteAllBytes(
            outputPath,
            atlas.EncodeToPNG()
        );
        #endregion

        // Atlas made
        Debug.Log(
            $"File in dir: {outputPath} \n" +
            $"Textures count: {textures.Count}"
        );

        return 1;
    }

}
