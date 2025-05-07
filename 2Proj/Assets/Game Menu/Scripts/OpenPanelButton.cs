using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenPanelButton : MonoBehaviour
{

    [SerializeField] private PanelType type;

    private MenuController controller;

    void Start()
    {
        controller = FindObjectOfType<MenuController>();
    }
    
    public void OneClick()
    {
        controller.OpenPanel(type);
    }
}
   