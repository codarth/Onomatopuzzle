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
            Debug.Log("Numpad 1 pressed");
            _mover.TryMove(facing);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Numpad 2 pressed");
            _mover.ChangeFacing();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("Numpad 3 pressed");
            _mover.TryJump(facing);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("Numpad 4 pressed");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log("Numpad 5 pressed");
        }
    }
}