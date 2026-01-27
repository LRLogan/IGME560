using System;
using Unity.Mathematics;
using UnityEngine;

// to keep track of
// the cost where we came from
// and the current cost added from
// the new node
[Serializable]
public struct PathFindingData
{
    public float3 comeFrom;
    public short currentCost;
}

// current position and target to move to
// for a character
[Serializable]
public struct CharacterPathData
{
    public int characterID;
    public Vector3 target;
    public Vector3 pos;
}

// gives pathing information such
// as the current node target index and 
// length of the path info for the goal
[Serializable]
public struct PathMovementInfo 
{
    public int currentIndex;
    public short length;
}