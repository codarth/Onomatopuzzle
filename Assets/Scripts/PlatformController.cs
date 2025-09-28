using UnityEngine;

public class PlatformController : MonoBehaviour
{
    [Header("Platform Movement Settings")]
    [Tooltip("Direction to move when pressing Q. Cardinal directions recommended (up/down/left/right).")]
    [SerializeField] private GridMover.MovementDirection direction = GridMover.MovementDirection.Right;

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

    // Debug field to show current direction in inspector
    [Header("Debug Info")]
    [SerializeField, Tooltip("Shows the current movement direction (read-only)")] 
    private string currentDirection;

    private void Awake()
    {
        if (gridMover == null)
        {
            gridMover = GetComponent<GridMover>();
        }
        
        _sr = GetComponentInChildren<SpriteRenderer>();
        if (_sr == null)
        {
            Debug.LogError("No SpriteRenderer found on children of " + gameObject.name);
        }

        // Update debug info
        UpdateDebugInfo();
    }

    private void Update()
    {
        if (gridMover == null) return;
        
        // Update debug info if direction changed
        UpdateDebugInfo();

        // Remove the input handling - platforms should only move when explicitly triggered
        // The player's TryZap() method will handle platform activation

        if (isCurrentlyMoving)
        {
            _sr.sprite = movingSprite;
        }
        else
        {
            _sr.sprite = idleSprite;
        }
    }


    private void UpdateDebugInfo()
    {
        currentDirection = direction.ToString();
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
        
        // Validate that this platform is actually below the player
        if (!IsPlayerDirectlyAbove())
        {
            Debug.Log($"Platform {gameObject.name} not active - player not above");
            return false;
        }
        
        // Check if the first step in the direction is possible
        Vector2Int directionVector = GridMover.GetDirectionVector(direction);
        Vector3Int currentCell = gridMover.CurrentCell;
        Vector3Int firstStep = currentCell + new Vector3Int(directionVector.x, directionVector.y, 0);
        
        Debug.Log($"Platform {gameObject.name} moving from {currentCell} to {firstStep} (direction: {direction})");
        
        StartCoroutine(MoveAndReverseDirection());
        return true;
    }
    
    /// <summary>
    /// Checks if a player is currently a child of this platform.
    /// This is more reliable than spatial detection as it uses the existing parent-child relationship.
    /// </summary>
    /// <returns>True if a player is parented to this platform, false otherwise.</returns>
    private bool IsPlayerDirectlyAbove()
    {
        // Check all direct children for a PlayerController component
        foreach (Transform child in transform)
        {
            PlayerController player = child.GetComponent<PlayerController>();
            if (player != null)
            {
                Debug.Log($"Found player {child.name} as child of platform {gameObject.name}");
                return true;
            }
        }
        
        // Also check nested children in case there's a hierarchy
        PlayerController[] playersInChildren = GetComponentsInChildren<PlayerController>();
        if (playersInChildren.Length > 0)
        {
            Debug.Log($"Found {playersInChildren.Length} player(s) in children of platform {gameObject.name}");
            return true;
        }
        
        Debug.Log($"No players found as children of platform {gameObject.name}");
        return false;
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
        GridMover.MovementDirection oldDirection = direction;
        ReverseDirection();
        Debug.Log($"Platform {gameObject.name} reversed direction: {oldDirection} -> {direction}");
        
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
}