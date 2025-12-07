using UnityEngine;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour
{
    public int gridSize = 100;
    public float roomSize = 30f; // đúng bằng size prefab
    public GameObject roomPrefab;
    public GameObject roadPrefab;

    public enum CellType { Empty, Room, Road }
    private CellType[,] grid;
    private List<Vector2Int> roomList = new List<Vector2Int>();

    void Start()
    {
        Generate();
    }

    void Generate()
    {
        grid = new CellType[gridSize, gridSize];

        // Spawn room start tại trung tâm
        Vector2Int start = new Vector2Int(gridSize / 2, gridSize / 2);
        SpawnRoom(start);
    }

    void SpawnRoom(Vector2Int pos)
    {
        if (grid[pos.x, pos.y] == CellType.Empty)
        {
            grid[pos.x, pos.y] = CellType.Room;
            roomList.Add(pos);
        }
    }

}
