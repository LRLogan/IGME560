using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    internal class Utils
    {
        public Texture2D[] UnpackAtlas(Texture2D atlas, int atlasSize, int texCount, int texSize)
        {
            Texture2D[] textures = new Texture2D[texCount];

            // Number of textures that fit across one row/column
            int texturesPerRow = atlasSize / texSize;

            int x = 0;
            int y = 0;

            // Loop through each texture in the atlas
            for (int i = 0; i < texCount; i++)
            {
                Texture2D curTex = new Texture2D(texSize, texSize);

                // Get the pixel data from this region of the atlas
                Color[] pixels = atlas.GetPixels(
                    x * texSize,
                    y * texSize,
                    texSize,
                    texSize
                );

                curTex.SetPixels(pixels);
                curTex.Apply();
                textures[i] = curTex;

                // Move to the next position in the atlas
                x++;
                if (x >= texturesPerRow)
                {
                    x = 0;
                    y++;
                }
            }
            return textures;
        }
    }
}
