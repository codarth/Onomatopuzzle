using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Grid-based movement component that moves a GameObject across a Tilemap/GridLayout.
/// Decoupled from any specific controller so it can be reused for players, enemies, or tiles.
/// Provides forward movement, jump-up-then-forward, jump-forward arc, and turning left/right.
/// </summary>
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
    [Tooltip("Grid or GridLayout that defines the world-to-cell mapping for movement.")]
    [SerializeField] private GridLayout grid;
    [Tooltip("Tilemap used for collision/ground checks. A tile present is treated as blocked/solid.")]
    [SerializeField] private Tilemap collisionTilemap;

    [Header("Movement Settings")]
    [Tooltip("Seconds to move one tile. Lower is faster. 0 snaps instantly.")]
    [SerializeField, Min(0f)] private float stepMoveTime = 0.12f;
    [Tooltip("Current facing direction in grid units. Typically (1,0) or (-1,0).")]
    [SerializeField] private Vector2Int facing = Vector2Int.right;

    // Properties
    /// <summary>
    /// Current facing direction in grid coordinates.
    /// </summary>
    public Vector2Int Facing => facing;

    /// <summary>
    /// True while a movement sequence or step is being executed.
    /// </summary>
    public bool IsMoving => _isMoving;

    /// <summary>
    /// The cell the object currently occupies (rounded to cell center).
    /// </summary>
    public Vector3Int CurrentCell => grid.WorldToCell(transform.position);

    // Events
    /// <summary>
    /// Invoked when the mover enters a new cell. Parameter is the new cell position.
    /// </summary>
    public event Action<Vector3Int> OnCellChanged;

    // Private state
    private bool _isMoving;

    /// <summary>
    /// Unity message. Ensures required references are located if not set via the Inspector.
    /// </summary>
    void Awake()
    {
        // Find grid if not assigned
        if (!grid)
        {
            grid = FindAnyObjectByType<Grid>();
            if (!grid)
            {
                Debug.LogWarning("GridMover: No Grid found in scene!");
            }
        }
        
        // Find collision tilemap if not assigned
        if (!collisionTilemap)
        {
            // Try to find a tilemap with "collision" in the name (case insensitive)
            Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.InstanceID);
            
            foreach (var tilemap in tilemaps)
            {
                if (tilemap.name.ToLower().Contains("collision"))
                {
                    collisionTilemap = tilemap;
                    break;
                }
            }
            
            // If no collision tilemap found by name, use the first tilemap as fallback
            if (!collisionTilemap && tilemaps.Length > 0)
            {
                collisionTilemap = tilemaps[0];
                Debug.LogWarning($"GridMover: No 'collision' tilemap found, using '{collisionTilemap.name}' as fallback");
            }
        }
    }

    
    void Reset()
    {
        grid = FindObjectsByType<Grid>(FindObjectsSortMode.InstanceID)[0]; // convenience
    }

    void Start()
    {
        SnapToGrid();
    }

    // Original facing flip kept as private for Turn command execution
    private void DoChangeFacing()
    {
        facing = (facing == Vector2Int.right) ? Vector2Int.left : Vector2Int.right;
    }

    // Public wrapper now routes through the sequence system
    /// <summary>
    /// Requests a facing change (left/right flip). The command is queued and executed via the movement sequence system.
    /// </summary>
    /// <returns>True if the command was accepted (not already moving and on valid ground), otherwise false.</returns>
    public bool ChangeFacing()
    {
        return TryExecuteSequence(new MoveStep[] {
            new MoveStep(MoveCommand.Turn)
        });
    }

    /// <summary>
    /// Attempts to move forward along the current facing direction by the specified number of tiles.
    /// </summary>
    /// <param name="distance">Number of tiles to move forward. Must be >= 1. Defaults to 1.</param>
    /// <returns>True if the move sequence was accepted and will run; false if blocked or already moving.</returns>
    public bool TryForward(int distance = 1)
    {
        return TryExecuteSequence(new MoveStep[] {
            new MoveStep(MoveCommand.Forward, distance)
        });
    }

    /// <summary>
    /// Attempts to jump straight up by a number of tiles, then move forward one tile in the current facing.
    /// Useful for climbing ledges or stairs.
    /// </summary>
    /// <param name="height">Number of tiles to jump upward before stepping forward. Must be >= 1.</param>
    /// <returns>True if the move sequence was accepted; false otherwise.</returns>
    public bool TryJumpUpThenForward(int height)
    {
        return TryExecuteSequence(new MoveStep[] {
            new MoveStep(MoveCommand.JumpUp, height)
        });
    }

    /// <summary>
    /// Attempts to jump forward in a shallow arc. First step is up-forward, last step is down-forward.
    /// </summary>
    /// <param name="distance">Total forward tiles to travel. Must be >= 3.</param>
    /// <returns>True if the move sequence was accepted; false otherwise.</returns>
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
    /// <summary>
    /// Attempts to execute a sequence of high-level movement steps.
    /// </summary>
    /// <param name="sequence">Array of MoveStep commands (Forward, JumpUp, JumpForward, Turn) in order.</param>
    /// <returns>True if accepted and started; false if invalid, blocked, or already moving.</returns>
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
        Vector2Int dir = facing;
        List<Vector2Int> steps = new List<Vector2Int>(distance);
        for (int i = 0; i < distance; i++) steps.Add(dir);
        yield return StepSequence(steps, 0f, MovementType.Forward);
    }

    private IEnumerator ExecuteJumpUp(int height)
    {
        List<Vector2Int> steps = new List<Vector2Int>(height + 1);
        for (int i = 0; i < height; i++) steps.Add(Vector2Int.up);
        Vector2Int f = facing;
        steps.Add(f);
        yield return StepSequence(steps, 0f, MovementType.JumpOrArc);
    }

    private IEnumerator ExecuteJumpForward(int distance)
    {
        List<Vector2Int> steps = new List<Vector2Int>(distance);
        Vector2Int f = facing;
        
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

    /// <summary>
    /// Instantly teleports the transform to the center of the given cell without animation.
    /// Triggers OnCellChanged for the destination cell.
    /// </summary>
    /// <param name="cell">Target grid cell to warp to.</param>
    public void WarpToCell(Vector3Int cell)
    {
        transform.position = CellCenterWorld(cell);
        OnCellChanged?.Invoke(cell);
    }

    bool IsBlocked(Vector3Int cell)
    {
        if (collisionTilemap == null) return false;
        return collisionTilemap.HasTile(cell);

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
        if (grid == null) return transform.position;
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
