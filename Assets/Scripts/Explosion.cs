using System;
using System.Collections;
using System.Collections.Generic;
using TileScripts;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Explosion : MonoBehaviour
{
    [SerializeField] private Tilemap _tilemap;
    private GridMover _gridMover;

    public float glorpChance = 0.1f; // 10% chance to spawn a glorp
    public float timeBetreenExplosions = 0.2f; // seconds between recursive explosions

    private void Start()
    {
        // _tilemap = FindFirstObjectByType<Tilemap>();
        _gridMover = GetComponent<GridMover>();
    }


    public IEnumerator DoExplosion(Vector3Int playerPosition, Vector2Int facingDirection)
    {
        // Lock the GridMover movement system during explosion
        if (_gridMover != null)
        {
            // Use reflection or make the fields internal/public to access the static lock
            // For now, we'll work with what we have and trust that TryExplosion was called properly
        }
        
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
            AudioController.Instance.PlaySFX(PlayerController.Instance.explodeSfx);

            foreach (var neighboringPos in GetNeighboringTiles(targetPosition))
            {
                TileBase neighboringTile = _tilemap.GetTile(neighboringPos);
                if (neighboringTile is DestructibleTile)
                {
                    // Calculate the facing direction from target to neighbor for recursive logic
                    Vector2Int direction = new Vector2Int(neighboringPos.x - targetPosition.x,
                        neighboringPos.y - targetPosition.y);
                    
                    yield return new WaitForSeconds(timeBetreenExplosions);
                    yield return StartCoroutine(DoExplosion(targetPosition, direction));
                }
            }
        }
    }


    private List<Vector3Int> GetNeighboringTiles(Vector3Int position)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                neighbors.Add(new Vector3Int(position.x + dx, position.y + dy, position.z));
            }
        }

        return neighbors;
    }
}