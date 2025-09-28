using UnityEngine;
using static DataHolder;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    private GridMover _mover;
    private Explosion _explosion;
    private GlobalState _globalState;

    [SerializeField] private int forwardDistance = 1;
    [SerializeField] private int jumpHeight = 2;
    [SerializeField] private int jumpDistance = 3;

    [Header("Energy Costs")] [SerializeField]
    private int forwardEnergyCost = 1;

    [SerializeField] private int changeFacingEnergyCost = 1;
    [SerializeField] private int jumpUpEnergyCost = 3;
    [SerializeField] private int longJumpEnergyCost = 3;
    [SerializeField] private int explosionEnergyCost = 5;
    [SerializeField] private int zapEnergyCost = 2;

    private AudioController AudioController => AudioController.Instance;
    [Header("Sound Effects")] 
    public AudioClip moveForwardSfx;
    public AudioClip jumpUpSfx;
    public AudioClip longJumpSfx;
    public AudioClip changeDirectionSfx;
    public AudioClip explodeSfx;
    public AudioClip zapSfx;
    public AudioClip batteryEmptySfx;
    public AudioClip gameWinSfx;
    public AudioClip deathSfx;
    public AudioClip glorpSfx;
    public AudioClip nextLevelSfx;
    public AudioClip buttonSfx;


    void Awake()
    {
        Instance = this;

        _mover = GetComponent<GridMover>();
        _explosion = GetComponent<Explosion>();
    }

    void Start()
    {
        _globalState = GlobalState.Instance;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.R))
        {
            if (!_globalState.hasEnoughPower(forwardEnergyCost))
            {
                AudioController.PlaySFX(batteryEmptySfx);
                return;
            }

            MoveForward();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.T))
        {
            if (!_globalState.hasEnoughPower(changeFacingEnergyCost))
            {
                AudioController.PlaySFX(batteryEmptySfx);
                return;
            }

            TryJumpAndMoveForward();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Y)/* && CurrentLevel > 0*/)
        {
            Debug.Log("CurrentLevel is " + CurrentLevel);
            if (!_globalState.hasEnoughPower(jumpUpEnergyCost))
            {
                AudioController.PlaySFX(batteryEmptySfx);
                return;
            }

            ChangeDirection();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.F)/* && CurrentLevel > 1*/)
        {
            if (!_globalState.hasEnoughPower(longJumpEnergyCost))
            {
                AudioController.PlaySFX(batteryEmptySfx);
                return;
            }

            TryJumpForward();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.G)/* && CurrentLevel < 2*/)
        {
            if (!_globalState.hasEnoughPower(explosionEnergyCost))
            {
                AudioController.PlaySFX(batteryEmptySfx);
                return;
            }

            TryExplosion();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.H)/* && CurrentLevel < 3*/)
        {
            if (!_globalState.hasEnoughPower(zapEnergyCost))
            {
                AudioController.PlaySFX(batteryEmptySfx);
                return;
            }

            TryZap();
        }
        else if (Input.GetMouseButtonDown(2))
        {
            Debug.Log("Exiting game...");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

    }

    public void MoveForward()
    {
        Debug.Log("Alpha 1 pressed: TryForward(2)");
        if (_mover.TryForward(forwardDistance))
        {
            _globalState.DecreasePower(forwardEnergyCost);
            AudioController.PlaySFX(moveForwardSfx);
        }
    }

    public void ChangeDirection()
    {
        Debug.Log("Alpha 2 pressed: ChangeFacing()");
        if (_mover.ChangeFacing())
        {
            _globalState.DecreasePower(changeFacingEnergyCost);
            AudioController.PlaySFX(changeDirectionSfx);
        }
    }

    public void TryJumpAndMoveForward()
    {
        Debug.Log("Alpha 3 pressed: TryJumpUpThenForward(3)");
        if (_mover.TryJumpUpThenForward(jumpHeight))
        {
            _globalState.DecreasePower(jumpUpEnergyCost);
            AudioController.PlaySFX(jumpUpSfx);
        }
    }

    public void TryJumpForward()
    {
        Debug.Log("Alpha 4 pressed: TryJumpForward(4)");
        if (_mover.TryJumpForward(jumpDistance))
        {
            _globalState.DecreasePower(longJumpEnergyCost);
            AudioController.PlaySFX(longJumpSfx);
        }
    }

    public bool TryExplosion()
    {
        Debug.Log("Alpha 5 pressed: TryExplosion()");
        if (_mover.TryExplosion())
        {
            _globalState.DecreasePower(explosionEnergyCost);
            // AudioController.PlaySFX(explodeSfx);

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
            AudioController.PlaySFX(zapSfx);

            return true;
        }

        return false;
    }
}