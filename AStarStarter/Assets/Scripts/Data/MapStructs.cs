

// The map data used in A*
// note that height is only useful if
// terrain isn't flat
public struct MapData
{
    public float height;
    public Tiles type;
    public int cost;
}

// The types of tiles for the terrain
// currently only two types
// and no real costs associated with them other
// than impassible
public enum Tiles
{
    NOT_WALKABLE,
    WALKABLE,
}