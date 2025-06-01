using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Classe représentant un nœud logique dans l'arbre technologique
class TreeNode
{
    public TechNode data; // Les données techniques liées à ce nœud
    public List<TreeNode> children = new(); // Liste des enfants (techs dépendantes)
    public int depth = 0; // Profondeur dans l’arbre
    public Vector2 position; // Position sur l’UI
}

public class TechTreeUI : MonoBehaviour
{
    [Header("Références")]
    public GameObject treePanel; // Panneau global de l’arbre
    public GameObject techNodePrefab; // Préfab pour chaque technologie
    public Transform nodesParent; // Le content du ScrollRect
    public TechTreeData techTreeData; // Les données de l’arbre

    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private BuildingBarUI buildingBarUI;

    private Dictionary<string, GameObject> nodeButtons = new(); // Dictionnaire des boutons associés aux nodes
    private GameObject arrowContainer; // Container pour les flèches

    [SerializeField] private float nodeSpacingX = 250f;
    [SerializeField] private float nodeSpacingY = 200f;

    void Start()
    {
        treePanel.SetActive(false); // On cache l’arbre au début
        foreach (var node in techTreeData.techNodes)
        {
            node.unlocked = false; // On verrouille tout au départ
        }
        LayoutAndInstantiateTree(); // On génère l’UI
    }

    public void ToggleTree()
    {
        treePanel.SetActive(!treePanel.activeSelf); // Ouvre ou ferme l’arbre
    }

    // Positionne et instancie tous les éléments du tech tree
    void LayoutAndInstantiateTree()
    {
        // Nettoyage des anciens éléments
        foreach (Transform child in nodesParent) Destroy(child.gameObject);
        nodeButtons.Clear();
        if (arrowContainer != null) Destroy(arrowContainer);

        // Création du graphe logique
        var graph = BuildTreeGraph();

        // On récupère les racines (pas de prérequis)
        var roots = graph.Values
            .Where(n => n.data.prerequisiteIds == null || n.data.prerequisiteIds.Count == 0)
            .ToList();

        // Placement des racines et de leurs enfants
        float startX = -50;
        foreach (var root in roots)
            startX = LayoutTree(root, startX);

        // Calcul des extrémités de l’arbre
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var node in graph.Values)
        {
            minX = Mathf.Min(minX, node.position.x);
            maxX = Mathf.Max(maxX, node.position.x);
            minY = Mathf.Min(minY, node.position.y);
            maxY = Mathf.Max(maxY, node.position.y);
        }

        // Décalage des positions pour tout voir à l’écran
        float margeX = 30f;
        float margeY = 30f;

        foreach (var node in graph.Values)
        {
            node.position.x = node.position.x - minX + margeX;
            node.position.y = node.position.y - maxY + margeY;
        }

        // Instanciation de chaque nœud (bouton UI)
        foreach (var node in graph.Values)
            InstantiateTreeNode(node);

        // Taille du ScrollRect content
        float treeWidth = (maxX - minX) + nodeSpacingX + margeX * 2;
        float treeHeight = (maxY - minY) + nodeSpacingY + margeY * 2;
        var nodesParentRect = nodesParent.GetComponent<RectTransform>();
        nodesParentRect.sizeDelta = new Vector2(treeWidth, treeHeight);

        // Création des flèches entre les nœuds
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

        // Mise à jour de la couleur des nœuds
        foreach (var node in techTreeData.techNodes){
            UpdateNodeVisual(nodeButtons[node.id], node);}

        // Ajustement final : recentrage
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

        Vector2 offset = new Vector2(padding - min.x, padding - max.y);
        foreach (var kv in nodeButtons)
        {
            RectTransform rt = kv.Value.GetComponent<RectTransform>();
            rt.anchoredPosition += offset;
        }
    }

    // Crée un bouton pour une tech
    void InstantiateTreeNode(TreeNode node)
    {
        GameObject btn = Instantiate(techNodePrefab, nodesParent);
        btn.name = node.data.id;

        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchoredPosition = node.position;

        // Affichage icône
        Transform iconTransform = btn.transform.Find("Icon");
        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null)
                iconImage.sprite = node.data.icon;
        }

        // Affichage coût
        Transform costTransform = btn.transform.Find("Cost");
        if (costTransform != null)
        {
            TMP_Text costText = costTransform.GetComponent<TMP_Text>();
            if (costText != null)
                costText.text = $"{node.data.cost}";
        }

        // Affichage nom
        Transform nameTransform = btn.transform.Find("Name");
        if (nameTransform != null)
        {
            TMP_Text nameText = nameTransform.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = node.data.displayName;
        }

        // Ajout du clic
        btn.GetComponent<Button>().onClick.AddListener(() => TryUnlock(node.data.id));
        nodeButtons[node.data.id] = btn;
    }

    // Crée une flèche entre deux nœuds
    void DrawArrow(GameObject fromBtn, GameObject toBtn)
    {
        if (arrowPrefab == null)
        {
            Debug.LogError("ArrowPrefab n'est pas assigné !");
            CreateSimpleArrow(fromBtn, toBtn);
            return;
        }

        GameObject arrow = Instantiate(arrowPrefab, arrowContainer.transform);
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();

        if (arrowRect == null)
        {
            Debug.LogError("Le prefab de flèche n'a pas de RectTransform !");
            return;
        }

        RectTransform fromRT = fromBtn.GetComponent<RectTransform>();
        RectTransform toRT = toBtn.GetComponent<RectTransform>();

        Vector2 fromPos = fromRT.anchoredPosition;
        Vector2 toPos = toRT.anchoredPosition;

        fromPos.y += -200f;
        toPos.y += -200f;
        fromPos.x += 150f;
        toPos.x += 150f;

        Vector2 direction = toPos - fromPos;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        arrowRect.anchorMin = new Vector2(0, 1);
        arrowRect.anchorMax = new Vector2(0, 1);
        arrowRect.pivot = new Vector2(0, 0.5f);
        arrowRect.sizeDelta = new Vector2(distance, 8f);
        arrowRect.anchoredPosition = fromPos;
        arrowRect.rotation = Quaternion.Euler(0, 0, angle);

        Image arrowImage = arrow.GetComponent<Image>();
        if (arrowImage != null)
        {
            if (arrowImage.sprite == null)
                arrowImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");

            arrowImage.color = Color.white;
            arrowImage.raycastTarget = false;
            arrowImage.type = Image.Type.Sliced;
        }

        arrow.transform.SetAsLastSibling();
    }

    // Crée une flèche simple si le prefab n’est pas assigné
    void CreateSimpleArrow(GameObject fromBtn, GameObject toBtn)
    {
        GameObject arrow = new GameObject("SimpleArrow", typeof(RectTransform));
        arrow.transform.SetParent(arrowContainer.transform, false);

        Image arrowImage = arrow.AddComponent<Image>();
        arrowImage.color = Color.white;
        arrowImage.raycastTarget = false;
        arrowImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");

        RectTransform arrowRect = arrow.GetComponent<RectTransform>();

        Vector2 fromPos = fromBtn.GetComponent<RectTransform>().anchoredPosition;
        Vector2 toPos = toBtn.GetComponent<RectTransform>().anchoredPosition;

        Vector2 direction = toPos - fromPos;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        arrowRect.anchorMin = new Vector2(0, 1);
        arrowRect.anchorMax = new Vector2(0, 1);
        arrowRect.pivot = new Vector2(0, 0.5f);
        arrowRect.sizeDelta = new Vector2(distance, 8f);
        arrowRect.anchoredPosition = fromPos;
        arrowRect.rotation = Quaternion.Euler(0, 0, angle);
    }

    // Construit le graphe logique des dépendances
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

    // Calcule la position logique d’un arbre récursivement
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

    // Gère le clic sur un nœud pour débloquer la tech
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

    // Met à jour l’apparence visuelle du bouton selon son état
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

    // Vérifie si les prérequis sont remplis pour une tech
    bool ArePrerequisitesMet(TechNode node)
    {
        if (node.prerequisiteIds == null || node.prerequisiteIds.Count == 0)
            return true;
        return node.prerequisiteIds.All(id =>
            techTreeData.techNodes.Find(n => n.id == id)?.unlocked == true);
    }

    // Applique l’effet d’une technologie (débloque des bâtiments ou des âges)
    void ApplyTechEffect(string techId)
    {
        switch (techId)
        {
            case "0": BuildingManager.UnlockBuilding("Puit"); break;
            case "3": BuildingManager.UnlockBuilding("Ferme de Baie"); break;
            case "4": BuildingManager.UnlockBuilding("Zone de bois"); break;
            case "6": BuildingManager.UnlockBuilding("Mine de Pierre"); break;
            case "7": BuildingManager.UnlockBuilding("Port"); break;
            case "10": AgeManager.Instance.AdvanceToNextAge(); break;
            case "11": BuildingManager.UnlockBuilding("Maison Pour 4"); break;
            case "12": BuildingManager.UnlockBuilding("Ferme de blé"); break;
            case "13": BuildingManager.UnlockBuilding("Peche"); break;
            case "14": BuildingManager.UnlockBuilding("Etable"); break;
            case "15": BuildingManager.UnlockBuilding("Scierie"); break;
            case "16": BuildingManager.UnlockBuilding("Eglise"); break;
            case "20": AgeManager.Instance.AdvanceToNextAge(); break;
            case "21": BuildingManager.UnlockBuilding("Maison Pour 6"); break;
            case "22": BuildingManager.UnlockBuilding("Bar"); break;
            case "23": BuildingManager.UnlockBuilding("Forge"); break;
            case "24": BuildingManager.UnlockBuilding("Mine de fer"); break;
            case "25": BuildingManager.UnlockBuilding("Stockage Avance"); break;
            case "26": BuildingManager.UnlockBuilding("Bibliotheque"); break;
            case "30": AgeManager.Instance.AdvanceToNextAge(); break;
            case "31": BuildingManager.UnlockBuilding("Immeuble"); break;
            case "32": BuildingManager.UnlockBuilding("Mine d'or"); break;
            case "33": BuildingManager.UnlockBuilding("Boulangerie industrielle"); break;
            case "34": BuildingManager.UnlockBuilding("Usine d'outil"); break;
            case "35": BuildingManager.UnlockBuilding("Entrepot"); break;
            case "36": BuildingManager.UnlockBuilding("Grand port"); break;
            case "37": BuildingManager.UnlockBuilding("Laboratoire"); break;
        }

        buildingBarUI.RefreshBar(AgeManager.Instance.GetCurrentAge());
    }
}
