using System;
using TileScripts;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Explosion : MonoBehaviour
{
    private Tilemap _tilemap;
    public float glorpChance = 0.1f; // 10% chance to spawn a glorp

    private void Start()
    {
        _tilemap = FindFirstObjectByType<Tilemap>();
    }

    public void DoExplosion(Vector3Int playerPosition, Vector2Int facingDirection)
    {
        Vector3Int targetPosition = playerPosition + new Vector3Int(facingDirection.x, facingDirection.y, 0);
        
        TileBase tile = _tilemap.GetTile(targetPosition);
        
        if (tile is DestructibleTile)
        {
            GlorpTile glorpTile = null;
            
            // random float  0-1
            float rand = UnityEngine.Random.Range(0f, 1f);
            // Debug.Log($"Random value for glorp chance: {rand} (glorpChance is {glorpChance})");
            if (rand < glorpChance)
            {
                glorpTile = Resources.Load<GlorpTile>("Tiles/GlorpTile");
                if (glorpTile == null)
                {
                    Debug.Log("GlorpTile not found in Resources/Tiles/GlorpTile");
                }
            }
            _tilemap.SetTile(targetPosition, glorpTile);
        }
    }
}
