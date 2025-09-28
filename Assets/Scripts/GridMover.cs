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

    // Direction enums for inspector-friendly selection
    public enum FacingDirection { Right, Left }
    public enum MovementDirection { Up, Down, Left, Right }

    [Header("References")]
    [Tooltip("Grid or GridLayout that defines the world-to-cell mapping for movement.")]
    [SerializeField] public GridLayout grid;
    
    [Header("Movement Settings")]
    [Tooltip("Seconds to move one tile. Lower is faster. 0 snaps instantly.")]
    [SerializeField, Min(0f)] private float stepMoveTime = 0.12f;
    [Tooltip("Current facing direction.")]
    [SerializeField] private FacingDirection facingDirection = FacingDirection.Right;
    

    // Properties
    /// <summary>
    /// Current facing direction in grid coordinates.
    /// </summary>
    public Vector2Int Facing => GetFacingVector();

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

    // Global movement lock: only one GridMover can move at a time across the scene
    private static bool _anyGridMoverMoving = false;
    private static GridMover _currentMovingGridMover = null;
    public static bool IsAnyGridMoverMoving => _anyGridMoverMoving;
    
    // Private list to cache tilemaps with colliders
    private Tilemap[] collisionTilemaps;

    // Moving platform parenting
    [Header("Platform Carry Settings")]
    [Tooltip("If enabled, objects with PlayerController will parent to PlatformController underfoot after moves.")]
    [SerializeField] private bool enablePlatformParenting = true;
    private Transform _currentPlatformParent = null;
    private bool _isPlayer = false;

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

        RefreshCollisionTilemaps();
        _isPlayer = GetComponent<PlayerController>() != null;
    }
    
    private void RefreshCollisionTilemaps()
    {
        // Find all GameObjects with both Tilemap and TilemapCollider2D
        TilemapCollider2D[] colliders = FindObjectsByType<TilemapCollider2D>(FindObjectsSortMode.None);
        collisionTilemaps = new Tilemap[colliders.Length];
        
        for (int i = 0; i < colliders.Length; i++)
        {
            collisionTilemaps[i] = colliders[i].GetComponent<Tilemap>();
        }
        
        Debug.Log($"GridMover: Found {collisionTilemaps.Length} tilemaps with colliders");
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
        facingDirection = (facingDirection == FacingDirection.Right) ? FacingDirection.Left : FacingDirection.Right;
        FlipSprite(facingDirection);
    }

    // Helper to convert enum to vector for internal use
    private Vector2Int GetFacingVector()
    {
        return facingDirection == FacingDirection.Right ? Vector2Int.right : Vector2Int.left;
    }

    // Static helper to convert MovementDirection enum to Vector2Int
    public static Vector2Int GetDirectionVector(MovementDirection dir)
    {
        switch (dir)
        {
            case MovementDirection.Up: return Vector2Int.up;
            case MovementDirection.Down: return Vector2Int.down;
            case MovementDirection.Left: return Vector2Int.left;
            case MovementDirection.Right: return Vector2Int.right;
            default: return Vector2Int.zero;
        }
    }

    private void FlipSprite(FacingDirection facingDirection)
    {
        if (facingDirection.Equals(FacingDirection.Left))
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
    }

    /// <summary>
    /// Requests a facing change (left/right flip). The command is queued and executed via the movement sequence system.
    /// </summary>
    /// <returns>True if the command was accepted (not already moving and on valid ground), otherwise false.</returns>
    public bool ChangeFacing()
    {
        if (_anyGridMoverMoving && _currentMovingGridMover != this) return false;
        if (!HasGroundBelow()) return false;
        TryExecuteSequence(new MoveStep[] {
            new MoveStep(MoveCommand.Turn)
        });
        return true;
    }


    /// <summary>
    /// Attempts to move forward along the current facing direction by the specified number of tiles.
    /// </summary>
    /// <param name="distance">Number of tiles to move forward. Must be >= 1. Defaults to 1.</param>
    /// <returns>True if the move sequence was accepted and will run; false if blocked or already moving.</returns>
    public bool TryForward(int distance = 1)
    {
        if (_anyGridMoverMoving && _currentMovingGridMover != this) return false;
        if (!HasGroundBelow()) return false;
        
        // Check if the first forward step is possible
        Vector2Int facingDir = GetFacingVector();
        Vector3Int currentCell = CurrentCell;
        Vector3Int firstStep = currentCell + new Vector3Int(facingDir.x, facingDir.y, 0);
        
        if (!CanStep(currentCell, facingDir))
        {
            return false;
        }
        
        TryExecuteSequence(new MoveStep[] {
            new MoveStep(MoveCommand.Forward, distance)
        });
        return true;
    }



    /// <summary>
    /// Attempts to jump straight up by a number of tiles, then move forward one tile in the current facing.
    /// Useful for climbing ledges or stairs.
    /// </summary>
    /// <param name="height">Number of tiles to jump upward before stepping forward. Must be >= 1.</param>
    /// <returns>True if the move sequence was accepted; false otherwise.</returns>
    public bool TryJumpUpThenForward(int height)
    {
        if (_anyGridMoverMoving && _currentMovingGridMover != this) return false;
        if (!HasGroundBelow()) return false;
        
        // Check if the first upward step is possible
        Vector3Int currentCell = CurrentCell;
        Vector3Int firstStep = currentCell + Vector3Int.up;
        
        if (!CanStep(currentCell, Vector2Int.up))
        {
            return false;
        }
        
        TryExecuteSequence(new MoveStep[] {
            new MoveStep(MoveCommand.JumpUp, height)
        });
        return true;
    }



    /// <summary>
    /// Attempts to jump forward in a shallow arc. First step is up-forward, last step is down-forward.
    /// </summary>
    /// <param name="distance">Total forward tiles to travel. Must be >= 3.</param>
    /// <returns>True if the move sequence was accepted; false otherwise.</returns>
    public bool TryJumpForward(int distance)
    {
        if (_anyGridMoverMoving && _currentMovingGridMover != this) return false;
        if (!HasGroundBelow()) return false;
        if (distance < 3)
        {
            Debug.LogWarning("TryJumpForward requires minimum distance of 3");
            return false;
        }
        
        // Check if the first diagonal up-forward step is possible
        Vector2Int facingDir = GetFacingVector();
        Vector2Int firstStep = new Vector2Int(facingDir.x, 1); // forward + up
        Vector3Int currentCell = CurrentCell;
        
        if (!CanStep(currentCell, firstStep))
        {
            return false;
        }
        
        TryExecuteSequence(new MoveStep[] {
            new MoveStep(MoveCommand.JumpForward, distance)
        });
        return true;
    }



    /// <summary>
    /// Simple directional movement without gravity or collision checks.
    /// Moves exactly 'distance' tiles in the given direction, using existing MoveOneStep timing.
    /// Intended for platforms or perfectly placed objects.
    /// </summary>
    /// <param name="direction">Desired direction. Will be normalized to a cardinal unit (up/down/left/right).</param>
    /// <param name="distance">Number of tiles to move. Must be >= 1.</param>
    /// <returns>True if movement started, false if already moving or invalid args.</returns>
    public void TryMoveInDirection(Vector2Int direction, int distance)
    {
        if (_anyGridMoverMoving && _currentMovingGridMover != this) return;
        if (_isMoving) return;
        if (distance <= 0) return;

        // Normalize to cardinal unit direction
        Vector2Int unit = new Vector2Int(Mathf.Clamp(direction.x, -1, 1), Mathf.Clamp(direction.y, -1, 1));
        int sum = Mathf.Abs(unit.x) + Mathf.Abs(unit.y);
        if (sum != 1)
        {
            // Pick dominant axis if diagonal or zero provided
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
                unit = new Vector2Int(direction.x == 0 ? 0 : (int)Mathf.Sign(direction.x), 0);
            else
                unit = new Vector2Int(0, direction.y == 0 ? 0 : (int)Mathf.Sign(direction.y));

            // If still zero, reject
            if (unit == Vector2Int.zero) return;
        }

        StartCoroutine(MoveInDirection(unit, distance));
    }

    // Coroutine that performs simple directional movement without any checks
    private IEnumerator MoveInDirection(Vector2Int unit, int distance)
    {
        _anyGridMoverMoving = true;
        _currentMovingGridMover = this;
        _isMoving = true;
        Vector3Int cell = CurrentCell;
        for (int i = 0; i < distance; i++)
        {
            Vector3Int next = cell + new Vector3Int(unit.x, unit.y, 0);
            yield return MoveOneStep(cell, next, 0f);
            cell = next;
        }
        _isMoving = false;
        UpdatePlatformParentIfNeeded();
        _anyGridMoverMoving = false;
        _currentMovingGridMover = null;
    }


    // Executes a sequence of high-level movement commands
    /// <summary>
    /// Attempts to execute a sequence of high-level movement steps.
    /// </summary>
    /// <param name="sequence">Array of MoveStep commands (Forward, JumpUp, JumpForward, Turn) in order.</param>
    /// <returns>True if accepted and started; false if invalid, blocked, or already moving.</returns>
    public bool TryExecuteSequence(MoveStep[] sequence)
    {
        if (_anyGridMoverMoving && _currentMovingGridMover != this) return false;
        if (_isMoving) return false;
        if (sequence == null || sequence.Length == 0) return false;
        if (!HasGroundBelow()) return false;
        StartCoroutine(ExecuteSequence(sequence));
        return true;
    }


    private IEnumerator ExecuteSequence(MoveStep[] sequence)
    {
        _anyGridMoverMoving = true;
        _currentMovingGridMover = this;
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
        UpdatePlatformParentIfNeeded();
        _anyGridMoverMoving = false;
        _currentMovingGridMover = null;
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
        Vector2Int dir = GetFacingVector();
        List<Vector2Int> steps = new List<Vector2Int>(distance);
        for (int i = 0; i < distance; i++) steps.Add(dir);
        yield return StepSequence(steps, 0f, MovementType.Forward);
    }

    private IEnumerator ExecuteJumpUp(int height)
    {
        List<Vector2Int> steps = new List<Vector2Int>(height + 1);
        for (int i = 0; i < height; i++) steps.Add(Vector2Int.up);
        Vector2Int f = GetFacingVector();
        steps.Add(f);
        yield return StepSequence(steps, 0f, MovementType.JumpOrArc);
    }

    private IEnumerator ExecuteJumpForward(int distance)
    {
        List<Vector2Int> steps = new List<Vector2Int>(distance);
        Vector2Int f = GetFacingVector();
        
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

    bool IsBlocked(Vector3Int cell)
    {
        Vector3 worldPos = grid.CellToWorld(cell) + grid.cellSize / 2f;
    
        // Use physics overlap to check for colliders at this position
        Collider2D collider = Physics2D.OverlapPoint(worldPos);
    
        // Accept any solid collider, not just TilemapCollider2D
        if (collider != null)
        {
            // Check if it's a trigger - if so, don't consider it blocking
            if (collider.isTrigger) return false;
        
            // Accept any non-trigger collider as blocking
            return true;
        }
    
        return false;

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
        if (grid == null) return transform.position;
        
        // Check if this is a platform (you could add a flag or detect by component)
        PlatformController platform = GetComponent<PlatformController>();
        if (platform != null)
        {
            // For platforms, align to grid corner instead of center
            return grid.CellToWorld(cell);
        }
        
        // For regular objects (like player), center in cell
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
        // After landing, update platform parenting (player should stick to platform if any)
        UpdatePlatformParentIfNeeded();
    }

    // Detect platform underfoot and set/unset parent accordingly for players
    private void UpdatePlatformParentIfNeeded()
    {
        if (!enablePlatformParenting) return;
        if (!_isPlayer) return;
    
        Debug.Log("UpdatePlatformParentIfNeeded called");
        Transform newParent = null;

        // Check the cell directly below the player, same as ground detection
        Vector3Int currentCell = CurrentCell;
        Vector3Int belowCell = currentCell + Vector3Int.down;
        Vector3 worldPos = grid.CellToWorld(belowCell) + grid.cellSize / 2f;
    
        Debug.Log($"Checking for platform below player at cell: {belowCell}, world pos: {worldPos}");
    
        // Use the same detection method as IsBlocked but look for platforms instead
        Collider2D collider = Physics2D.OverlapPoint(worldPos);
    
        if (collider != null)
        {
            Debug.Log($"Found collider below: {collider.name}");
        
            // Check if this collider belongs to a platform
            PlatformController plat = collider.GetComponentInParent<PlatformController>();
            if (plat != null)
            {
                Debug.Log($"Found PlatformController on: {plat.name}");
                newParent = plat.transform;
            }
            else
            {
                Debug.Log($"No PlatformController found on {collider.name}");
            }
        }
        else
        {
            Debug.Log("No colliders found below player");
        }

        if (newParent != null)
        {
            if (_currentPlatformParent != newParent)
            {
                Debug.Log($"Setting parent to: {newParent.name}");
                transform.SetParent(newParent, true);
                _currentPlatformParent = newParent;
            }
        }
        else
        {
            if (_currentPlatformParent != null)
            {
                Debug.Log("Removing platform parent");
                transform.SetParent(null, true);
                _currentPlatformParent = null;
            }
        }
    }
    
    /// <summary>
    /// Attempts to perform an explosion in the facing direction.
    /// Respects the global movement lock to ensure explosions don't interfere with movement.
    /// </summary>
    /// <returns>True if explosion was accepted and will start; false if blocked by movement lock.</returns>
    public bool TryExplosion()
    {
        if (_anyGridMoverMoving && _currentMovingGridMover != this) return false;
        if (_isMoving) return false;
        
        // Explosion doesn't need ground validation - can explode in air
        // Get the explosion component and start the explosion
        Explosion explosion = GetComponent<Explosion>();
        if (explosion != null)
        {
            StartCoroutine(explosion.DoExplosion(CurrentCell, Facing));
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Attempts to perform a "zap" action. If the player is on a platform, 
    /// triggers the platform's MoveAndReverseDirection coroutine.
    /// </summary>
    /// <returns>True if zap was accepted and a platform was triggered; false otherwise.</returns>
    public bool TryZap()
    {
        if (_anyGridMoverMoving && _currentMovingGridMover != this) return false;
        if (_isMoving) return false;
        if (!_isPlayer) return false; // Only players can zap
        
        Debug.Log($"Player attempting zap. Current parent: {(transform.parent != null ? transform.parent.name : "null")}");
        
        // Check if player is currently parented to a platform
        PlatformController platform = GetCurrentPlatformParent();
        if (platform != null)
        {
            Debug.Log($"Found parent platform: {platform.name}");
            // Trigger the platform's MoveAndReverseDirection
            if (platform.TryMoveAndReverseDirection())
            {
                Debug.Log($"Successfully activated parent platform: {platform.name}");
                return true;
            }
            else
            {
                Debug.Log($"Parent platform {platform.name} rejected movement request");
            }
        }
        else
        {
            Debug.Log("Player is not parented to any platform");
        }
        
        return false;
    }
    
    private PlatformController GetCurrentPlatformParent()
    {
        if (transform.parent == null)
        {
            return null;
        }
        
        // Check if the direct parent is a platform
        PlatformController platform = transform.parent.GetComponent<PlatformController>();
        Debug.Log($"Checking parent: {platform.name}");;
        if (platform != null)
        {
            return platform;
        }
        
        // Check if any parent in the hierarchy is a platform (for nested hierarchies)
        platform = transform.parent.GetComponentInParent<PlatformController>();
        return platform;
    }

    private void OnDestroy()
    {
        // Release global lock if this mover was holding it
        if (_currentMovingGridMover == this)
        {
            _anyGridMoverMoving = false;
            _currentMovingGridMover = null;
        }
    }
}