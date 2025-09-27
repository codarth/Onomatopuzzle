using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerOverlap : MonoBehaviour
{
    private Tilemap tilemap;                
    public WinningTile winningTile;        

    private void Start()
    {
        tilemap = FindFirstObjectByType<Tilemap>();
        if (tilemap != null)
        {
            Debug.Log("Tilemap found: " + tilemap.name);
        }
        else
        {
            Debug.LogWarning("No Tilemap found in the scene.");
        }
    }

    private void Update()
    {
        Vector3Int playerCellPos = tilemap.WorldToCell(transform.position);
        TileBase tile = tilemap.GetTile(playerCellPos);

        if (tile == winningTile)
        {
            Debug.Log("Player overlapped the Winning Tile!");
        }
    }
}