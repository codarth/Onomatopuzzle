using UnityEngine;

public class ButtonMapper : MonoBehaviour
{
    public PlayerController playerController;

    private void Start()
    {
        playerController = FindAnyObjectByType<PlayerController>();
    }
    
    public void Button1()
    {
        Debug.Log("Button1 pressed");
        if (playerController)
        {
            playerController.MoveForward();
            AudioController.Instance.PlaySFX(PlayerController.Instance.buttonSfx);
        }
        else
        {
            Debug.LogError("Player not assigned to Button Mapper in UI");
        }
    }
    public void Button2()
    {
        Debug.Log("Button2 pressed");
        if (playerController)
        {
            playerController.TryJumpAndMoveForward();
            AudioController.Instance.PlaySFX(PlayerController.Instance.buttonSfx);
        }
        else
        {
            Debug.LogError("Player not assigned to Button Mapper in UI");
        }
    }
    public void Button3()
    {
        Debug.Log("Button3 pressed");
        if (playerController)
        {
            playerController.ChangeDirection();
            AudioController.Instance.PlaySFX(PlayerController.Instance.buttonSfx);
        }
        else
        {
            Debug.LogError("Player not assigned to Button Mapper in UI");
        }
    }
    public void Button4()
    {
        Debug.Log("Button4 pressed");
        if (playerController)
        {
            playerController.TryJumpForward();
            AudioController.Instance.PlaySFX(PlayerController.Instance.buttonSfx);
        }
        else
        {
            Debug.LogError("Player not assigned to Button Mapper in UI");
        }
    }
    public void Button5()
    {
        Debug.Log("Button5 pressed");
        if (playerController)
        {
            playerController.TryExplosion();
            AudioController.Instance.PlaySFX(PlayerController.Instance.buttonSfx);
        }
        else
        {
            Debug.LogError("Player not assigned to Button Mapper in UI");
        }
    }
    
    public void Button6()
    {
        Debug.Log("Button6 pressed - Zap");
        if (playerController)
        {
            playerController.TryZap();
            AudioController.Instance.PlaySFX(PlayerController.Instance.buttonSfx);
        }
        else
        {
            Debug.LogError("Player not assigned to Button Mapper in UI");
        }
    }
}
