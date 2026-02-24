using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

public class SimCharManager : MonoBehaviour
{
    public float Speed=1;
    public float Mass = 1;
    public float MaxForce = 1;
    public float Tolerance = 0.1f;
    public float SlowingRadius = 0.5f;
    public GameObject CharacterModel;
    public GameManager gameMan;
    
    // internal member instance information
    List<Task> CharacterTasks= new List<Task>(10);
    List<GameObject> Characters = new List<GameObject>(10);
    List<Vector3> Targets = new List<Vector3>(10);
    List<PathMovementInfo> CharacterPathMovementInfo= new List<PathMovementInfo>(10);
    List<List<Vector3>> CharacterPaths= new List<List<Vector3>>(10);
    private List<Transform> CharacterTransforms = new List<Transform>(10);
    private List<Vector3> CharacterVelocities = new List<Vector3>(10);
    private MapData[] MapData = null;
    bool mCharactersSpawned = false;
    byte mMinWidth, mMaxWidth;
    byte mMinDepth, mMaxDepth;
    AStarSystem mAStarSystem;

    public List<GameObject> Items = new List<GameObject>(10);

    // When characters are finished spawning the update
    // functionality runs.
    void Update()
    {
        // assumes we have the map data and all char data
        // to go with it
        if (mCharactersSpawned)
        {
            // NOTE: if too many chars this would need to be split and
            // time shared
            int count = Characters.Count;
            for (int i = 0; i < count; i++)
            {
                switch (CharacterTasks[i])
                {
                    case Task.SetTarget:
                        /* OLD Set target system
                        //Debug.Log("setting target for character: " + i);
                        //set target
                        bool targetSet = false;
                        int tryingToSetCount = 0;
                        while (!targetSet && tryingToSetCount < 20)
                        {
                            byte x = (byte) Random.Range(mMinWidth, mMaxWidth);
                            byte z = (byte) Random.Range(mMinDepth, mMaxDepth);
                            float y = MapData[x * (mMaxWidth + 1) + z].height;
                            Targets[i] = new Vector3(x, y, z);
                            if (MapData[x * mMaxWidth + z].type != Tiles.NOT_WALKABLE)
                            {
                                targetSet = true;
                                Debug.DrawLine(new Vector3(x-0.5f, 1, z-0.5f), 
                                    new Vector3(x+0.5f, 1, z+0.5f), Color.red, 10, false);
                                //Debug.Log("target is: " + Targets[i]);
                            }
                            tryingToSetCount++;
                        }

                       
                        // change new task to be pathfinding
                        CharacterTasks[i] = Task.Pathfinding;
                        
                        // add the pathing to the aStar queue
                        //Vector3 currentPos = Characters[i].gameObject.GetComponent<Transform>().position;
                        Vector3 currentPos = CharacterTransforms[i].position;
                        mAStarSystem.queueForPathing.Enqueue(
                            new CharacterPathData { characterID = i, target = Targets[i], pos= currentPos});
                        */

                        // NEW Set target system
                        Transform playerTransform = CharacterTransforms[i];
                        List<GameObject> sortedList = Items

                            // Filtering out targeted objects
                            .Where(obj => !obj.GetComponent<ForageItem>().isTargeted)

                            // Sorting by distance
                            .OrderBy(obj => Vector3.Distance(playerTransform.position, obj.transform.position))
                            .ToList();

                        GameObject clocest = sortedList[0];
                        Vector3 clocestPos = clocest.gameObject.GetComponent<Transform>().position;

                        Targets[i] = new Vector3(
                            clocestPos.x,
                            MapData[(int)clocestPos.x * (mMaxWidth + 1) + (int)clocestPos.z].height,
                            clocestPos.z);

                        Characters[i].GetComponent<Agent>().target = clocest.GetComponent<ForageItem>();

                        // change new task to be pathfinding
                        CharacterTasks[i] = Task.Pathfinding;

                        // add the pathing to the aStar queue
                        //Vector3 currentPos = Characters[i].gameObject.GetComponent<Transform>().position;
                        Vector3 currentPos = new Vector3(playerTransform.position.x,
                            playerTransform.position.y,
                            playerTransform.position.z);

                        mAStarSystem.queueForPathing.Enqueue(
                            new CharacterPathData { characterID = i, target = Targets[i], pos = currentPos });

                        break;
                    case Task.Pathfinding:
                        // astar will look for this task and handle it

                        // refresh target
                        Characters[i].GetComponent<Agent>().target.Reset(gameMan.GetRandLoc());
                        break;
                    case Task.Move:
                        // movement system is called in fixedupdate
                        break;
                    case Task.Idle:
                        // do nothing
                        break;
                    default:
                        break;
                }
            }

            // run AStar on anything that needs it
            mAStarSystem.runAStar(CharacterTasks, CharacterPathMovementInfo, MapData, CharacterPaths);
        }
    }

    // Assumes: map and all char arrays already exist
    public void SpawnRandomCharacters(int numberToSpawn, byte minWidth,
        byte maxWidth,
        byte minDepth, byte maxDepth, MapData[] yMapLocations)
    {
        MapData = yMapLocations;
        mMinWidth = minWidth;
        mMaxWidth = maxWidth;
        mMinDepth = minDepth;
        mMaxDepth = maxDepth;
        mAStarSystem = new AStarSystem(mMaxWidth);
        
        for (int i = 0; i < numberToSpawn; i++)
        {
            bool done = false;
            // hardcode the number of times to try 
            // initializing the location for a spawn
            // so we're not stuck in infinity if somebody
            // fills the board up with obstacles
            int tryInitializing = 100;
            byte x = (byte)Random.Range(minWidth, maxWidth);
            byte z = (byte)Random.Range(minDepth, maxDepth);
            while (!done && tryInitializing > 0)
            {
                if (yMapLocations[x * mMaxWidth + z].type == Tiles.NOT_WALKABLE)
                {
                    // try again!
                    x = (byte)Random.Range(minWidth, maxWidth);
                    z = (byte)Random.Range(minDepth, maxDepth);
                }
                else
                {
                    done = true;
                }
                tryInitializing--;
                if (tryInitializing == 0)
                {
                    Debug.Log("Spawning character " + i + " has failed. Stopping program.");
                    return;
                }
            }
          
            GameObject tmp = GameObject.Instantiate(CharacterModel);
            float y = yMapLocations[x * (maxWidth + 1) + z].height;
            tmp.transform.position = new Vector3(x, y+0.5f, z);
            Characters.Add(tmp);
            CharacterTransforms.Add(tmp.transform);
            CharacterVelocities.Add(Vector3.zero);
            CharacterTasks.Add(Task.SetTarget);
            Targets.Add(new Vector3());
            CharacterPathMovementInfo.Add(new PathMovementInfo());
            List<Vector3> path = new List<Vector3>(100);
            CharacterPaths.Add(path);
            tmp.SetActive(true);
        }
        mCharactersSpawned = true;
        Debug.Log("Characters spawned.");
    }

    public void FixedUpdate()
    {
        // assumes we have the map data and all char data
        // to go with it
        if (mCharactersSpawned)
        {
            // NOTE: if too many chars this would need to be split and
            // time shared
            int count = Characters.Count;
            for (int i = 0; i < count; i++)
            {
                if (CharacterTasks[i] == Task.Move)
                {
                    MovementSystem(i);
                }
            }
        }
    }

    public void MovementSystem(int characterID)
    {
        int currentPathIndex = CharacterPathMovementInfo[characterID].currentIndex;
        List<Vector3> path = CharacterPaths[characterID];
        Vector3 pos = CharacterTransforms[characterID].position;
        PathMovementInfo pathMovement = CharacterPathMovementInfo[characterID];

        if (currentPathIndex < CharacterPathMovementInfo[characterID].length)
        {
            Vector3 currentTarget = new Vector3(path[currentPathIndex].x, pos.y, path[currentPathIndex].z);

            // Calculate DX and DZ (y represents up, therefore we won't be using that in this case).
            // we need to follow targets to get to the end of the path
            float dx = currentTarget.x - pos.x;
            float dz = currentTarget.z - pos.z;
            float xDiff = Mathf.Abs(dx);
            float zDiff = Mathf.Abs(dz);

            if (xDiff > Tolerance || zDiff > Tolerance)
            {
                
                VehicleData data = new VehicleData
                {
                    pos = pos,
                    mass = Mass,
                    max_force=MaxForce,
                    max_speed = Speed,
                    velocity = CharacterVelocities[characterID]
                };

                if (currentPathIndex < pathMovement.length - 1)
                {
                    Seek(ref data, currentTarget);
                }
                else
                {
                    Arrive(ref data, currentTarget);
                }
                
                // transfer the algorithm results to the graphical model
                CharacterVelocities[characterID] = data.velocity;
                CharacterTransforms[characterID].transform.position = data.pos;
                CharacterTransforms[characterID].transform.forward = Vector3.Normalize(data.velocity);
            }
            else
            {
                if (currentPathIndex < pathMovement.length - 1)
                {
                    pathMovement.currentIndex = pathMovement.currentIndex + 1;
                    CharacterPathMovementInfo[characterID] = pathMovement;
                } else
                {
                    CharacterTasks[characterID] = Task.SetTarget;
                    CharacterVelocities[characterID] = Vector3.zero;
                }
                
            }
        }
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


    // Arrive algorithm from a combination of Craig Reynold's Steering paper and 
    // https://code.tutsplus.com/understanding-steering-behaviors-flee-and-arrival--gamedev-1303t
    public void Arrive(ref VehicleData data, Vector3 target)
    {
        Vector3 desired = target - data.pos;
        float mag = desired.magnitude;

        desired = Vector3.Normalize(target - data.pos) * data.max_speed * (mag / 2);
        Vector3 steering = desired - data.velocity;

        data.velocity = truncateLength(data.velocity + steering, data.max_speed);
        data.pos = data.pos + data.velocity;
    }
    
    // Seek algorithm is from Craig Reynold's Steering paper
    public void Seek(ref VehicleData data, Vector3 target)
    {
        data.velocity = truncateLength(data.velocity + GetSeekAcc(ref data, target), data.max_speed);
        data.pos = data.pos + data.velocity;
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
