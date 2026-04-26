using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class TreeGen : MonoBehaviour
{
    private Hashtable ruleSet = new Hashtable(10);
    private StringBuilder rulesToDo = new StringBuilder("");
    private StringBuilder startRule = new StringBuilder("");
    private TerrainGen terrainGen;
    private TerrainPointData[,] heightMap;
    [SerializeField] private TerrainSettings settings;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        terrainGen = FindFirstObjectByType<TerrainGen>();
        heightMap = terrainGen.heightMap;
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
    }

    /// <summary>
    /// Builds a tree with a givin number of iterations and placesit accordingly 
    /// </summary>
    /// <param name="iterations"></param>
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
