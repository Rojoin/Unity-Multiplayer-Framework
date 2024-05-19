using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField]public UnityEvent<Vector2> OnMoveChannel;
    [SerializeField]private VoidChannelSO OnRollChannel;
    [SerializeField]private VoidChannelSO OnPauseChannel;
    [SerializeField]private BoolChannelSO OnFocusChannel;
    [SerializeField]private BoolChannelSO OnFireChannel;



    public void OnMove(InputAction.CallbackContext ctx)
    {
        OnMoveChannel.Invoke(ctx.ReadValue<Vector2>());
    }
    public void OnRollInput(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            OnRollChannel.RaiseEvent();
        }
    }
    public void OnFocusMode(InputAction.CallbackContext ctx)
    {
        OnFocusChannel.RaiseEvent(ctx.performed);
    } 
    public void OnFire(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            //OnFire.Invoke();
            OnFireChannel.RaiseEvent(true);
        }
        else if (ctx.canceled)
        {
            OnFireChannel.RaiseEvent(false);
        }
    }
    public void OnPauseMode(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            OnPauseChannel.RaiseEvent();
        }
    }

}