using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TechTreeUI : MonoBehaviour
{
    [Header("Références")]
    public GameObject treePanel;
    public GameObject techNodePrefab;
    public Transform nodesParent;
    public TechTreeData techTreeData;
    public int currentResearchPoints = 100;

    private Dictionary<string, GameObject> nodeButtons = new();
    private GameObject arrowContainer;

    [SerializeField] private BuildingBarUI buildingBarUI;


    void Start()
    {
        treePanel.SetActive(false);
        InstantiateTree();
    }

    public void ToggleTree()
    {
        treePanel.SetActive(!treePanel.activeSelf);
    }

    void InstantiateTree()
    {
        if (techTreeData == null || techTreeData.techNodes == null) return;

        // Nettoyage ancien affichage
        foreach (Transform child in nodesParent)
        {
            Destroy(child.gameObject);
        }
        nodeButtons.Clear();

        // Container flèches
        arrowContainer = new GameObject("ArrowLines");
        arrowContainer.transform.SetParent(nodesParent, false);

        int i = 0;
        foreach (var node in techTreeData.techNodes)
        {
            GameObject btn = Instantiate(techNodePrefab, nodesParent);
            btn.name = node.id;

            RectTransform rt = btn.GetComponent<RectTransform>();
            int level = GetDepthLevel(node);
            Vector2 pos = new Vector2(300 * i, -350 * level);
            rt.anchoredPosition = pos;

               // Icon
            Transform iconTransform = btn.transform.Find("Icon");
            if (iconTransform != null)
            {
                Image iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null)
                    iconImage.sprite = node.icon;
            }

            // Coût
            Transform costTransform = btn.transform.Find("Cost");
            if (costTransform != null)
            {
                var costText = costTransform.GetComponent<TMPro.TMP_Text>();
                if (costText != null)
                    costText.text = $"{node.cost}";
            }

            // Nom
            Transform nameTransform = btn.transform.Find("Name");
            if (nameTransform != null)
            {
                var nameText = nameTransform.GetComponent<TMPro.TMP_Text>();
                if (nameText != null)
                    nameText.text = node.displayName;
            }

            btn.GetComponent<Button>().onClick.AddListener(() => TryUnlock(node.id));

            nodeButtons[node.id] = btn;
            i++;
        }

        // Mise à jour des couleurs
        foreach (var node in techTreeData.techNodes)
        {
            UpdateNodeVisual(nodeButtons[node.id], node);
        }

        // Dessin des flèches
        foreach (var node in techTreeData.techNodes)
        {
            foreach (string prereqId in node.prerequisiteIds)
            {
                if (nodeButtons.TryGetValue(prereqId, out GameObject fromBtn) &&
                    nodeButtons.TryGetValue(node.id, out GameObject toBtn))
                {
                    DrawArrow(fromBtn, toBtn);
                }
            }
        }
    }

    void UpdateNodeVisual(GameObject button, TechNode node)
    {
        var image = button.GetComponent<Image>();
        if (node.unlocked) image.color = Color.green;
        else if (ArePrerequisitesMet(node)) image.color = Color.white;
        else image.color = Color.gray;
    }

    bool ArePrerequisitesMet(TechNode node)
    {
        return node.prerequisiteIds.All(id =>
            techTreeData.techNodes.Find(n => n.id == id)?.unlocked == true);
    }

    void TryUnlock(string nodeId)
    {
        var node = techTreeData.techNodes.Find(n => n.id == nodeId);
        if (node == null || node.unlocked) return;

        if (!ArePrerequisitesMet(node)) return;
        if (currentResearchPoints < node.cost) return;

        currentResearchPoints -= node.cost;
        node.unlocked = true;
        UpdateNodeVisual(nodeButtons[nodeId], node);
        ApplyTechEffect(node.id);

        // Met à jour tous les nœuds après déblocage
        foreach (var n in techTreeData.techNodes)
            UpdateNodeVisual(nodeButtons[n.id], n);
    }

    void ApplyTechEffect(string techId)
    {
        switch (techId)
        {
            case "0":
                Bonus.woodBonus += 1f;
                Debug.Log("Bonus Bois débloqué");
                break;
            case "1":
                BuildingManager.UnlockBuilding("Puit");
                GameAge age = AgeManager.Instance.GetCurrentAge(); 
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge()); 
                break;
            case "2":
                AgeManager.Instance.AdvanceToNextAge();
                break;
        }
    }

    int GetDepthLevel(TechNode node, HashSet<string> visited = null)
    {
        visited ??= new HashSet<string>();

        if (visited.Contains(node.id))
        {
            Debug.LogError($"Boucle circulaire détectée : {node.id} dépend de lui-même !");
            return 0;
        }

        visited.Add(node.id);

        if (node.prerequisiteIds == null || node.prerequisiteIds.Count == 0)
            return 0;

        int max = 0;
        foreach (string id in node.prerequisiteIds)
        {
            var prereq = techTreeData.techNodes.Find(n => n.id == id);
            if (prereq != null)
            {
                int depth = GetDepthLevel(prereq, new HashSet<string>(visited));
                max = Mathf.Max(max, depth + 1);
            }
        }

        return max;
    }


    void DrawArrow(GameObject fromBtn, GameObject toBtn)
    {
        GameObject lineObj = new GameObject("Arrow");
        lineObj.transform.SetParent(arrowContainer.transform, false);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = 1f;
        lr.endWidth = 1f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.white;
        lr.endColor = Color.white;
        lr.useWorldSpace = false;

        Vector3 from = fromBtn.GetComponent<RectTransform>().anchoredPosition;
        Vector3 to = toBtn.GetComponent<RectTransform>().anchoredPosition;

        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
    }
}
