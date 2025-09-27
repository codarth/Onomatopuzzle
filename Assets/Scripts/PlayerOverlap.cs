using UnityEngine;
using UnityEngine.SceneManagement;
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
            // load the next level

            LoadNextLevel();
        }
    }

    private void LoadNextLevel()
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
}