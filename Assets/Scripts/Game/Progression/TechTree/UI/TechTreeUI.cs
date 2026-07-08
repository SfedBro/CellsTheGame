using UnityEngine;

public class TechTreeUI : MonoBehaviour
{
    public static TechTreeUI Instance;

    public GameObject rootPanel;
    
    [Header("Line Settings")]
    public TechTreeLineUI linePrefab;
    public Transform linesContainer;

    private TechTreeNodeUI[] nodes;
    private System.Collections.Generic.List<TechTreeLineUI> generatedLines = new System.Collections.Generic.List<TechTreeLineUI>();

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        Instance = this;
        if (rootPanel != null) rootPanel.SetActive(false);

        inputActions = new InputSystem_Actions();
        inputActions.UI.OpenTechTree.performed += _ => ToggleWindow();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Start()
    {
        // Cache all nodes in the tree
        nodes = GetComponentsInChildren<TechTreeNodeUI>(true);
        GenerateLines();
    }

    private void GenerateLines()
    {
        if (linePrefab == null || linesContainer == null || nodes == null) return;

        foreach (var targetNode in nodes)
        {
            if (targetNode.nodeData == null || targetNode.nodeData.dependencies == null) continue;

            foreach (var depData in targetNode.nodeData.dependencies)
            {
                if (depData == null) continue;

                // Find source node UI
                TechTreeNodeUI sourceNode = System.Array.Find(nodes, n => n.nodeData == depData);
                if (sourceNode != null)
                {
                    TechTreeLineUI line = Instantiate(linePrefab, linesContainer);
                    line.Setup(sourceNode, targetNode);
                    generatedLines.Add(line);
                }
            }
        }
    }

    private void ToggleWindow()
    {
        if (rootPanel != null && rootPanel.activeSelf)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        if (rootPanel == null) return;
        
        // When opening Tech Tree, you might want to close Build Mode or other windows
        BuildManager buildManager = FindFirstObjectByType<BuildManager>();
        if (buildManager != null && buildManager.IsBuildMode)
        {
            // Optional: exit build mode, or just ignore
        }

        if (PlayerInventoryWindow.Instance != null)
        {
            PlayerInventoryWindow.Instance.Close();
        }

        rootPanel.SetActive(true);
        RefreshTree();
    }

    public void Close()
    {
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }
    }

    public void RefreshTree()
    {
        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                node.Refresh();
            }
        }

        if (generatedLines != null)
        {
            foreach (var line in generatedLines)
            {
                line.Refresh();
            }
        }
    }
}
