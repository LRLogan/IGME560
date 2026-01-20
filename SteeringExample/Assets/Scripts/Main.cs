using UnityEngine;

public class Main : MonoBehaviour
{
    public float Mass = 1;
    public float MaxForce = 1;
    public float MaxSpeed = 0.05f;
    public float Slowing_Distance = 1f;
    public GameObject vehicleModel;
    public GameObject targetModel;
    
    private VehicleData vehicleData;
    private Vector3 target;
    
    // We set up the internal data structure for the vehicleData here
    // based on editor values entered.
    void Start()
    {
        vehicleData.mass = Mass;
        vehicleData.max_force = MaxForce;
        vehicleData.max_speed = MaxSpeed;
        vehicleData.slowing_distance = Slowing_Distance;
        vehicleData.velocity = Vector3.zero;
        vehicleData.pos = vehicleModel.transform.position;
        
    }

    // Update is called once per frame
    void Update()
    {
        // make sure to reset the goal if somebody moves it
        target = targetModel.transform.position;

        // Applying a steering behavior
        Arrive(ref vehicleData, target);

        // once the data in the program changes, we
        // can then update the model to show it
        vehicleModel.transform.position = vehicleData.pos;
            vehicleModel.transform.forward = Vector3.Normalize(vehicleData.velocity);
    }

    /// <summary>
    /// Gets the desired acceleration for a target start and end pos
    /// </summary>
    /// <param name="data"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    private Vector3 GetSeekAcc(ref VehicleData data, Vector3 target)
    {
        Vector3 desired_velocity = Vector3.Normalize((target - data.pos) * data.max_speed);
        Vector3 steering = desired_velocity - data.velocity;
        steering = truncateLength(steering, data.max_force);
        return steering / data.mass;
    }

    /// <summary>
    /// The famous seek technique as originally described in Craig Reynold's 1999 GDC paper at:
    /// https://www.red3d.com/cwr/steer/gdc99/
    /// Note that his original paper has position and target reversed if we want to head
    /// toward the target.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="target"></param>
    public void Seek(ref VehicleData data, Vector3 target)
    {
        data.velocity = truncateLength(data.velocity + GetSeekAcc(ref data, target), data.max_speed);
        data.pos = data.pos + data.velocity;
    }

    /// <summary>
    /// The famous flee steering technique as originally described in Craig Reynold's 1999 GDC paper at:
    /// https://www.red3d.com/cwr/steer/gdc99/
    /// </summary>
    /// <param name="data"></param>
    /// <param name="target"></param>
    public void Flee(ref VehicleData data, Vector3 target)
    {
        data.velocity = truncateLength(data.velocity + (GetSeekAcc(ref data, target) * -1), data.max_speed);
        data.pos = data.pos + data.velocity;
    }
    
    /// <summary>
    /// The famous arrive steering technique as originally described in Craig Reynold's 1999 GDC paper at:
    /// https://www.red3d.com/cwr/steer/gdc99/
    /// </summary>
    /// <param name="data"></param>
    /// <param name="target"></param>
    public void Arrive(ref VehicleData data, Vector3 target)
    {
        Vector3 desired = target - data.pos;
        float mag = desired.magnitude;

        desired = Vector3.Normalize(target - data.pos) * data.max_speed * (mag / data.slowing_distance);
        Vector3 steering = desired - data.velocity;

        data.velocity = truncateLength(data.velocity + steering, data.max_speed);
        data.pos = data.pos + data.velocity;
        
        // Added check for snapping when super close to target to avoid extreamly slow speeds
        if((target - data.pos).magnitude < 0.1f)
        {
            data.velocity = Vector3.zero;
            data.pos = target;
        }
    }
    
    // This is a clamping function so that the vector 
    // input does not get larger than the maxLength entered
    public Vector3 truncateLength (Vector3 vector, float maxLength)
    {
        float maxLengthSquared = maxLength * maxLength;
        float vecLengthSquared = vector.sqrMagnitude;
        if (vecLengthSquared <= maxLengthSquared)
            return vector;
        else
            return (vector) * (maxLength / Mathf.Sqrt((vecLengthSquared)));
    }
}
