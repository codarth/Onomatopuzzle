
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

    [Header("Explosion Settings")]
    public TileBase animatedExplosionTile; // Drag your BoomTile_Animation asset here
    public float explosionAnimationDuration = 1.8f; // Duration of one animation cycle (9 frames at 5 fps = 1.8 seconds)
    
    public float glorpChance = 0.1f; // 10% chance to spawn a glorp
    public float timeBetreenExplosions = 0.2f; // seconds between recursive explosions

    private void Start()
    {
        // _tilemap = FindFirstObjectByType<Tilemap>();
        _gridMover = GetComponent<GridMover>();
        
        // Load the animated tile from Resources if not assigned
        if (animatedExplosionTile == null)
        {
            animatedExplosionTile = Resources.Load<TileBase>("Tiles/BoomTile_Animation");
            if (animatedExplosionTile == null)
            {
                Debug.LogError("BoomTile_Animation not found in Resources/Tiles/");
            }
        }
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
            // Start the asynchronous explosion sequence
            StartCoroutine(ExplodeTileSequence(targetPosition));

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
    
    private IEnumerator ExplodeTileSequence(Vector3Int targetPosition)
    {
        Debug.Log($"Starting explosion sequence at {targetPosition}");
        
        // Step 1: Replace tile with animated explosion tile
        _tilemap.SetTile(targetPosition, animatedExplosionTile);
        
        // Play explosion sound with null checks
        if (AudioController.Instance != null && PlayerController.Instance != null && PlayerController.Instance.explodeSfx != null)
        {
            AudioController.Instance.PlaySFX(PlayerController.Instance.explodeSfx);
        }
        else
        {
            Debug.LogWarning("Could not play explosion sound - one of the required components is null");
        }
        
        // Force tilemap refresh to ensure the tile is properly set
        _tilemap.RefreshTile(targetPosition);
        
        Debug.Log($"Set animated tile at {targetPosition}, waiting {explosionAnimationDuration} seconds");
        
        // Step 2: Wait for the duration of 1 animation loop
        yield return new WaitForSeconds(explosionAnimationDuration);
        
        Debug.Log($"Animation duration complete, replacing tile at {targetPosition}");
        
        // Step 3: Replace the tile with a glorp or null tile
        TileBase finalTile = null;
        
        // Random chance for glorp
        float rand = UnityEngine.Random.Range(0f, 1f);
        if (rand < glorpChance)
        {
            finalTile = Resources.Load<GlorpTile>("Tiles/GlorpTile");
            if (finalTile == null)
            {
                Debug.Log("GlorpTile not found in Resources/Tiles/GlorpTile");
            }
            else
            {
                Debug.Log($"Placing glorp tile at {targetPosition}");
            }
        }
        else
        {
            Debug.Log($"Removing tile at {targetPosition}");
        }
        
        // Force remove the animated tile first, then set the final tile
        _tilemap.SetTile(targetPosition, null);
        _tilemap.RefreshTile(targetPosition);
        
        if (finalTile != null)
        {
            _tilemap.SetTile(targetPosition, finalTile);
        }
        
        _tilemap.RefreshTile(targetPosition);
        
        Debug.Log($"Explosion sequence complete at {targetPosition}");
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