using System;
using TileScripts;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Explosion : MonoBehaviour
{
    private Tilemap tilemap;

    private void Start()
    {
        tilemap = FindFirstObjectByType<Tilemap>();
    }

    public void DoExplosion(Vector3Int playerPosition, Vector2Int facingDirection)
    {
        Vector3Int targetPosition = playerPosition + new Vector3Int(facingDirection.x, facingDirection.y, 0);
        
        TileBase tile = tilemap.GetTile(targetPosition);
        
        if (tile is DestructibleTile)
        {
            tilemap.SetTile(targetPosition, null);
        }
    }
}
