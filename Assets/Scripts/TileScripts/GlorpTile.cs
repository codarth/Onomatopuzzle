using UnityEngine;
using UnityEngine.Tilemaps;

namespace TileScripts
{
    [CreateAssetMenu(fileName = "GlorpTile", menuName = "Tiles/GlorpTile")]
    public class GlorpTile : TileBase
    {
        public Sprite sprite;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = sprite;
            tileData.colliderType = Tile.ColliderType.None;
        }
    }
}