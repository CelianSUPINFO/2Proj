using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OpenPanelButton : MonoBehaviour
{

    [SerializeField] private PanelType type;
    [SerializeField] private OpenPanelButton onSwitchBackAction;

    private MenuController controller;
    private MenuInputs inputs;

    void Start()
    {
        controller = FindObjectOfType<MenuController>();
        inputs = controller.GetComponent<MenuInputs>();
    }

    public void OnClick()
    {
        controller.OpenPanel(type);
        if (onSwitchBackAction != null) inputs.SetBackListener(onSwitchBackAction.OnClick);
        else inputs.SetBackListener();
    }
}