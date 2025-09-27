using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridMover : MonoBehaviour
{
    public enum MovementType { Forward, JumpOrArc }

    [Header("References")]
    [SerializeField] private GridLayout grid;          // Drag the Grid here
    [SerializeField] private Tilemap collisionTilemap; // Optional: walls / blocked cells

    [Header("Movement")]
    [SerializeField, Min(0f)] private float stepMoveTime = 0.12f; // seconds per tile
    [SerializeField] private bool allowDiagonal = true;
    [SerializeField] private int moveDistance = 1;
    [SerializeField] private int jumpHeight = 2;
    [SerializeField] private int jumpDistance = 1;
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

    /// <summary>Move forward a number of tiles along the given direction, step-by-step with collision checks.</summary>
    public void TryForward()
    {
        TryForward(_pc != null ? _pc.facing : Vector2Int.right, moveDistance);
    }

    /// <summary>Move forward a number of tiles along an explicit direction, step-by-step with collision checks.</summary>
    public bool TryForward(Vector2Int dir, int tiles)
    {
        if (_isMoving) return false;
        if (tiles <= 0) return false;
        if (dir == Vector2Int.zero) return false;
        if (!HasGroundBelow()) return false;

        // Build steps list
        List<Vector2Int> steps = new List<Vector2Int>();
        for (int i = 0; i < tiles; i++) steps.Add(new Vector2Int(Mathf.Clamp(dir.x, -1, 1), Mathf.Clamp(dir.y, -1, 1)));

        StartCoroutine(StepSequence(steps, 0f, MovementType.Forward));
        return true;
    }

    /// <summary>Jump up by 'up' tiles, then move forward by 'forward' tiles in current facing.</summary>
    public void TryJumpUpThenForward()
    {
        //if (_isMoving) return false;
        //if (jumpHeight <= 0 && jumpDistance <= 0) return false;
        //if (!HasGroundBelow()) return false;

        List<Vector2Int> steps = new List<Vector2Int>();
        for (int i = 0; i < jumpHeight; i++) steps.Add(Vector2Int.up);
        Vector2Int f = _pc != null ? _pc.facing : Vector2Int.right;
        for (int i = 0; i < jumpDistance; i++) steps.Add(new Vector2Int(Mathf.Clamp(f.x, -1, 1), Mathf.Clamp(f.y, -1, 1)));

        StartCoroutine(StepSequence(steps, 0f, MovementType.JumpOrArc));
        //return true;
    }

    /// <summary>Jump in an arc following the delta vector step-by-step. arcHeight affects tween only.</summary>
    public void TryJumpArc(Vector2Int delta, float arcHeight)
    {
        //if (_isMoving) return false;
        //if (delta == Vector2Int.zero) return false;
        //if (!HasGroundBelow()) return false;

        List<Vector2Int> steps = BuildFacingArcPath(delta);
        StartCoroutine(StepSequence(steps, Mathf.Max(0f, arcHeight), MovementType.JumpOrArc));
        //return true;
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

    // Build an arc that goes diagonally up in the facing (assumed horizontal) direction,
    // then diagonally down, attempting to end exactly at delta.
    private List<Vector2Int> BuildFacingArcPath(Vector2Int delta)
    {
        // If facing is vertical, fallback to the original path builder to avoid ambiguous arcs
        if (_pc != null && Mathf.Abs(_pc.facing.y) == 1)
        {
            return BuildChebyshevPath(delta);
        }

        int total = Mathf.Abs(delta.x);
        int sx = Math.Sign(delta.x != 0 ? delta.x : (_pc != null ? _pc.facing.x : 1));
        if (total == 0)
        {
            // No horizontal distance; fallback to original behavior
            return BuildChebyshevPath(delta);
        }

        int desiredDy = delta.y;
        // First half up-diagonals then down-diagonals
        int n1 = (total + desiredDy) / 2; // may clamp below
        if (n1 < 0) n1 = 0;
        if (n1 > total) n1 = total;
        int n2 = total - n1;

        int resY = n1 - n2; // net vertical from diagonal portions
        int resX = sx * total; // net horizontal from diagonal portions

        List<Vector2Int> steps = new List<Vector2Int>(total + 2);
        for (int i = 0; i < n1; i++) steps.Add(new Vector2Int(sx, 1));
        for (int i = 0; i < n2; i++) steps.Add(new Vector2Int(sx, -1));

        // Correct any remainder to exactly match delta
        int remY = desiredDy - resY;
        int remX = delta.x - resX;
        if (remY != 0)
        {
            int sy = Math.Sign(remY);
            for (int i = 0; i < Mathf.Abs(remY); i++) steps.Add(new Vector2Int(0, sy));
        }
        if (remX != 0)
        {
            int sxx = Math.Sign(remX);
            for (int i = 0; i < Mathf.Abs(remX); i++) steps.Add(new Vector2Int(sxx, 0));
        }

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
    
    private bool HasGroundBelow()
    {
        return HasGroundBelowAt(CurrentCell);
    }

    private bool HasGroundBelowAt(Vector3Int cell)
    {
        Vector3Int belowCell = cell + Vector3Int.down;
        return IsBlocked(belowCell); // assuming solid tiles = ground
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
                float eased = Mathf.Clamp01(t); // linear interpolation for constant speed across steps
                Vector3 pos = Vector3.Lerp(start, end, eased);
                if (arcHeight > 0f)
                {
                    float arc = 3f * arcHeight * eased * (1f - eased); // simple parabola
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

    private IEnumerator StepSequence(List<Vector2Int> steps, float arcHeight, MovementType moveType)
    {
        _isMoving = true;
        Vector3Int cell = CurrentCell;

        int i = 0;
        while (i < steps.Count)
        {
            // Normalize step to unit grid step
            Vector2Int step = steps[i];
            Vector2Int unit = new Vector2Int(Mathf.Clamp(step.x, -1, 1), Mathf.Clamp(step.y, -1, 1));
            if (unit == Vector2Int.zero)
            {
                i++;
                continue;
            }

            // If starting in air during a Forward sequence, fall first
            if (moveType == MovementType.Forward && !HasGroundBelowAt(cell))
            {
                yield return FallUntilGrounded();
                cell = CurrentCell;
                continue; // re-evaluate same step after landing
            }

            Vector3Int next = cell + new Vector3Int(unit.x, unit.y, 0);

            // Standard collision checks
            if (!CanStep(cell, unit))
            {
                break; // blocked: stop immediately
            }

            if (moveType == MovementType.Forward)
            {
                // Perform the tween for this single step without requiring ground under the next cell
                yield return MoveOneStep(cell, next, 0f);
                cell = next;

                // After moving, if no ground, fall immediately
                if (!HasGroundBelowAt(cell))
                {
                    yield return FallUntilGrounded();
                    cell = CurrentCell;
                }
            }
            else // JumpOrArc
            {
                // Ignore ground checks during jump/arc
                yield return MoveOneStep(cell, next, arcHeight);
                cell = next;
            }

            i++;
        }

        // After jump/arc sequence completes, if we ended midair, fall
        if (moveType == MovementType.JumpOrArc && !HasGroundBelowAt(cell))
        {
            yield return FallUntilGrounded();
        }

        _isMoving = false;
    }

    private IEnumerator FallUntilGrounded()
    {
        Vector3Int cell = CurrentCell;
        // Fall one cell at a time until grounded
        while (!HasGroundBelowAt(cell))
        {
            Vector3Int down = cell + Vector3Int.down;
            // Safety: if something marked as blocked below, stop
            if (IsBlocked(down)) break;
            yield return MoveOneStep(cell, down, 0f);
            cell = down;
        }
    }
}
