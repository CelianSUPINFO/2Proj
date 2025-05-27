using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

class TreeNode
{
    public TechNode data;
    public List<TreeNode> children = new();
    public int depth = 0;
    public Vector2 position;
}

public class TechTreeUI : MonoBehaviour
{
    [Header("Références")]
    public GameObject treePanel;
    public GameObject techNodePrefab;
    public Transform nodesParent;
    public TechTreeData techTreeData;

    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private BuildingBarUI buildingBarUI;

    private Dictionary<string, GameObject> nodeButtons = new();
    private GameObject arrowContainer;

    private float nodeSpacingX = 180f;
    private float nodeSpacingY = 200f;

    void Start()
    {
        treePanel.SetActive(false);
        foreach (var node in techTreeData.techNodes)
        {
            node.unlocked = false;
        }
        LayoutAndInstantiateTree();
    }

    public void ToggleTree()
    {
        treePanel.SetActive(!treePanel.activeSelf);
    }

    // --- Arbre logique ---

    Dictionary<string, TreeNode> BuildTreeGraph()
    {
        Dictionary<string, TreeNode> allNodes = new();

        foreach (var tech in techTreeData.techNodes)
            allNodes[tech.id] = new TreeNode { data = tech };

        foreach (var node in allNodes.Values)
        {
            foreach (string prereqId in node.data.prerequisiteIds)
            {
                if (allNodes.TryGetValue(prereqId, out var parent))
                {
                    parent.children.Add(node);
                    node.depth = Mathf.Max(node.depth, parent.depth + 1);
                }
            }
        }

        return allNodes;
    }

    float LayoutTree(TreeNode node, float x = 0)
    {
        if (node.children.Count == 0)
        {
            node.position = new Vector2(x, -node.depth * nodeSpacingY);
            return x + nodeSpacingX;
        }

        float currentX = x;
        foreach (var child in node.children)
            currentX = LayoutTree(child, currentX);

        float left = node.children.First().position.x;
        float right = node.children.Last().position.x;
        float center = (left + right) / 2f;

        node.position = new Vector2(center, -node.depth * nodeSpacingY);
        return currentX;
    }

    // --- Génération ---

    void LayoutAndInstantiateTree()
    {
        foreach (Transform child in nodesParent) Destroy(child.gameObject);
        nodeButtons.Clear();

        arrowContainer = new GameObject("ArrowLines");
        arrowContainer.transform.SetParent(nodesParent, false);

        var graph = BuildTreeGraph();

        // Trouver racines (sans prerequisite)
        var roots = graph.Values
            .Where(n => n.data.prerequisiteIds == null || n.data.prerequisiteIds.Count == 0)
            .ToList();

        float startX = 0;
        foreach (var root in roots)
            startX = LayoutTree(root, startX);

        // Générer boutons
        foreach (var node in graph.Values)
            InstantiateTreeNode(node);

        // Générer flèches
        foreach (var node in graph.Values)
        {
            foreach (var child in node.children)
            {
                if (nodeButtons.TryGetValue(node.data.id, out var fromBtn) &&
                    nodeButtons.TryGetValue(child.data.id, out var toBtn))
                {
                    DrawArrow(fromBtn, toBtn);
                }
            }
        }

        // Appliquer couleur visuelle
        foreach (var node in techTreeData.techNodes)
            UpdateNodeVisual(nodeButtons[node.id], node);
    }

    void InstantiateTreeNode(TreeNode node)
    {
        GameObject btn = Instantiate(techNodePrefab, nodesParent);
        btn.name = node.data.id;

        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchoredPosition = node.position;

        Transform iconTransform = btn.transform.Find("Icon");
        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null)
                iconImage.sprite = node.data.icon;
        }

        Transform costTransform = btn.transform.Find("Cost");
        if (costTransform != null)
        {
            TMP_Text costText = costTransform.GetComponent<TMP_Text>();
            if (costText != null)
                costText.text = $"{node.data.cost}";
        }

        Transform nameTransform = btn.transform.Find("Name");
        if (nameTransform != null)
        {
            TMP_Text nameText = nameTransform.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = node.data.displayName;
        }

        btn.GetComponent<Button>().onClick.AddListener(() => TryUnlock(node.data.id));
        nodeButtons[node.data.id] = btn;
    }

    void DrawArrow(GameObject fromBtn, GameObject toBtn)
    {
        GameObject arrow = Instantiate(arrowPrefab, arrowContainer.transform);
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();

        Vector2 start = fromBtn.GetComponent<RectTransform>().anchoredPosition;
        Vector2 end = toBtn.GetComponent<RectTransform>().anchoredPosition;

        Vector2 direction = end - start;
        float distance = direction.magnitude;

        arrowRect.sizeDelta = new Vector2(distance, 3f);
        arrowRect.anchoredPosition = start + direction / 2;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrowRect.rotation = Quaternion.Euler(0, 0, angle);
    }

    // --- Logique techs ---

    void TryUnlock(string nodeId)
    {
        var node = techTreeData.techNodes.Find(n => n.id == nodeId);
        if (node == null || node.unlocked) return;

        if (!ArePrerequisitesMet(node)) return;
        if (!ResourceManager.Instance.HasEnough(ResourceType.Search, node.cost)) return;

        ResourceManager.Instance.Spend(ResourceType.Search, node.cost);

        node.unlocked = true;
        UpdateNodeVisual(nodeButtons[nodeId], node);
        ApplyTechEffect(node.id);

        foreach (var n in techTreeData.techNodes)
            UpdateNodeVisual(nodeButtons[n.id], n);
    }

    void UpdateNodeVisual(GameObject button, TechNode node)
    {
        var image = button.GetComponent<Image>();
        var btn = button.GetComponent<Button>();

        if (node.unlocked)
        {
            image.color = Color.green;
            btn.interactable = false; // débloqué = non-cliquable
        }
        else if (ArePrerequisitesMet(node))
        {
            image.color = Color.white;
            btn.interactable = true;  // prêt à être débloqué
        }
        else
        {
            image.color = Color.gray;
            btn.interactable = false; // verrouillé
        }
    }


    bool ArePrerequisitesMet(TechNode node)
    {
        return node.prerequisiteIds.All(id =>
            techTreeData.techNodes.Find(n => n.id == id)?.unlocked == true);
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
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "10":
                AgeManager.Instance.AdvanceToNextAge();
                break;
        }
    }
}
