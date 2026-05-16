using UnityEngine;

/// <summary>
/// Container class for indevidual vertecies terrain data
/// </summary>
public class TerrainPointData
{
    // Fields
    public float height, slope;
    public bool isOccupied = false;
    public string status = null;
    public Vector3 normal;

    // Constructor
    public TerrainPointData(float height, float slope)
    {
        this.height = height;
        this.slope = slope;
    }

    public TerrainPointData(float height, float slope, Vector3 normal)
    {
        this.height = height;
        this.slope = slope;
        this.normal = normal;
    }
}
