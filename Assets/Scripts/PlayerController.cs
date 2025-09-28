using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private GridMover _mover;
    private Explosion _explosion;
    
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
            MoveForward();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TryJumpAndMoveForward();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ChangeDirection();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TryJumpForward();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            DoExplosion();
        }
    }
    public void MoveForward()
    {
        Debug.Log("Alpha 1 pressed: TryForward(2)");
        _mover.TryForward(forwardDistance);
    }

    public void ChangeDirection()
    {
        Debug.Log("Alpha 2 pressed: ChangeFacing()");
        _mover.ChangeFacing();
    }

    public void TryJumpAndMoveForward()
    {
        Debug.Log("Alpha 3 pressed: TryJumpUpThenForward(3)");
        _mover.TryJumpUpThenForward(jumpHeight);
    }

    public void TryJumpForward()
    {
        Debug.Log("Alpha 4 pressed: TryJumpForward(4)");
        _mover.TryJumpForward(jumpDistance);
    }

    public void DoExplosion()
    {
        Debug.Log("Alpha 5 pressed: TryForward()");
        StartCoroutine(_explosion.DoExplosion(_mover.CurrentCell, _mover.Facing));
    }
}