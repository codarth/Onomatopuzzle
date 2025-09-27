using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private GridMover _mover;
    private Explosion _explosion;

    public Vector2Int facing = Vector2Int.right;

    void Awake()
    {
        _mover = GetComponent<GridMover>();
        _explosion = GetComponent<Explosion>();
    }

    void Start()
    {
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("Alpha 1 pressed: TryForward(2)");
            _mover.TryForward(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Alpha 2 pressed: ChangeFacing()");
            _mover.ChangeFacing();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("Alpha 3 pressed: TryJumpUpThenForward(3)");
            _mover.TryJumpUpThenForward(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("Alpha 4 pressed: TryJumpForward(4)");
            _mover.TryJumpForward(7);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log("Alpha 5 pressed: TryForward()");
            _explosion.DoExplosion(_mover.CurrentCell, facing);
        }
    }
}