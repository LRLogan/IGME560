using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Text;

public class TreeGen : MonoBehaviour
{
    private Hashtable ruleSet = new Hashtable(10);
    private StringBuilder rulesToDo = new StringBuilder("");    // lang in IGME540 PE
    private StringBuilder startRule;
    private TerrainGen terrainGen;
    private TerrainPointData[,] heightMap;
    private float[,] treeOverlayNoise;
    [SerializeField] private TerrainSettings settings;
    [SerializeField] private Transform treeParent;

    // Temp vars while trees are not done
    public GameObject tempTreeObj;
    private float angleToUse = 25f;
    private int iterations = 4;

    // ---------------------------------


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void StartFullTreeGenSeq()
    {
        terrainGen = FindFirstObjectByType<TerrainGen>();
        heightMap = terrainGen.heightMap;
        treeOverlayNoise = new float[heightMap.GetLength(0) / settings.treeNoiseTerrainRatio,
            heightMap.GetLength(1) / settings.treeNoiseTerrainRatio];

        // Hard coding a starter l-sys
        startRule = new StringBuilder("X");
        ruleSet.Add("X", "F-[[X]+X]+F[+FX]-X");
        ruleSet.Add("F", "FF");

        // Start the generation pipeline
        SurveyGrid();
    }

    /// <summary>
    /// Prepares the density of trees in each area of the terrain grid and manages overall placement
    /// </summary>
    private void SurveyGrid()
    {
        // A setting will control how far we are zoomed in on a new perlin noise grid
        // This grid will then overlay the terrain grid such that a cluster of terrain points will be mapped to a single tree noise node
        // Depending on the value of the overlay grid we will know how dense that area should be
        // Lastly we can create and place the tree based on the terrain grid and its point data

        int size;
        int startZ;
        int startX;

        // Iterate terrain size / ratio to obtain the correct size of the overlay grid
        for (int i = 0; i < heightMap.GetLength(0) / settings.treeNoiseTerrainRatio; i++)
        {
            for (int j = 0; j < heightMap.GetLength(1) / settings.treeNoiseTerrainRatio; j++)
            {
                // Store the noise value
                float noise = Mathf.PerlinNoise(
                    i * settings.treeNoiseFrequency, 
                    j * settings.treeNoiseFrequency);

                // convert the noise value to a solid itaration number and include the density modifier
                int attempts = Mathf.RoundToInt(noise * settings.treeDensityMod);

                // Create that amount of trees in the respective sector on the terrainGrid
                for (int t = 0; t < attempts; t++)
                {
                    startX = i * settings.treeNoiseTerrainRatio;
                    startZ = j * settings.treeNoiseTerrainRatio;
                    size = settings.treeNoiseTerrainRatio;
                    // Get an unoccupied locatiojn on the terrain grid and place a tree on it
                    TryPlaceTreeInCell(i, j);
                }

            }
        }


    }

    private void TryPlaceTreeInCell(int i, int j)
    {
        int cellSize = settings.treeNoiseTerrainRatio;

        int baseX = i * cellSize;
        int baseZ = j * cellSize;

        // random point inside the cell
        int x = baseX + UnityEngine.Random.Range(0, cellSize);
        int z = baseZ + UnityEngine.Random.Range(0, cellSize);

        if (!IsValidTreeLocation(x, z)) return;

        PlaceTree(x, z);
    }

    private bool IsValidTreeLocation(int x, int z)
    {
        TerrainPointData p = heightMap[x, z];

        // 1. Occupancy check
        if (p.isOccupied) return false;

        // Slope check
        if (p.slope > settings.maxTreeSlope) return false;

        // Height band (could be used for biome control in future)
        //if (p.height < settings.minTreeHeight || p.height > settings.maxTreeHeight)
            //return false;

        // Normal check 
        if (p.normal.y < settings.minNormalY) return false;

        // Nearby tree check
        if (HasNearbyTree(x, z, settings.treeSpacingRadius)) return false;

        return true;
    }

    /// <summary>
    /// Currently checks for nearby tree but could be anuthing that is occupying the space
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    private bool HasNearbyTree(int x, int z, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                int nx = x + dx;
                int nz = z + dz;

                if (nx < 0 || nz < 0 || nx >= heightMap.GetLength(0) || nz >= heightMap.GetLength(1))
                    continue;

                if (heightMap[nx, nz].isOccupied)
                    return true;
            }
        }

        return false;
    }

    private void PlaceTree(int x, int z)
    {
        TerrainPointData p = heightMap[x, z];

        Vector3 pos = new Vector3(x, p.height, z);

        // Swap out for CreateTree when ready 
        //Instantiate(tempTreeObj, AdjustPosToTerrain(pos), Quaternion.identity, treeParent);
        CreateTree(3, AdjustPosToTerrain(pos));
        

        // mark occupied
        p.isOccupied = true;
        p.status = "tree";
    }

    /// <summary>
    /// Builds a tree with a givin number of iterations and placesit accordingly 
    /// </summary>
    /// <param name="iterations">how deep should the tree be made</param>
    /// <param name="placePos">position on terrain</param>
    private void CreateTree(int iterations, Vector3 placePos)
    {
        // Build the tree rules
        StringBuilder curRule = new StringBuilder(startRule.ToString());
        for (int i = 0; i < iterations; i++)
        {
            for(int j = 0; j < curRule.Length; j++)
            {
                string buffer = GetRule(curRule[j].ToString());
                curRule = curRule.Replace(curRule[j].ToString(), buffer, j, 1);
                j += buffer.Length - 1;
            }
        }
        rulesToDo = curRule;

        // Lastly finish decoding the rules and place the tree
        Dispatch(placePos);
    }

    /// <summary>
    /// Quick access to a rule 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private string GetRule(string input) => 
        ruleSet.ContainsKey(input) ? (string)ruleSet[input] : input;


    /// <summary>
    /// Responcible for dispatching a set of rules for constructing a tree
    /// </summary>
    private void Dispatch(Vector3 startPos)
    {
        Stack<TurtleState> stack = new Stack<TurtleState>();

        Vector3 pos = startPos;

        // Start pointing UP (critical for trees)
        Quaternion rot = Quaternion.LookRotation(Vector3.up);

        float step = settings.branchLength;
        float angle = angleToUse;

        for (int i = 0; i < rulesToDo.Length; i++)
        {
            char c = rulesToDo[i];

            switch (c)
            {
                case 'F':
                    {
                        Vector3 newPos = pos + rot * Vector3.forward * step;

                        Debug.DrawLine(pos, newPos, Color.green, 100f);

                        pos = newPos;
                        break;
                    }

                // Yaw (left/right)
                case '+':
                    rot *= Quaternion.Euler(0, angle, 0);
                    break;

                case '-':
                    rot *= Quaternion.Euler(0, -angle, 0);
                    break;

                // Pitch (up/down branching)
                case '&':
                    rot *= Quaternion.Euler(angle, 0, 0);
                    break;

                case '^':
                    rot *= Quaternion.Euler(-angle, 0, 0);
                    break;

                // Roll (adds natural variation)
                case '\\':
                    rot *= Quaternion.Euler(0, 0, angle);
                    break;

                case '/':
                    rot *= Quaternion.Euler(0, 0, -angle);
                    break;

                case '[':
                    stack.Push(new TurtleState
                    {
                        position = pos,
                        rotation = rot
                    });
                    break;

                case ']':
                    if (stack.Count > 0)
                    {
                        var state = stack.Pop();
                        pos = state.position;
                        rot = state.rotation;
                    }
                    break;
            }
        }

        // Convert into splines here
    }

    /// <summary>
    /// Accounts for the terrain offset
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private Vector3 AdjustPosToTerrain(Vector3 pos)
    {
        return new Vector3(
            pos.x - settings.size / 2,
            pos.y,
            pos.z - settings.size / 2);
    }





}

struct TurtleState
{
    public Vector3 position;
    public Quaternion rotation;
}
