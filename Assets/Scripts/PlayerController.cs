using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private GridMover _mover;

    public Vector2Int facing = Vector2Int.right;

    void Awake()
    {
        _mover = GetComponent<GridMover>();
    }

    void Start()
    {
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("Alpha 1 pressed: TryForward()");
            _mover.TryForward();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Alpha 2 pressed: ChangeFacing()");
            _mover.ChangeFacing();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("Alpha 3 pressed: TryJumpUpThenForward(3,1)");
            _mover.TryJumpUpThenForward();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("Alpha 4 pressed: TryJumpArc(diagonal 2,2)");
            _mover.TryJumpArc(new Vector2Int(2, 2), 0.5f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log("Alpha 5 pressed: TryForward()");
            _mover.TryForward();
        }
    }
}