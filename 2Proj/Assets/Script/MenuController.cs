using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum PanelType
{
    None,
    Main,
    Option,
    Credits,
}

public class MenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private List<MenuPanel> panelsList = new List<MenuPanel>();
    private Dictionary<PanelType, MenuPanel> panelsDict = new Dictionary<PanelType, MenuPanel>();

    [SerializeField] private EventSystem eventController;
    private GameManager manager;
    private MenuInputs inputs;

    private void Start()
    {
        manager = GameManager.instance;
        inputs = GetComponent<MenuInputs>();

        foreach (var _panel in panelsList)
        {
            if (_panel)
            {
                panelsDict.Add(_panel.GetPanelType(), _panel);
                _panel.Init(this);
            }
        }

        OpenOnePanel(PanelType.Main, false);
    }

    private void OpenOnePanel(PanelType _type, bool _animate)
    {
        foreach (var _panel in panelsList) _panel.ChangeState(_animate, false);

        if (_type != PanelType.None) panelsDict[_type].ChangeState(_animate, true);
    }

    public void OpenPanel(PanelType _type)
    {
        OpenOnePanel(_type, true);
    }

    public void ChangeScene(string _sceneName)
    {
        manager.ChangeScene(_sceneName);
    }

    public void Quit()
    {
        manager.Quit();
    }

    public void SetSelectedGameObject(GameObject _element, Button _rightPanel, Button _leftPanel)
    {
        eventController.SetSelectedGameObject(_element);

        if(_rightPanel != null) inputs.SetShoulderListener(MenuInputs.Side.Right, _rightPanel.onClick.Invoke, _rightPanel.Select);
        if(_leftPanel != null) inputs.SetShoulderListener(MenuInputs.Side.Left, _leftPanel.onClick.Invoke, _leftPanel.Select);
    }
}