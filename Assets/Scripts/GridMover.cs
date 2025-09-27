using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridLayout grid;          // Drag the Grid here
    [SerializeField] private Tilemap collisionTilemap; // Optional: walls / blocked cells

    [Header("Movement")]
    [SerializeField, Min(0f)] private float stepMoveTime = 0.12f; // seconds per tile
    [SerializeField] private bool allowDiagonal = false;

    public bool IsMoving { get; private set; }
    public Vector3Int CurrentCell => grid.WorldToCell(transform.position);

    /// <summary>Raised after the object arrives on a new cell.</summary>
    public event Action<Vector3Int> OnCellChanged;

    void Reset()
    {
        grid = FindObjectOfType<Grid>(); // convenience
    }

    void Start()
    {
        SnapToGrid();
    }

    /// <summary>Attempts to move by one cell in the given direction (grid units).</summary>
    public bool TryMove(Vector2Int dir)
    {
        if (IsMoving) return false;
        if (dir == Vector2Int.zero) return false;
        if (!allowDiagonal && Mathf.Abs(dir.x) == 1 && Mathf.Abs(dir.y) == 1) return false;

        Vector3Int targetCell = CurrentCell + new Vector3Int(dir.x, dir.y, 0);
        if (IsBlocked(targetCell)) return false;

        StartCoroutine(MoveToCell(targetCell));
        return true;
    }

    /// <summary>Immediately snaps to the center of the current cell.</summary>
    public void SnapToGrid()
    {
        Vector3Int cell = grid.WorldToCell(transform.position);
        transform.position = CellCenterWorld(cell);
    }

    /// <summary>Teleports to a specific cell (no tween).</summary>
    public void WarpToCell(Vector3Int cell)
    {
        transform.position = CellCenterWorld(cell);
        OnCellChanged?.Invoke(cell);
    }

    bool IsBlocked(Vector3Int cell)
    {
        // Treat any tile present in the collisionTilemap as blocked.
        if (collisionTilemap != null && collisionTilemap.HasTile(cell)) return true;
        return false;
    }

    Vector3 CellCenterWorld(Vector3Int cell)
    {
        // Center of the cell using Grid cell size
        return grid.CellToWorld(cell) + (Vector3)grid.cellSize / 2f;
    }

    IEnumerator MoveToCell(Vector3Int targetCell)
    {
        IsMoving = true;

        Vector3 start = transform.position;
        Vector3 end = CellCenterWorld(targetCell);
        float t = 0f;

        // Handle instantaneous move
        if (stepMoveTime <= 0f)
        {
            transform.position = end;
        }
        else
        {
            while (t < 1f)
            {
                t += Time.deltaTime / stepMoveTime;
                transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            transform.position = end;
        }

        IsMoving = false;
        OnCellChanged?.Invoke(targetCell);
    }
}
