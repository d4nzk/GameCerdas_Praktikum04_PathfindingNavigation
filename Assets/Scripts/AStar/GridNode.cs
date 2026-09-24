using UnityEngine;

public enum TerrainType
{
    Normal,
    Mud,
    Road
}

public class GridNode
{
    public int x;
    public int y;

    public Vector3 worldPosition;

    public bool walkable;
    public TerrainType terrainType;
    public int movementCost;

    public int gCost;
    public int hCost;

    public GridNode parent;

    public GameObject visual;

    public int FCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    public GridNode(
        int x,
        int y,
        Vector3 worldPosition,
        bool walkable,
        TerrainType terrainType,
        int movementCost)
    {
        this.x = x;
        this.y = y;
        this.worldPosition = worldPosition;
        this.walkable = walkable;
        this.terrainType = terrainType;
        this.movementCost = movementCost;

        gCost = int.MaxValue;
        hCost = 0;
        parent = null;
    }
}