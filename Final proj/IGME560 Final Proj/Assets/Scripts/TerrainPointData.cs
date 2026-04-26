using UnityEngine;

public class TerrainPointData
{
    // Fields
    public float height, slope;
    public bool isOccupied = false;
    public string status = null;

    // Constructor
    public TerrainPointData(float height, float slope)
    {
        this.height = height;
        this.slope = slope;
    }
}
