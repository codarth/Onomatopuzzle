using TileScripts;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class PlayerOverlap : MonoBehaviour
{
    private Tilemap tilemap;
    private WinningTile winningTile;
    private DamageTile damageTile;

    private void Start()
    {
        tilemap = FindFirstObjectByType<Tilemap>();
        if (tilemap == null) Debug.Log("tilemap not set in PlayerOverlap");
        // get winningTile by path

        winningTile = Resources.Load<WinningTile>("Tiles/WinningTile");
        if (winningTile == null) Debug.Log("winningTile not found in PlayerOverlap");
        damageTile = Resources.Load<DamageTile>("Tiles/DamageTile");
        if (damageTile == null) Debug.Log("damageTile not found in PlayerOverlap");
    }

    private void Update()
        {
            Vector3Int playerCellPos = tilemap.WorldToCell(transform.position);
            TileBase tile = tilemap.GetTile(playerCellPos);

            if (tile == winningTile)
            {
                //Debug.Log("Player overlapped the Winning Tile!");
                LoadNextLevel();
            }
            else if (tile == damageTile)
            {
                // Debug.Log("Player overlapped the Damage Tile!");
                DamagePlayer();
            }
        }

        void LoadNextLevel()
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            int nextSceneIndex = currentSceneIndex + 1;

            if (currentSceneIndex < SceneManager.sceneCountInBuildSettings - 1)
            {
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                Debug.Log("No more levels to load. You reached the end of the game!");
            }
        }

        void DamagePlayer()
        {
            // Debug.Log("Player takes damage! Implement health reduction here.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
}