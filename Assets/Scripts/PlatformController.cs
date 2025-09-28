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
            StartCoroutine(MoveAndReverseDirection());
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
}