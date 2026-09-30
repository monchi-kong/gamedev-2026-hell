using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "BreakableBlock", menuName = "Tiles/Breakable Block")]
public class BreakableTile : Tile
{
    public bool Break(Tilemap map, Vector3Int cell)
    {
        if (map.GetTile(cell) != this) return false;
        map.SetTile(cell, null);
        var tileCollider = map.GetComponent<TilemapCollider2D>();
        if (tileCollider != null) tileCollider.ProcessTilemapChanges();
        return true;
    }
}
