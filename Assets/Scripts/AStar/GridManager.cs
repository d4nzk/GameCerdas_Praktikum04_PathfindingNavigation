using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid")]
    public int width = 10;
    public int height = 10;
    public float cellSize = 1f;

    [Header("Obstacle Detection")]
    public LayerMask obstacleMask;

    [Header("Terrain Layers")]
    public LayerMask mudMask;
    public LayerMask roadMask;
    public int normalCost = 10;
    public int mudCost = 30;
    public int roadCost = 5;

    [Header("Visualization")]
    public bool showGrid = true;
    public float visualHeight = 0.05f;
    public Color normalColor = Color.white;
    public Color mudColor = new Color(0.55f, 0.3f, 0.1f);
    public Color roadColor = Color.gray;

    [System.NonSerialized]
    public GridNode[,] grid;

    private Transform visualParent;

    private void Awake()
    {
        CreateGrid();
    }

    public void CreateGrid()
    {
        grid = new GridNode[width, height];

        GameObject holder = new GameObject("GridVisuals");
        holder.transform.SetParent(transform);
        visualParent = holder.transform;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 worldPosition = GetWorldPosition(x, y);

                bool blocked = Physics.CheckBox(
                    worldPosition + Vector3.up * 0.5f,
                    new Vector3(
                        cellSize * 0.4f,
                        0.45f,
                        cellSize * 0.4f),
                    Quaternion.identity,
                    obstacleMask
                );

                bool walkable = !blocked;
                TerrainType terrainType = GetTerrainType(worldPosition);

                GridNode node = new GridNode(
                    x,
                    y,
                    worldPosition,
                    walkable,
                    terrainType,
                    GetTerrainCost(terrainType)
                );

                grid[x, y] = node;

                if (showGrid)
                {
                    CreateVisual(node);
                }
            }
        }
    }

    private Vector3 GetWorldPosition(int x, int y)
    {
        return transform.position +
               new Vector3(
                   x * cellSize,
                   0f,
                   y * cellSize
               );
    }

    private void CreateVisual(GridNode node)
    {
        GameObject tile =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        tile.name = $"Node_{node.x}_{node.y}";

        tile.transform.SetParent(visualParent);

        tile.transform.position =
            node.worldPosition +
            Vector3.down * (visualHeight * 0.5f);

        tile.transform.localScale =
            new Vector3(
                cellSize * 0.9f,
                visualHeight,
                cellSize * 0.9f
            );

        Collider col = tile.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        node.visual = tile;

        SetNodeColor(
            node,
            node.walkable ? GetTerrainColor(node.terrainType) : Color.black
        );
    }

    private TerrainType GetTerrainType(Vector3 worldPosition)
    {
        Vector3 terrainCheckPosition =
            worldPosition + Vector3.down * 0.5f;

        if (Physics.CheckBox(
                terrainCheckPosition,
                new Vector3(cellSize * 0.4f, 0.6f, cellSize * 0.4f),
                Quaternion.identity,
                roadMask))
        {
            return TerrainType.Road;
        }

        if (Physics.CheckBox(
            terrainCheckPosition,
            new Vector3(cellSize * 0.4f, 0.6f, cellSize * 0.4f),
                Quaternion.identity,
                mudMask))
        {
            return TerrainType.Mud;
        }

        return TerrainType.Normal;
    }

    private int GetTerrainCost(TerrainType terrainType)
    {
        switch (terrainType)
        {
            case TerrainType.Mud:
                return mudCost;
            case TerrainType.Road:
                return roadCost;
            default:
                return normalCost;
        }
    }

    public int GetMinimumTerrainCost()
    {
        return Mathf.Max(
            1,
            Mathf.Min(normalCost, Mathf.Min(mudCost, roadCost))
        );
    }

    private Color GetTerrainColor(TerrainType terrainType)
    {
        switch (terrainType)
        {
            case TerrainType.Mud:
                return mudColor;
            case TerrainType.Road:
                return roadColor;
            default:
                return normalColor;
        }
    }

    public GridNode NodeFromWorldPosition(Vector3 worldPosition)
    {
        Vector3 local =
            worldPosition - transform.position;

        int x = Mathf.RoundToInt(local.x / cellSize);
        int y = Mathf.RoundToInt(local.z / cellSize);

        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);

        return grid[x, y];
    }

    public List<GridNode> GetNeighbors(GridNode node)
    {
        List<GridNode> neighbors =
            new List<GridNode>();

        TryAddNeighbor(node.x + 1, node.y, neighbors);
        TryAddNeighbor(node.x - 1, node.y, neighbors);
        TryAddNeighbor(node.x, node.y + 1, neighbors);
        TryAddNeighbor(node.x, node.y - 1, neighbors);
        TryAddDiagonalNeighbor(
            node.x + 1,
            node.y + 1,
            node,
            neighbors
        );
        TryAddDiagonalNeighbor(
            node.x + 1,
            node.y - 1,
            node,
            neighbors
        );
        TryAddDiagonalNeighbor(
            node.x - 1,
            node.y + 1,
            node,
            neighbors
        );
        TryAddDiagonalNeighbor(
            node.x - 1,
            node.y - 1,
            node,
            neighbors
        );

        return neighbors;
    }

    private void TryAddNeighbor(
        int x,
        int y,
        List<GridNode> neighbors)
    {
        if (x < 0 || x >= width ||
            y < 0 || y >= height)
        {
            return;
        }

        neighbors.Add(grid[x, y]);
    }

    private void TryAddDiagonalNeighbor(
        int x,
        int y,
        GridNode node,
        List<GridNode> neighbors)
    {
        int horizontalX = x;
        int horizontalY = node.y;
        int verticalX = node.x;
        int verticalY = y;

        if (x < 0 || x >= width ||
            y < 0 || y >= height ||
            !grid[horizontalX, horizontalY].walkable ||
            !grid[verticalX, verticalY].walkable)
        {
            return;
        }

        neighbors.Add(grid[x, y]);
    }

    public void ResetSearchData()
    {
        foreach (GridNode node in grid)
        {
            node.gCost = int.MaxValue;
            node.hCost = 0;
            node.parent = null;

            if (showGrid)
            {
                SetNodeColor(
                    node,
                    node.walkable ?
                    GetTerrainColor(node.terrainType) :
                    Color.black
                );
            }
        }
    }

    public void SetNodeColor(
        GridNode node,
        Color color)
    {
        if (node.visual == null)
        {
            return;
        }

        Renderer renderer =
            node.visual.GetComponent<Renderer>();

        renderer.material.color = color;
    }
}