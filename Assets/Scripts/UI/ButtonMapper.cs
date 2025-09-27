using UnityEngine;

public class ButtonMapper : MonoBehaviour
{
    public GridMover gridMover;

    public void Button1()
    {
        Debug.Log("Button1 pressed");
        if (gridMover)
        {
            gridMover.TryForward();
        }
        else
        {
            Debug.LogError("Player not assigned to Button Mapper in UI");
        }
    }
    public void Button2()
    {
        Debug.Log("Button2 pressed");
        if (gridMover)
        {
        }
        else
        {
            Debug.LogError("Player not assigned to Button Mapper in UI");
        }
    }
    public void Button3()
    {
        Debug.Log("Button3 pressed");
        if (gridMover)
        {
        }
        else
        {
            Debug.LogError("Player not assigned to Button Mapper in UI");
        }
    }
    public void Button4()
    {
        Debug.Log("Button4 pressed");
        if (gridMover)
        {
        }
        else
        {
            Debug.LogError("Player not assigned to Button Mapper in UI");
        }
    }
    public void Button5()
    {
        Debug.Log("Button5 pressed");
        if (gridMover)
        {
        }
        else
        {
            Debug.LogError("Player not assigned to Button Mapper in UI");
        }
    }
}
