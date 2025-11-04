using UnityEngine;
using UnityEngine.Tilemaps;

public class TileCoordinateDebugger : MonoBehaviour
{
    public Tilemap targetTilemap;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = targetTilemap.WorldToCell(worldPos);
            Debug.Log($"Tile Coordinate: {cellPos}");
        }
    }
}
