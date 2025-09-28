using UnityEngine;

public class PlatformController : MonoBehaviour
{
    [Header("Platform Movement Settings")]
    [Tooltip("Direction to move when pressing Q. Cardinal directions recommended (up/down/left/right).")]
    [SerializeField] private Vector2Int direction = default;

    [Tooltip("How many tiles to move in the configured direction when pressing Q.")]
    [SerializeField, Min(1)] private int distance = 1;

    [Header("References")]
    [Tooltip("GridMover component that performs the actual movement.")]
    [SerializeField] private GridMover gridMover;

    private void Awake()
    {
        if (gridMover == null)
        {
            gridMover = GetComponent<GridMover>();
        }
        if (direction == default)
        {
            direction = Vector2Int.right; // default per requirements
        }
    }

    private void Update()
    {
        if (gridMover == null) return;
        if (Input.GetKeyDown(KeyCode.Q))
        {
            gridMover.TryMoveInDirection(direction, Mathf.Max(1, distance));
        }
    }
}
