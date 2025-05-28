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

        // Créer le conteneur de flèches APRÈS les nœuds (pour qu'il soit devant)
        if (arrowContainer != null) Destroy(arrowContainer);
        
        var graph = BuildTreeGraph();

        // Trouver racines (sans prerequisite)
        var roots = graph.Values
            .Where(n => n.data.prerequisiteIds == null || n.data.prerequisiteIds.Count == 0)
            .ToList();

        float startX = 0;
        foreach (var root in roots)
            startX = LayoutTree(root, startX);

        // Générer boutons D'ABORD
        foreach (var node in graph.Values)
            InstantiateTreeNode(node);

        // Créer le conteneur de flèches APRÈS les nœuds
        arrowContainer = new GameObject("ArrowLines");
        arrowContainer.transform.SetParent(nodesParent, false);
        
        // Ajouter un Canvas Group pour contrôler l'ordre de rendu
        CanvasGroup arrowCanvasGroup = arrowContainer.AddComponent<CanvasGroup>();
        arrowCanvasGroup.interactable = false;
        arrowCanvasGroup.blocksRaycasts = false;

        // Générer flèches APRÈS (pour avoir les positions correctes)
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
        // Vérification du prefab
        if (arrowPrefab == null)
        {
            Debug.LogError("ArrowPrefab n'est pas assigné !");
            CreateSimpleArrow(fromBtn, toBtn); // Fallback
            return;
        }

        GameObject arrow = Instantiate(arrowPrefab, arrowContainer.transform);
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();

        if (arrowRect == null)
        {
            Debug.LogError("Le prefab de flèche n'a pas de RectTransform !");
            return;
        }

        Vector2 start = fromBtn.GetComponent<RectTransform>().anchoredPosition;
        Vector2 end = toBtn.GetComponent<RectTransform>().anchoredPosition;

        Vector2 direction = end - start;
        float distance = direction.magnitude;

        // Configuration pour remplir toute la zone
        arrowRect.anchorMin = Vector2.zero;
        arrowRect.anchorMax = Vector2.one;
        arrowRect.offsetMin = Vector2.zero;
        arrowRect.offsetMax = Vector2.zero;
        
        // Taille correcte pour la distance
        arrowRect.sizeDelta = new Vector2(distance, 8f);
        arrowRect.anchoredPosition = start + direction / 2;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrowRect.rotation = Quaternion.Euler(0, 0, angle);

        // S'assurer que la flèche est visible au premier plan
        Image arrowImage = arrow.GetComponent<Image>();
        if (arrowImage != null)
        {
            // Forcer un sprite visible si celui du prefab ne fonctionne pas
            if (arrowImage.sprite == null)
            {
                // Utiliser le sprite par défaut d'Unity (carré blanc)
                arrowImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            }
            
            arrowImage.color = Color.red; // Rouge pour être bien visible
            arrowImage.raycastTarget = false; // Évite d'intercepter les clics
            arrowImage.type = Image.Type.Sliced; // Pour bien s'étirer
        }

        // Mettre la flèche au premier plan
        arrow.transform.SetAsLastSibling();

        Debug.Log($"Flèche créée de {fromBtn.name} vers {toBtn.name} - Distance: {distance}");
    }

    // Méthode fallback pour créer une flèche simple si le prefab manque
    void CreateSimpleArrow(GameObject fromBtn, GameObject toBtn)
    {
        GameObject arrow = new GameObject("SimpleArrow");
        arrow.transform.SetParent(arrowContainer.transform, false);
        
        Image arrowImage = arrow.AddComponent<Image>();
        arrowImage.color = Color.yellow; // Couleur visible pour debug
        arrowImage.raycastTarget = false;
        
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        
        Vector2 start = fromBtn.GetComponent<RectTransform>().anchoredPosition;
        Vector2 end = toBtn.GetComponent<RectTransform>().anchoredPosition;

        Vector2 direction = end - start;
        float distance = direction.magnitude;

        // Configuration pour remplir la zone correctement
        arrowRect.anchorMin = Vector2.zero;
        arrowRect.anchorMax = Vector2.one;
        arrowRect.offsetMin = Vector2.zero;
        arrowRect.offsetMax = Vector2.zero;
        
        arrowRect.sizeDelta = new Vector2(distance, 10f); // Plus épais pour être visible
        arrowRect.anchoredPosition = start + direction / 2;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrowRect.rotation = Quaternion.Euler(0, 0, angle);
        
        // Mettre au premier plan
        arrow.transform.SetAsLastSibling();
        
        Debug.Log($"Flèche simple créée de {fromBtn.name} vers {toBtn.name}");
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
        if (node.prerequisiteIds == null || node.prerequisiteIds.Count == 0)
            return true;
            
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