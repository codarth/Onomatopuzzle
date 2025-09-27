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

    // New generic movement command types
    public enum MoveCommand { Forward, JumpUp, JumpForward, Turn }

    // Data structure for a single movement step in a sequence
    public struct MoveStep
    {
        public MoveCommand command;
        public int parameter; // distance/height; ignored for Turn

        public MoveStep(MoveCommand cmd, int param = 1)
        {
            command = cmd;
            parameter = param;
        }
    }

    [Header("References")]
    [SerializeField] private GridLayout grid;          // Drag the Grid here
    [SerializeField] private Tilemap collisionTilemap; // Optional: walls / blocked cells

    [Header("Movement")]
    [SerializeField, Min(0f)] private float stepMoveTime = 0.12f; // seconds per tile
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

    // Original facing flip kept as private for Turn command execution
    private void DoChangeFacing()
    {
        _pc.facing = _pc.facing == Vector2Int.right ? Vector2Int.left : Vector2Int.right;
    }

    // Public wrapper now routes through the sequence system
    public bool ChangeFacing()
    {
        return TryExecuteSequence(new MoveStep[] {
            new MoveStep(MoveCommand.Turn)
        });
    }

    /// <summary>Move forward along current facing by the given distance (default 1).</summary>
    public bool TryForward(int distance = 1)
    {
        return TryExecuteSequence(new MoveStep[] {
            new MoveStep(MoveCommand.Forward, distance)
        });
    }

    /// <summary>Jump up by the specified height, then move forward one tile in current facing.</summary>
    public bool TryJumpUpThenForward(int height)
    {
        return TryExecuteSequence(new MoveStep[] {
            new MoveStep(MoveCommand.JumpUp, height)
        });
    }

    /// <summary>Jump forward in an arc; requires a minimum distance of 3.</summary>
    public bool TryJumpForward(int distance)
    {
        if (distance < 3)
        {
            Debug.LogWarning("TryJumpForward requires minimum distance of 3");
            return false;
        }
        return TryExecuteSequence(new MoveStep[] {
            new MoveStep(MoveCommand.JumpForward, distance)
        });
    }



    // Executes a sequence of high-level movement commands
    public bool TryExecuteSequence(MoveStep[] sequence)
    {
        if (_isMoving) return false;
        if (sequence == null || sequence.Length == 0) return false;
        if (!HasGroundBelow()) return false;
        StartCoroutine(ExecuteSequence(sequence));
        return true;
    }

    private IEnumerator ExecuteSequence(MoveStep[] sequence)
    {
        _isMoving = true;
        for (int i = 0; i < sequence.Length; i++)
        {
            yield return ExecuteCommand(sequence[i]);

            // Always check for falling after any move
            if (!HasGroundBelow())
            {
                yield return FallUntilGrounded();
            }
        }
        _isMoving = false;
    }

    private IEnumerator ExecuteCommand(MoveStep step)
    {
        switch (step.command)
        {
            case MoveCommand.Forward:
                yield return ExecuteForward(Mathf.Max(1, step.parameter));
                break;
            case MoveCommand.JumpUp:
                yield return ExecuteJumpUp(Mathf.Max(1, step.parameter));
                break;
            case MoveCommand.JumpForward:
                yield return ExecuteJumpForward(Mathf.Max(1, step.parameter));
                break;
            case MoveCommand.Turn:
                DoChangeFacing();
                // tiny yield to keep coroutine progression consistent
                yield return null;
                break;
            default:
                yield return null;
                break;
        }
    }

    private IEnumerator ExecuteForward(int distance)
    {
        Vector2Int dir = _pc != null ? _pc.facing : Vector2Int.right;
        List<Vector2Int> steps = new List<Vector2Int>(distance);
        for (int i = 0; i < distance; i++) steps.Add(dir);
        yield return StepSequence(steps, 0f, MovementType.Forward);
    }

    private IEnumerator ExecuteJumpUp(int height)
    {
        List<Vector2Int> steps = new List<Vector2Int>(height + 1);
        for (int i = 0; i < height; i++) steps.Add(Vector2Int.up);
        Vector2Int f = _pc != null ? _pc.facing : Vector2Int.right;
        steps.Add(f);
        yield return StepSequence(steps, 0f, MovementType.JumpOrArc);
    }

    private IEnumerator ExecuteJumpForward(int distance)
    {
        List<Vector2Int> steps = new List<Vector2Int>(distance);
        Vector2Int f = _pc != null ? _pc.facing : Vector2Int.right;
        
        // First step: forward-up
        steps.Add(new Vector2Int(f.x, f.y + 1));
        
        // Middle steps: forward only (distance - 2 steps)
        for (int i = 0; i < distance - 2; i++) 
        {
            steps.Add(f);
        }
        
        // Last step: forward-down
        steps.Add(new Vector2Int(f.x, f.y - 1));
        
        yield return StepSequence(steps, 0.5f, MovementType.JumpOrArc);
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
        Debug.Log("executed is tehre something below me");
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

    private IEnumerator MoveOneStep(Vector3Int from, Vector3Int to, float maxArcHeight)
    {
        Vector3 start = CellCenterWorld(from);
        Vector3 end = CellCenterWorld(to);
        float t = 0f;

        // Determine if this is a diagonal step (moves in both x and y)
        Vector3Int step = to - from;
        bool isDiagonal = (Mathf.Abs(step.x) == 1 && Mathf.Abs(step.y) == 1);
        float actualArcHeight = isDiagonal ? Mathf.Max(0f, maxArcHeight) : 0f;

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
                if (actualArcHeight > 0f)
                {
                    float arc = 3f * actualArcHeight * eased * (1f - eased); // simple parabola
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
                // Ignore ground checks during jump/arc. Apply arc only to diagonal steps via MoveOneStep.
                yield return MoveOneStep(cell, next, 0.5f);
                cell = next;
            }

            i++;
        }

        // After jump/arc sequence completes, if we ended midair, fall

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
