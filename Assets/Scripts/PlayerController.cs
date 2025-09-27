using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private GridMover _mover;
    private Explosion _explosion;

    public Vector2Int facing = Vector2Int.right;

    [SerializeField] private int forwardDistance = 1;
    [SerializeField] private int jumpHeight = 2;
    [SerializeField] private int jumpDistance = 3;
    
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
            _mover.TryForward(forwardDistance);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Alpha 2 pressed: ChangeFacing()");
            _mover.ChangeFacing();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("Alpha 3 pressed: TryJumpUpThenForward(3)");
            _mover.TryJumpUpThenForward(jumpHeight);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("Alpha 4 pressed: TryJumpForward(4)");
            _mover.TryJumpForward(jumpDistance);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log("Alpha 5 pressed: TryForward()");
            _explosion.DoExplosion(_mover.CurrentCell, facing);
        }
    }
}