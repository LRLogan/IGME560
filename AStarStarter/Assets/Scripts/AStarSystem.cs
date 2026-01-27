using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Utils;

public class AStarSystem : IDisposable
{
    PriorityQueue<Vector3, float> mPriorityQueue;
    NativeHashMap<int, PathFindingData> mScratchMap;
    int mBoardWidth;
    public Queue<CharacterPathData> queueForPathing = new Queue<CharacterPathData>(20);

    public AStarSystem (int boardWidth)
    {
        mBoardWidth = boardWidth;
        mPriorityQueue = new PriorityQueue<Vector3, float>(500);
        mScratchMap = new NativeHashMap<int, PathFindingData>(boardWidth * 3, Allocator.Persistent);
    }


    public void Dispose()
    {
        if (mScratchMap.IsCreated)
        {
            mScratchMap.Dispose();
        }
    }
    
    public void runAStar(List<Task> characterTasks, List<PathMovementInfo> pathMovement, MapData[] mapData, 
        List<List<Vector3>> CharacterPaths)
    {
        int count = queueForPathing.Count;
        if (count > 0)
        {
            // run a star for all characters in the queue
            for (int i = 0; i < count; i++)
            {
                CharacterPathData charData = queueForPathing.Dequeue();
                int characterID = charData.characterID;
                if (characterTasks[characterID] == Task.Pathfinding)
                {
                    //Debug.Log("starting pathfinding for character: " + characterID);
                    Vector3 target = charData.target;
                    Vector3 pos = charData.pos;
                    PathMovementInfo pathMove = pathMovement[characterID];
                    
                    // path list to add to
                   var path = CharacterPaths[characterID];
                    path.Clear();
                    mScratchMap.Clear();
                    mPriorityQueue.Clear();
                    int currentX = (int)Mathf.Round(pos.x);
                    int currentZ = (int)Mathf.Round(pos.z);
                    if ((int)target.x == currentX && (int)target.z == currentZ)
                    {
                        pathMove.length = 0;
                        pathMove.currentIndex = 0;
                        // we're at the target, so find another one
                        characterTasks[characterID] = Task.SetTarget;
                    }
                    else
                    {
                        // we'll search from the target to the start so that
                        // we don't have to reverse the path at the end and 
                        // because all nodes are equally traversable both ways
                        int x = (int)target.x;
                        int y = (int)target.y;
                        int z = (int)target.z;
                        int currentCost = 0;
                        bool found = false;

                        mPriorityQueue.Enqueue(new Vector3(x, y, z), 0.0f);
                        var pathFindingData = new PathFindingData
                        {
                            comeFrom = new Vector3(x, y, z),
                            currentCost = (short)currentCost
                        };
                        if (!mScratchMap.TryAdd(x * mBoardWidth + z, pathFindingData))
                        {
                            // something weird is wrong so just return
                            return;
                        }


                        while (mPriorityQueue.Count>0)
                        {
                            var current = mPriorityQueue.Dequeue();
                            x = (int)Mathf.Round(current.x);
                            y = (int)Mathf.Round(current.y);
                            z = (int)Mathf.Round(current.z);

                            // if our hash map is too full, then we should quit looking
                            // the constant of 5 is one more than the # of nodes potentially
                            // added during an iteration
                            if (mScratchMap.Count >= ((mBoardWidth * mBoardWidth / 2) - 5))
                            {
                                found = false;
                                break;
                            }

                            // if it came out of the priority queue, it's also in the hashmap
                            mScratchMap.TryGetValue(x * mBoardWidth + z, out pathFindingData);
                            if (x == currentX && z == currentZ)
                            {
                                // we found the path
                                found = true;
                                break;
                            }
                            else
                            {
                                // add the neighbors if they haven't been visited before
                                // NOTE: not moving in diagonals here
                                // left
                                AddToQueueIfNeeded(new Vector3(x - 1, y, z), mBoardWidth, ref mPriorityQueue, ref mScratchMap,
                                    pathFindingData, new Vector3 { x = x, y = y, z = z }, new Vector3 { x = pos.x, y = pos.y, z = pos.z },
                                    mapData);
                                // right
                                AddToQueueIfNeeded(new Vector3(x + 1, y, z), mBoardWidth, ref mPriorityQueue, ref mScratchMap,
                                    pathFindingData, new Vector3 { x = x, y = y, z = z }, new Vector3 { x = pos.x, y = pos.y, z = pos.z },
                                    mapData);
                                // down
                                AddToQueueIfNeeded(new Vector3(x, y, z - 1), mBoardWidth, ref mPriorityQueue, ref mScratchMap,
                                    pathFindingData, new Vector3 { x = x, y = y, z = z }, new Vector3 { x = pos.x, y = pos.y, z = pos.z },
                                    mapData);
                                // up
                                AddToQueueIfNeeded(new Vector3(x, y, z + 1), mBoardWidth, ref mPriorityQueue, ref mScratchMap,
                                    pathFindingData, new Vector3 { x = x, y = y, z = z }, new Vector3 { x = pos.x, y = pos.y, z = pos.z },
                                    mapData);
                            }

                        }
                        
                        // now put the path together from the comeFrom nodes
                        if (found)
                        {
                            int newPathLength = 0;
                            path.Clear();
                            path.Add(new Vector3 ( x, y, z ));
                            // Debug text to make sure things are correct:
                            //UnityEngine.Debug.Log("pathfinding from : " + pos + " to " + target);
                            while (mScratchMap.TryGetValue(x * mBoardWidth + z, out pathFindingData))
                            {
                                if (pathFindingData.comeFrom.x == x && pathFindingData.comeFrom.z == z)
                                {
                                    // this is the end of the path and we're looping
                                    newPathLength++;
                                    path.Add(new Vector3(x, y, z));
                                    break;
                                }
                                else
                                {
                                    newPathLength++;
                                    x = (int)pathFindingData.comeFrom.x;
                                    y = (int)pathFindingData.comeFrom.y;
                                    z = (int)pathFindingData.comeFrom.z;
                                    path.Add(new Vector3( x, y, z));
                                    //UnityEngine.Debug.Log("index " + pathLength + " is " + x + " " + y);
                                    Debug.DrawLine(new Vector3(x - 0.5f, 1, z - 0.5f),
                                    new Vector3(x + 0.5f, 1, z + 0.5f), Color.yellow, 10, false);

                                }
                            }
                            pathMove.length = (short)newPathLength;
                            pathMove.currentIndex = 0;

                            characterTasks[characterID] = Task.Move;

                        }
                        else
                        {
                            Debug.Log("path not found from: " + target + " to " + pos);
                            characterTasks[characterID] = Task.SetTarget;
                        }
                    }

                    pathMovement[characterID] = pathMove;
                }
            }
        }
    }

    public void AddToQueueIfNeeded(Vector3 pos, int boardWidth, ref PriorityQueue<Vector3, float> priorityQueue,
        ref NativeHashMap<int, PathFindingData> pathfindingData, PathFindingData previousData, Vector3 comeFrom, Vector3 goal,
        MapData[] gridMapData)
    {
        PathFindingData data;
        int x = (int)pos.x;
        int z = (int)pos.z;
        if (x >= 0 && z >= 0 && x < boardWidth && z < boardWidth)
        {
            if (gridMapData[x * boardWidth + z].type != Tiles.NOT_WALKABLE)
            {
                int cost = gridMapData[x * boardWidth + z].cost;
                // we should add this as it's a legal value
                int newCost = previousData.currentCost + cost;
                float priority = 0;
                int hvalue = 0;
                if (pathfindingData.TryGetValue(x * boardWidth + z, out data))
                {
                    // NOTE: this would make sense if we had different costs depending on map
                    // tile. It isn't necessary when cost is uniform throughout
                    // only add again if it's a cheaper cost
                    // if (newCost < data.currentCost)
                    // {
                    //     data.currentCost = newCost;
                    //     data.comeFrom = comeFrom;
                    //     pathfindingData.Remove(x * boardWidth + y);
                    //     pathfindingData.TryAdd(x * boardWidth + y, data);
                    //     hvalue = manhattanDistance(goal, pos);
                    //     priority = newCost + hvalue;
                    //     priorityQueue.Put(new PathElementWithPriority {position = pos, priority = priority});
                    // }
                }
                else
                {
                    // this hasn't been searched at all, so let's add it
                    data.currentCost = (short)newCost;
                    data.comeFrom = comeFrom;
                    pathfindingData.TryAdd(x * boardWidth + z, data);
                    // heuristic cost:
                    hvalue = manhattanDistance(goal, pos);
                    priority = newCost + hvalue;
                    priorityQueue.Enqueue(pos, priority);
                    Debug.DrawLine(new Vector3(x - 0.5f, 1, z - 0.5f),
                        new Vector3(x + 0.5f, 1, z + 0.5f), Color.blue, 10, false);
                }
            }

        }
    }

    // assume that everything is the same in terms of movement cost (diag vs. straight)
    public int manhattanDistance(Vector3 goal, Vector3 current)
    {
        return (int)(Mathf.Abs(current.x - goal.x) + Mathf.Abs(current.z - goal.z));
    }
}
