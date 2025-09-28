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
        }
        else
        {
            Debug.LogError("Player not assigned to Button Mapper in UI");
        }
    }
}
