using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public class TreeGen : MonoBehaviour
{
    private Hashtable ruleSet = new Hashtable(10);
    private StringBuilder rulesToDo = new StringBuilder("");
    private StringBuilder startRule = new StringBuilder("");
    private TerrainGen terrainGen;
    private TerrainPointData[,] heightMap;
    private float[,] treeOverlayNoise;
    [SerializeField] private TerrainSettings settings;

    public GameObject tempTreeObj;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        terrainGen = FindFirstObjectByType<TerrainGen>();
        heightMap = terrainGen.heightMap;
        treeOverlayNoise = new float[heightMap.GetLength(0), heightMap.GetLength(1)];
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

        // Iterate terrain size / ratio to obtain the correct size of the overlay grid
        for(int i = 0; i < heightMap.GetLength(0) / settings.treeNoiseTerrainRatio; i++)
        {
            for (int j = 0; j < heightMap.GetLength(1) / settings.treeNoiseTerrainRatio; j++)
            {
                // Store the noise value
                treeOverlayNoise[i,j] = Mathf.PerlinNoise(i * settings.treeNoiseFrequency, j * settings.treeNoiseFrequency);

                // convert the noise value to a solid itaration number and include the density modifier
                int iterate = (int)(treeOverlayNoise[i, j] * settings.treeDensityMod);

                // Create that amount of trees in the respective sector on the terrainGrid
                for(int t = 0; t < iterate; t++)
                {
                    // Get an unoccupied locatiojn on the terrain grid and place a tree on it

                }

            }
        }


    }

    private void SpawnTempOnj(Vector3 placePos)
    {
        Instantiate(tempTreeObj, placePos, Quaternion.identity);
    }

    /// <summary>
    /// Builds a tree with a givin number of iterations and placesit accordingly 
    /// </summary>
    /// <param name="iterations">how deep should the tree be made</param>
    /// <param name="placePos">position on terrain</param>
    private void CreateTree(int iterations, Vector3 placePos)
    {
        // Build the tree
        StringBuilder curRule = startRule;
        for(int i = 0; i < iterations; i++)
        {
            for(int j = 0; j < curRule.Length; j++)
            {
                string buffer = GetRule(rulesToDo[j].ToString());
                curRule = curRule.Replace(curRule[j].ToString(), buffer, j, 1);
                j += buffer.Length - 1;
            }
        }
        rulesToDo = curRule;

        // Lastly place the tree
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
    private void Dispatch()
    {
        if (rulesToDo.Length > 0)
        {
            string buffer = rulesToDo[0].ToString();

            switch (buffer)
            {
                case "-":

                    break;

                case "+":

                    break;

                case "F":

                    break;

                case "[":

                    break;

                case "]":

                    break;
            }
        }
    }








}
