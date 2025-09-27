using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "WinningTile", menuName = "Tiles/WinningTile")]
public class WinningTile : TileBase
{
    public Sprite sprite;
    
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = sprite;
        tileData.colliderType = Tile.ColliderType.None;
    } 
}
