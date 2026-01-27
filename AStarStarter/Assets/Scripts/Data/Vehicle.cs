using UnityEngine;

/// <summary>
/// From Craig Reynolds famous steering paper:
/// https://www.red3d.com/cwr/steer/gdc99/
/// Orientation has been left off below since it can
/// be calculated from the velocity.
/// 
/// Simple Vehicle Model:
/// mass          scalar
/// position      vector
/// velocity      vector
/// max_force     scalar
/// max_speed     scalar
/// </summary>
public struct VehicleData
{
    public float mass;
    public Vector3 pos;
    public Vector3 velocity;
    public float max_force;
    public float max_speed;
}
