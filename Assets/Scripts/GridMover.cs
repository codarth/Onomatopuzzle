using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
    [SerializeField] private int speed = 1;
    private PlayerController _pc;

    private bool _isMoving;
    public bool IsMoving => _isMoving;
    public Vector3Int CurrentCell => grid.WorldToCell(transform.position);

    /// <summary>Raised after the object arrives on a new cell.</summary>
    public event Action<Vector3Int> OnCellChanged;

    void Awake()
    {
        // Find grid if not assigned
        if (!grid)
        {
            grid = FindObjectOfType<Grid>();
            if (!grid)
            {
                Debug.LogWarning("GridMover: No Grid found in scene!");
            }
        }
        
        // Find collision tilemap if not assigned
        if (collisionTilemap) return;
        
        // Try to find a tilemap with "collision" in the name (case insensitive)
        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.InstanceID);
        Debug.Log(tilemaps.Length);

        collisionTilemap = FindObjectsByType<Tilemap>(FindObjectsSortMode.InstanceID).First(); 
        
        // If no collision tilemap found by name, could optionally use the first tilemap
        // or leave it null for no collision checking
    }
    
    void Reset()
    {
        grid = FindObjectsByType<Grid>(FindObjectsSortMode.InstanceID)[0]; // convenience
    }

    void Start()
    {
        SnapToGrid();
        _pc = GetComponent<PlayerController>();
    }

    public void ChangeFacing()
    {
        _pc.facing = _pc.facing == Vector2Int.right ? Vector2Int.left : Vector2Int.right;
    }

    /// <summary>Attempts to move by one cell in the given direction (grid units). Backward compatible wrapper.</summary>
    public bool TryMove(Vector2Int dir)
    {
        if (_isMoving) return false;
        if (dir == Vector2Int.zero) return false;
        if (!allowDiagonal && Mathf.Abs(dir.x) == 1 && Mathf.Abs(dir.y) == 1) return false;

        // Single-step forward in provided direction
        return TryForward(dir, 1);
    }

    /// <summary>Attempts to jump 3 tiles up then 1 tile in facing direction. Backward compatible wrapper.</summary>
    public bool TryJump(Vector2Int facingDirection)
    {
        // Preserve old API: ignore argument magnitude, use current facing and fixed distances
        return TryJumpUpThenForward(3, 1);
    }

    /// <summary>Move forward a number of tiles along the given direction, step-by-step with collision checks.</summary>
    public bool TryForward()
    {
        return TryForward(_pc != null ? _pc.facing : Vector2Int.right, speed);
    }

    /// <summary>Move forward a number of tiles along an explicit direction, step-by-step with collision checks.</summary>
    public bool TryForward(Vector2Int dir, int tiles)
    {
        if (_isMoving) return false;
        if (tiles <= 0) return false;
        if (dir == Vector2Int.zero) return false;

        // Build steps list
        List<Vector2Int> steps = new List<Vector2Int>();
        for (int i = 0; i < tiles; i++) steps.Add(new Vector2Int(Mathf.Clamp(dir.x, -1, 1), Mathf.Clamp(dir.y, -1, 1)));

        StartCoroutine(StepSequence(steps, 0f));
        return true;
    }

    /// <summary>Jump up by 'up' tiles, then move forward by 'forward' tiles in current facing.</summary>
    public bool TryJumpUpThenForward(int up, int forward)
    {
        if (_isMoving) return false;
        if (up <= 0 && forward <= 0) return false;

        List<Vector2Int> steps = new List<Vector2Int>();
        for (int i = 0; i < up; i++) steps.Add(Vector2Int.up);
        Vector2Int f = _pc != null ? _pc.facing : Vector2Int.right;
        for (int i = 0; i < forward; i++) steps.Add(new Vector2Int(Mathf.Clamp(f.x, -1, 1), Mathf.Clamp(f.y, -1, 1)));

        StartCoroutine(StepSequence(steps, 0f));
        return true;
    }

    /// <summary>Jump in an arc following the delta vector step-by-step. arcHeight affects tween only.</summary>
    public bool TryJumpArc(Vector2Int delta, float arcHeight)
    {
        if (_isMoving) return false;
        if (delta == Vector2Int.zero) return false;

        List<Vector2Int> steps = BuildChebyshevPath(delta);
        StartCoroutine(StepSequence(steps, Mathf.Max(0f, arcHeight)));
        return true;
    }

    private List<Vector2Int> BuildChebyshevPath(Vector2Int delta)
    {
        List<Vector2Int> steps = new List<Vector2Int>();
        int sx = Math.Sign(delta.x);
        int sy = Math.Sign(delta.y);
        int ax = Mathf.Abs(delta.x);
        int ay = Mathf.Abs(delta.y);
        int diag = Math.Min(ax, ay);
        for (int i = 0; i < diag; i++) steps.Add(new Vector2Int(sx, sy));
        for (int i = 0; i < ax - diag; i++) steps.Add(new Vector2Int(sx, 0));
        for (int i = 0; i < ay - diag; i++) steps.Add(new Vector2Int(0, sy));
        return steps;
    }

    /// <summary>Immediately snaps to the center of the current cell.</summary>
    private void SnapToGrid()
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
        Debug.Log(collisionTilemap);
        Debug.Log(collisionTilemap.HasTile(cell));
        // Treat any tile present in the collisionTilemap as blocked.
        return (!collisionTilemap.IsUnityNull() && collisionTilemap.HasTile(cell));
    }

    Vector3 CellCenterWorld(Vector3Int cell)
    {
        // Center of the cell using Grid cell size
        return grid.CellToWorld(cell) + grid.cellSize / 2f;
    }

    // Strict corner rule and per-step validation
    private bool CanStep(Vector3Int from, Vector2Int step)
    {
        Vector3Int target = from + new Vector3Int(step.x, step.y, 0);
        int ax = Mathf.Abs(step.x);
        int ay = Mathf.Abs(step.y);
        if (ax == 1 && ay == 1)
        {
            // Diagonal: block if target OR either adjacent orthogonal is blocked
            Vector3Int orthoA = from + new Vector3Int(step.x, 0, 0);
            Vector3Int orthoB = from + new Vector3Int(0, step.y, 0);
            if (IsBlocked(target) || IsBlocked(orthoA) || IsBlocked(orthoB)) return false;
            return true;
        }
        else
        {
            // Cardinal (or zero/invalid)
            if (ax + ay == 0) return false; // no movement
            return !IsBlocked(target);
        }
    }

    private IEnumerator MoveOneStep(Vector3Int from, Vector3Int to, float arcHeight)
    {
        Vector3 start = CellCenterWorld(from);
        Vector3 end = CellCenterWorld(to);
        float t = 0f;

        if (stepMoveTime <= 0f)
        {
            transform.position = end;
        }
        else
        {
            while (t < 1f)
            {
                t += Time.deltaTime / stepMoveTime;
                float eased = Mathf.SmoothStep(0f, 1f, t);
                Vector3 pos = Vector3.Lerp(start, end, eased);
                if (arcHeight > 0f)
                {
                    float arc = 4f * arcHeight * eased * (1f - eased); // simple parabola
                    pos += Vector3.up * arc;
                }
                transform.position = pos;
                yield return null;
            }
            transform.position = end;
        }

        // Snap to exact center to avoid drift and notify
        transform.position = end;
        OnCellChanged?.Invoke(to);
    }

    private IEnumerator StepSequence(List<Vector2Int> steps, float arcHeight)
    {
        _isMoving = true;
        Vector3Int cell = CurrentCell;

        foreach (var step in steps)
        {
            // Normalize step to unit grid step
            Vector2Int unit = new Vector2Int(Mathf.Clamp(step.x, -1, 1), Mathf.Clamp(step.y, -1, 1));
            if (unit == Vector2Int.zero)
            {
                continue;
            }

            Vector3Int next = cell + new Vector3Int(unit.x, unit.y, 0);
            if (!CanStep(cell, unit))
            {
                break; // blocked: stop immediately
            }

            // Perform the tween for this single step
            yield return MoveOneStep(cell, next, arcHeight);
            cell = next;
        }

        _isMoving = false;
    }
}
