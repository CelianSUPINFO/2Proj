using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

[RequireComponent(typeof(PlayerInput))]
public class MenuInputs : MonoBehaviour
{
    public enum Side { Left, Right, }

    private PlayerInput inputs;

    private UnityEvent backEvent;
    private UnityEvent rightShoulderEvent, leftShoulderEvent;

    private void Awake()
    {
        inputs = GetComponent<PlayerInput>();
        backEvent = new UnityEvent();
        rightShoulderEvent = new UnityEvent();
        leftShoulderEvent = new UnityEvent();
    }

    private void OnBack() { backEvent.Invoke(); }
    private void OnRightShoulder() { rightShoulderEvent.Invoke(); }
    private void OnLeftShoulder() { leftShoulderEvent.Invoke(); }


    public void SetBackListener(UnityAction _call)
    {
        backEvent.RemoveAllListeners();
        backEvent.AddListener(_call);
    }
    public void SetBackListener() { backEvent.RemoveAllListeners(); }

    public void SetShoulderListener(Side _side, UnityAction _call, UnityAction _selection)
    {
        UnityEvent _event = _side == Side.Left ? leftShoulderEvent : rightShoulderEvent;

        _event.RemoveAllListeners();
        _event.AddListener(_selection);
        _event.AddListener(_call);
    }
    public void SetShoulderListener(Side _side)
    {
        UnityEvent _event = _side == Side.Left ? leftShoulderEvent : rightShoulderEvent;
        _event.RemoveAllListeners();
    }
}
