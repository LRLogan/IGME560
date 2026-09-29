using System;
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Stores the generated data for one island.
    /// Heights and masks are normalized to the range 0-1.
    /// </summary>
    public class IslandTerrainData
    {
        public readonly int width;
        public readonly int depth;

        public readonly float[] heightMap;
        public readonly float[] islandMask;
        public int[] coastDistance;

        public readonly float worldWidth;
        public readonly float worldDepth;
        public readonly float maxHeight;

        public GameObject islandRef;

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

            if (worldWidth <= 0)
                throw new ArgumentException("World width must be greater than 0.");

            if (worldDepth <= 0)
                throw new ArgumentException("World depth must be greater than 0.");

            if (maxHeight <= 0)
                throw new ArgumentException("Max height must be greater than 0.");

            this.width = width;
            this.depth = depth;

            this.worldWidth = worldWidth;
            this.worldDepth = worldDepth;
            this.maxHeight = maxHeight;

            heightMap = new float[width * depth];
            islandMask = new float[width * depth];
            coastDistance = new int[width * depth];
        }

        public void SetIslandRef(GameObject islandRef)
        {
            this.islandRef = islandRef;
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

        public float GetIslandMask(int x, int z)
        {
            return islandMask[GetIndex(x, z)];
        }

        public void SetIslandMask(int x, int z, float value)
        {
            islandMask[GetIndex(x, z)] = value;
        }
    }
}