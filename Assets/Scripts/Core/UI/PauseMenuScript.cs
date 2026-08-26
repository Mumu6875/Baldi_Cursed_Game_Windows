using UnityEngine;
using UnityEngine.EventSystems;

public class PauseMenuScript : MonoBehaviour
{
    private void Update()
    {
        if (usingJoystick & EventSystem.current.currentSelectedGameObject == null)
        {
            if (!gc.mouseLocked)
            {
                gc.LockMouse();
            }
        }
        else if (!usingJoystick && gc.mouseLocked)
        {
            gc.UnlockMouse();
        }
    }
    public GameControllerScript gc;

    private bool usingJoystick
    {
        get
        {
            return false;
        }
    }
}
