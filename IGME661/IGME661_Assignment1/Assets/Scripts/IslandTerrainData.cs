using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    internal class IslandTerrainData
    {
        public readonly int width;
        public readonly int depth;

        public readonly float[] heightMap;
        public readonly float[] islandMask;

        public readonly float worldWidth;
        public readonly float worldDepth;
        public readonly float maxHeight;

        public IslandTerrainData(
            int width,
            int depth,
            float worldWidth,
            float worldDepth,
            float maxHeight)
        {
            if (width < 2)
                throw new ArgumentException("Width must be at least 2.");

            if (depth < 2)
                throw new ArgumentException("Depth must be at least 2.");

            this.width = width;
            this.depth = depth;

            this.worldWidth = worldWidth;
            this.worldDepth = worldDepth;
            this.maxHeight = maxHeight;

            heightMap = new float[width * depth];
            islandMask = new float[width * depth];
        }

        public int GetIndex(int x, int z)
        {
            return x * depth + z;
        }

        public float GetHeight(int x, int z)
        {
            return heightMap[GetIndex(x, z)];
        }

        public void SetHeight(int x, int z, float value)
        {
            heightMap[GetIndex(x, z)] = value;
        }

        public void SetIslandMask(int x, int z, float value)
        {
            islandMask[GetIndex(x, z)] = value;
        }

    }
}
