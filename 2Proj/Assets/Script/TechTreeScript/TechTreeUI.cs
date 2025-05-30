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
    public Transform nodesParent; // Content du ScrollRect
    public TechTreeData techTreeData;

    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private BuildingBarUI buildingBarUI;

    private Dictionary<string, GameObject> nodeButtons = new();
    private GameObject arrowContainer;

    // Espacement configurable
    [SerializeField] private float nodeSpacingX = 250f;
    [SerializeField] private float nodeSpacingY = 200f;

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

    // --- AFFICHAGE ET LAYOUT ---
    void LayoutAndInstantiateTree()
    {
        // 1. Nettoyage ancien affichage
        foreach (Transform child in nodesParent) Destroy(child.gameObject);
        nodeButtons.Clear();
        if (arrowContainer != null) Destroy(arrowContainer);

        // 2. Création du graphe logique
        var graph = BuildTreeGraph();

        // 3. Détection des racines
        var roots = graph.Values
            .Where(n => n.data.prerequisiteIds == null || n.data.prerequisiteIds.Count == 0)
            .ToList();

        // 4. Placement logique de chaque nœud (LayoutTree modifie node.position)
        float startX = -50;
        foreach (var root in roots)
            startX = LayoutTree(root, startX);

        // 5. Calculer min/max
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var node in graph.Values)
        {
            minX = Mathf.Min(minX, node.position.x);
            maxX = Mathf.Max(maxX, node.position.x);
            minY = Mathf.Min(minY, node.position.y);
            maxY = Mathf.Max(maxY, node.position.y);
        }

        // 6. Décaler tous les noeuds pour minX=0 (gauche) et maxY=0 (haut), avec marge
        float margeX = 30f; // marge à gauche
        float margeY = 30f; // marge en haut

        foreach (var node in graph.Values)
        {
            node.position.x = node.position.x - minX + margeX;
            node.position.y = node.position.y - maxY + margeY;
        }

        // 7. Instanciation des boutons aux bonnes positions
        foreach (var node in graph.Values)
            InstantiateTreeNode(node);

        // 8. Détermination de la taille idéale du content
        float treeWidth = (maxX - minX) + nodeSpacingX + margeX * 2;
        float treeHeight = (maxY - minY) + nodeSpacingY + margeY * 2;
        var nodesParentRect = nodesParent.GetComponent<RectTransform>();
        nodesParentRect.sizeDelta = new Vector2(treeWidth, treeHeight);

        // 9. PAS de centrage automatique ! Content est ancré haut gauche.

        // 10. Génération des flèches (APRÈS tous les boutons)
        arrowContainer = new GameObject("ArrowLines");
        arrowContainer.transform.SetParent(nodesParent, false);
        CanvasGroup arrowCanvasGroup = arrowContainer.AddComponent<CanvasGroup>();
        arrowCanvasGroup.interactable = false;
        arrowCanvasGroup.blocksRaycasts = false;

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

        // 11. Met à jour la couleur/état de chaque nœud selon son statut
        foreach (var node in techTreeData.techNodes){
            UpdateNodeVisual(nodeButtons[node.id], node);}

        // === Ajustement dynamique du content ===
        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;

        foreach (var node in graph.Values)
        {
            Vector2 pos = node.position;
            min = Vector2.Min(min, pos);
            max = Vector2.Max(max, pos);
        }

        float padding = 200f;

        RectTransform contentRT = nodesParent.GetComponent<RectTransform>();
        contentRT.sizeDelta = new Vector2(
            max.x - min.x + padding * 2,
            Mathf.Abs(min.y - max.y) + padding * 2
        );

        // Recentrage des nodes
        Vector2 offset = new Vector2(padding - min.x, padding - max.y);
        foreach (var kv in nodeButtons)
        {
            RectTransform rt = kv.Value.GetComponent<RectTransform>();
            rt.anchoredPosition += offset;
        }


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
        if (arrowPrefab == null)
        {
            Debug.LogError("ArrowPrefab n'est pas assigné !");
            CreateSimpleArrow(fromBtn, toBtn); // fallback
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

        arrowRect.anchorMin = Vector2.zero;
        arrowRect.anchorMax = Vector2.one;
        arrowRect.offsetMin = Vector2.zero;
        arrowRect.offsetMax = Vector2.zero;
        arrowRect.sizeDelta = new Vector2(distance, 8f);
        arrowRect.anchoredPosition = start + direction / 2f;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrowRect.rotation = Quaternion.Euler(0, 0, angle);

        Image arrowImage = arrow.GetComponent<Image>();
        if (arrowImage != null)
        {
            if (arrowImage.sprite == null)
                arrowImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            arrowImage.color = Color.red;
            arrowImage.raycastTarget = false;
            arrowImage.type = Image.Type.Sliced;
        }

        arrow.transform.SetAsLastSibling();
    }

    void CreateSimpleArrow(GameObject fromBtn, GameObject toBtn)
    {
        GameObject arrow = new GameObject("SimpleArrow");
        arrow.transform.SetParent(arrowContainer.transform, false);

        Image arrowImage = arrow.AddComponent<Image>();
        arrowImage.color = Color.yellow;
        arrowImage.raycastTarget = false;

        RectTransform arrowRect = arrow.GetComponent<RectTransform>();

        Vector2 start = fromBtn.GetComponent<RectTransform>().anchoredPosition;
        Vector2 end = toBtn.GetComponent<RectTransform>().anchoredPosition;
        Vector2 direction = end - start;
        float distance = direction.magnitude;

        arrowRect.anchorMin = Vector2.zero;
        arrowRect.anchorMax = Vector2.one;
        arrowRect.offsetMin = Vector2.zero;
        arrowRect.offsetMax = Vector2.zero;
        arrowRect.sizeDelta = new Vector2(distance, 10f);
        arrowRect.anchoredPosition = start + direction / 2;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrowRect.rotation = Quaternion.Euler(0, 0, angle);

        arrow.transform.SetAsLastSibling();
    }

    // --- LOGIQUE DES NOEUDS (identique à la tienne) ---
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

    float LayoutTree(TreeNode node, float x = -50)
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

    // --- LOGIQUE DE DEBLOCAGE ---
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
            btn.interactable = false;
        }
        else if (ArePrerequisitesMet(node))
        {
            image.color = Color.white;
            btn.interactable = true;
        }
        else
        {
            image.color = Color.gray;
            btn.interactable = false;
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
            case "2":
                BuildingManager.UnlockBuilding("Stockage");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "3":
                BuildingManager.UnlockBuilding("Ferme de Baie");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "4":
                BuildingManager.UnlockBuilding("Zone de bois");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "5":
                // Sac +
                break;
            case "6":
                BuildingManager.UnlockBuilding("Mine de Pierre");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "7":
                BuildingManager.UnlockBuilding("Port");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "10":
                AgeManager.Instance.AdvanceToNextAge();
                break;
            case "11":
                BuildingManager.UnlockBuilding("Maison Pour 4");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "12":
                BuildingManager.UnlockBuilding("Ferme de blé");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "13":
                BuildingManager.UnlockBuilding("Peche");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "14":
                BuildingManager.UnlockBuilding("Etable");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "15":
                BuildingManager.UnlockBuilding("Scierie");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "16":
                BuildingManager.UnlockBuilding("Eglise");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "20":
                AgeManager.Instance.AdvanceToNextAge();
                break;
            case "21":
                BuildingManager.UnlockBuilding("Maison Pour 6");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "22":
                BuildingManager.UnlockBuilding("Bar");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "23":
                BuildingManager.UnlockBuilding("Forge");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "24":
                BuildingManager.UnlockBuilding("Mine de fer");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "25":
                BuildingManager.UnlockBuilding("Stockage Avance");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "26":
                BuildingManager.UnlockBuilding("Bibliotheque");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "30":
                AgeManager.Instance.AdvanceToNextAge();
                break;
            case "31":
                BuildingManager.UnlockBuilding("Immeuble");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "32":
                BuildingManager.UnlockBuilding("Mine d'or");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "33":
                BuildingManager.UnlockBuilding("Boulangerie industrielle");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "34":
                BuildingManager.UnlockBuilding("Usine d'outil");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "35":
                BuildingManager.UnlockBuilding("Entrepot");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "36":
                BuildingManager.UnlockBuilding("Grand port");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
            case "37":
                BuildingManager.UnlockBuilding("Laboratoire");
                buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
                break;
        }
    }

}
