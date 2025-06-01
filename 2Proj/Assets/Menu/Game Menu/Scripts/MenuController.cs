using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum PanelType
{
    None,
    Main,
    Option,
    Credits,
    Exit,
}
public class MenuController : MonoBehaviour
{
    [FormerlySerializedAs("panelslist")]
    [Header("Panels")]
    [SerializeField] private List<MenuPanel> panelsList = new List<MenuPanel>();
    private Dictionary<PanelType, MenuPanel> panelsDictionary = new Dictionary<PanelType, MenuPanel>();
    private GameManager manager;

    private void Start()
    {
        manager = GameManager.instance;

        foreach (var _panel in panelsList)
        {
            if (_panel) panelsDictionary.Add(_panel.GetPanelType(), _panel);
        }
        OpenOnePanel(PanelType.Main, false);
    }

    private void OpenOnePanel(PanelType _type, bool _animate)
    {
        // Désactiver tous les panels
        foreach (var _panel in panelsList)
        {
            if (_panel != null)
            {
                _panel.gameObject.SetActive(false);
            }
        }

        // Activer celui demandé
        if (_type != PanelType.None && panelsDictionary.ContainsKey(_type))
        {
            var panel = panelsDictionary[_type];

            panel.gameObject.SetActive(true); // 🔥 le rend visible dans la hiérarchie
            panel.ChangeState(_animate, true); // 🔄 applique l’animation si besoin
            
        }
    }

    public void OpenPanel(PanelType _type)
    {
        OpenOnePanel(_type, true);
    }
    public void ChangeScene(string _sceneName)
    {
        manager.ChangeScene(_sceneName);
    }
    public void QuitGame()
    {
        manager.QuitGame();
    } 
}