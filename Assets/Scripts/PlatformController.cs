using UnityEngine;

public class PlatformController : MonoBehaviour
{
    [Header("Platform Movement Settings")]
    [Tooltip("Direction to move when pressing Q. Cardinal directions recommended (up/down/left/right).")]
    [SerializeField] private GridMover.MovementDirection direction = default;

    [Tooltip("How many tiles to move in the configured direction when pressing Q.")]
    [SerializeField, Min(1)] private int distance = 1;

    [Header("References")]
    [Tooltip("GridMover component that performs the actual movement.")]
    [SerializeField] private GridMover gridMover;
    
    // Track if we're currently moving to avoid multiple simultaneous moves
    private bool isCurrentlyMoving = false;
    
    [Header("Sprites")]
    public Sprite idleSprite;
    public Sprite movingSprite;
    
    private SpriteRenderer _sr;

    private void Awake()
    {
        if (gridMover == null)
        {
            gridMover = GetComponent<GridMover>();
        }
        if (direction == default)
        {
            direction = GridMover.MovementDirection.Right; // default per requirements
        }
        
        _sr = GetComponentInChildren<SpriteRenderer>();
        if (_sr == null)
        {
            Debug.LogError("No SpriteRenderer found on children of " + gameObject.name);
        }
    }

    private void Update()
    {
        if (gridMover == null) return;
        
        // Only allow new movement if not currently moving
        if (Input.GetKeyDown(KeyCode.Q) && !isCurrentlyMoving)
        {
            TryMoveAndReverseDirection();
        }

        if (isCurrentlyMoving)
        {
            _sr.sprite = movingSprite;
        }
        else
        {
            _sr.sprite = idleSprite;
        }
    }

    /// <summary>
    /// Attempts to move the platform and reverse its direction.
    /// Follows the same pattern as other "Try" methods with validation and boolean return.
    /// </summary>
    /// <returns>True if the movement was accepted and started; false if blocked or already moving.</returns>
    public bool TryMoveAndReverseDirection()
    {
        if (gridMover == null) return false;
        if (isCurrentlyMoving) return false;
        if (GridMover.IsAnyGridMoverMoving) return false;
        
        // Check if the first step in the direction is possible
        Vector2Int directionVector = GridMover.GetDirectionVector(direction);
        Vector3Int currentCell = gridMover.CurrentCell;
        Vector3Int firstStep = currentCell + new Vector3Int(directionVector.x, directionVector.y, 0);
        
        // Use basic collision check - platforms don't use the same CanStep logic as players
        if (IsBlocked(firstStep))
        {
            return false;
        }
        
        StartCoroutine(MoveAndReverseDirection());
        return true;
    }

    private System.Collections.IEnumerator MoveAndReverseDirection()
    {
        isCurrentlyMoving = true;
        
        // Start the movement
        gridMover.TryMoveInDirection(GridMover.GetDirectionVector(direction), Mathf.Max(1, distance));
        
        // Wait until the movement is complete
        while (gridMover.IsMoving)
        {
            yield return null;
        }
        
        // Reverse the direction after movement completes
        ReverseDirection();
        
        isCurrentlyMoving = false;
    }

    private void ReverseDirection()
    {
        switch (direction)
        {
            case GridMover.MovementDirection.Up:
                direction = GridMover.MovementDirection.Down;
                break;
            case GridMover.MovementDirection.Down:
                direction = GridMover.MovementDirection.Up;
                break;
            case GridMover.MovementDirection.Left:
                direction = GridMover.MovementDirection.Right;
                break;
            case GridMover.MovementDirection.Right:
                direction = GridMover.MovementDirection.Left;
                break;
        }
    }
    
    /// <summary>
    /// Simple collision check for platforms - just checks if a cell is blocked.
    /// </summary>
    /// <param name="cell">The cell to check</param>
    /// <returns>True if blocked, false if clear</returns>
    private bool IsBlocked(Vector3Int cell)
    {
        Vector3 worldPos = gridMover.grid.CellToWorld(cell) + gridMover.grid.cellSize / 2f;
        Collider2D collider = Physics2D.OverlapPoint(worldPos);
        
        if (collider != null && !collider.isTrigger)
        {
            return true;
        }
        
        return false;
    }
}