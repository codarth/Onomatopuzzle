using UnityEngine;
using static DataHolder;

public class PlayerController : MonoBehaviour
{
    private GridMover _mover;
    private Explosion _explosion;
    private GlobalState _globalState;
    
    [SerializeField] private int forwardDistance = 1;
    [SerializeField] private int jumpHeight = 2;
    [SerializeField] private int jumpDistance = 3;
    
    [Header("Energy Costs")]
    [SerializeField] private int forwardEnergyCost = 1;
    [SerializeField] private int changeFacingEnergyCost = 1;
    [SerializeField] private int jumpUpEnergyCost = 3;
    [SerializeField] private int longJumpEnergyCost = 3;
    [SerializeField] private int explosionEnergyCost = 5;
    [SerializeField] private int zapEnergyCost = 2;

    void Awake()
    {
        _mover = GetComponent<GridMover>();
        _explosion = GetComponent<Explosion>();
    }

    void Start()
    {
        _globalState = GlobalState.Instance;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (!_globalState.hasEnoughPower(forwardEnergyCost)) return;
            MoveForward();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (!_globalState.hasEnoughPower(changeFacingEnergyCost)) return;
            TryJumpAndMoveForward();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && CurrentLevel > 0)
        {
            Debug.Log("CurrentLevel is " + CurrentLevel);
            if (!_globalState.hasEnoughPower(jumpUpEnergyCost)) return;
            ChangeDirection();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) && CurrentLevel > 1)
        {
            if (!_globalState.hasEnoughPower(longJumpEnergyCost)) return;
            TryJumpForward();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5) && CurrentLevel < 2)
        {
            if (!_globalState.hasEnoughPower(explosionEnergyCost)) return;
            TryExplosion();
        }        
        else if (Input.GetKeyDown(KeyCode.Alpha6) && CurrentLevel < 3)
        {
            if (!_globalState.hasEnoughPower(zapEnergyCost)) return;
            TryZap();
        }

    }
    
    public void MoveForward()
    {
        Debug.Log("Alpha 1 pressed: TryForward(2)");
        if (_mover.TryForward(forwardDistance))
        {
            _globalState.DecreasePower(forwardEnergyCost);
        }

    }

    public void ChangeDirection()
    {
        Debug.Log("Alpha 2 pressed: ChangeFacing()");
        if (_mover.ChangeFacing())
        {
            _globalState.DecreasePower(changeFacingEnergyCost);
        }
    }

    public void TryJumpAndMoveForward()
    {
        Debug.Log("Alpha 3 pressed: TryJumpUpThenForward(3)");
        if (_mover.TryJumpUpThenForward(jumpHeight))
        {
            _globalState.DecreasePower(jumpUpEnergyCost);
        }

    }

    public void TryJumpForward()
    {
        Debug.Log("Alpha 4 pressed: TryJumpForward(4)");
        if (_mover.TryJumpForward(jumpDistance))
        {
            _globalState.DecreasePower(longJumpEnergyCost);
        }

    }

    public bool TryExplosion()
    {
        Debug.Log("Alpha 5 pressed: TryExplosion()");
        if (_mover.TryExplosion())
        {
            _globalState.DecreasePower(explosionEnergyCost);
            return true;
        }
        return false;
    }

    public bool TryZap()
    {
        Debug.Log("Alpha 6 pressed: TryZap()");
        if (_mover.TryZap())
        {
            _globalState.DecreasePower(zapEnergyCost);
            return true;
        }
        return false;
    }

}