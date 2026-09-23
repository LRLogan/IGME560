using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Generates and places cliff prefabs around the edges of a procedural island.
    /// </summary>
    public class PrefabCliffsGen : MonoBehaviour
    {
        [Header("Cliff Prefabs")]
        [SerializeField] private GameObject cliffStraightPrefab;
        [SerializeField] private GameObject cliffInsideCornerPrefab;
        [SerializeField] private GameObject cliffOutsideCornerPrefab;

        [Header("Cliff Settings")]
        [SerializeField] private float cliffSlope = 0.7f;
        [SerializeField] private float cliffHeight = 5f;
        [SerializeField] private float cliffOffset = 0.05f;

        /// <summary>
        /// Entry point used by TerrainGen to generate the cliff prefabs
        /// around the island.
        /// </summary>
        public void CreateCliffs(IslandTerrainData terrainData)
        {
            if (terrainData == null)
            {
                Debug.LogWarning("PrefabCliffsGen: Terrain data is null.");
                return;
            }

            if (terrainData.islandRef == null)
            {
                Debug.LogWarning("PrefabCliffsGen: Island reference is null.");
                return;
            }

            // Find all potential cliff edges around the island
            List<CliffEdge> cliffEdges = FindCliffEdges(terrainData);

            if (cliffEdges.Count == 0)
            {
                Debug.Log("PrefabCliffsGen: No cliff edges found.");
                return;
            }

            // Connect individual edges into continuous cliff sections
            List<CliffChain> cliffChains = ConnectCliffEdges(cliffEdges);

            // Create the cliff prefabs from the connected sections
            SpawnCliffPieces(terrainData, cliffChains);
        }

        /// <summary>
        /// Finds terrain edges that qualify as cliffs.
        /// </summary>
        private List<CliffEdge> FindCliffEdges(IslandTerrainData terrainData)
        {
            List<CliffEdge> cliffEdges = new List<CliffEdge>();

            // TODO Cliff detection 

            return cliffEdges;
        }

        /// <summary>
        /// Connects individual cliff edges into continuous
        /// sections following the shape of the island.
        /// </summary>
        private List<CliffChain> ConnectCliffEdges(List<CliffEdge> cliffEdges)
        {
            List<CliffChain> cliffChains = new List<CliffChain>();

            // TODO Edge connection logic 

            return cliffChains;
        }

        /// <summary>
        /// Places the appropriate cliff prefabs along each
        /// connected cliff section.
        /// </summary>
        private void SpawnCliffPieces(IslandTerrainData terrainData, List<CliffChain> cliffChains)
        {
            // TODO Prefab placement logic 
        }


        #region Helper classes
        /// <summary>
        /// Stores information about a single cliff boundary edge.
        /// </summary>
        private class CliffEdge
        {
            public Vector3 start;
            public Vector3 end;

            public Vector3 tangent;
            public Vector3 outward;

            public float height;
            public float slope;
        }

        /// <summary>
        /// Stores a connected series of cliff edges.
        /// </summary>
        private class CliffChain
        {
            public List<CliffEdge> edges = new List<CliffEdge>();
        }
        #endregion
    }
}
