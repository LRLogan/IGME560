using UnityEngine;

/// <summary>
/// Script to control the overall flow of the simulation.
/// This script should be last in the execution order.
/// </summary>
public class SimManager : MonoBehaviour
{
    [SerializeField] private TerrainGen terrainGen;
    [SerializeField] private TreeGen treeGen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        FullRegen();
    }

    public void FullRegen()
    {
        terrainGen.StartFullTerrainSeq();
        treeGen.StartFullTreeGenSeq();
    }

}
