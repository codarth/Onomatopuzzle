using UnityEngine;
using UnityEngine.Tilemaps;

namespace TileScripts
{
    [CreateAssetMenu(fileName = "DestructibleTile", menuName = "Tiles/DestructibleTile")]
    public class DestructibleTile : TileBase
    {
        public Sprite sprite;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = sprite;
            tileData.colliderType = Tile.ColliderType.Sprite;
        }
    }
}