using TileScripts;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using static DataHolder;

public class PlayerOverlap : MonoBehaviour
{
    [SerializeField] private Tilemap _tilemap;
    private WinningTile _winningTile;
    private DamageTile _damageTile;
    private GlorpTile _glorpTile;

    private void Start()
    {
        // _tilemap = FindFirstObjectByType<Tilemap>();
        if (_tilemap == null) Debug.Log("tilemap not set in PlayerOverlap");
        // get winningTile by path

        _winningTile = Resources.Load<WinningTile>("Tiles/WinningTile");
        if (_winningTile == null) Debug.Log("winningTile not found in PlayerOverlap");
        _damageTile = Resources.Load<DamageTile>("Tiles/DamageTile");
        if (_damageTile == null) Debug.Log("damageTile not found in PlayerOverlap");
        _glorpTile = Resources.Load<GlorpTile>("Tiles/GlorpTile");
        if (_glorpTile == null) Debug.Log("glorpTile not found in PlayerOverlap");
    }

    private void Update()
    {
        Vector3Int playerCellPos = _tilemap.WorldToCell(transform.position);
        TileBase tile = _tilemap.GetTile(playerCellPos);

        if (tile == _winningTile)
        {
            //Debug.Log("Player overlapped the Winning Tile!");
            AudioController.Instance.PlaySFX(PlayerController.Instance.gameWinSfx);
            LoadNextLevel();
        }
        else if (tile == _damageTile)
        {
            // Debug.Log("Player overlapped the Damage Tile!");
            AudioController.Instance.PlaySFX(PlayerController.Instance.deathSfx);
            DamagePlayer();
        }
        else if (_glorpTile)
        {
            if (tile == _glorpTile)
            {
                Debug.Log("Player overlapped the Glorp Tile!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                AudioController.Instance.PlaySFX(PlayerController.Instance.glorpSfx);
                _tilemap.SetTile(_tilemap.WorldToCell(transform.position), null);
            }
        }
    }

    void LoadNextLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (currentSceneIndex < SceneManager.sceneCountInBuildSettings - 1)
        {
            CurrentLevel++;
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