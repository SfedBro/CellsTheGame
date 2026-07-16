using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public enum InventoryLayoutMode
{
    Split,          // 50% Left, 50% Right
    MaximizedLeft,  // 100% Left, Right hidden
    MaximizedRight  // 100% Right, Left hidden
}

public class PlayerInventoryWindow : MonoBehaviour
{
    public static PlayerInventoryWindow Instance;

    [Header("References")]
    public GameObject rootPanel;
    public InventoryPanelUI leftPanel;
    public InventoryPanelUI rightPanel;

    [Header("Default UI Panels")]
    public GameObject defaultPanelPrefab;
    private GameObject instantiatedRightPanelPrefabSource;

    [Header("Layout Docking Preview")]
    public RectTransform previewOverlay; // Translucent indicator panel for docking preview

    [Header("Drag and Drop (Modules)")]
    public Canvas canvas;
    public RectTransform dragIconTransform;
    public Image dragIconImage;

    [Header("Input")]
    private InputSystem_Actions inputActions;

    private ModuleSlotUI currentlyDraggingSlot;
    private bool isWindowOpen = false;
    private InventoryLayoutMode currentLayoutMode = InventoryLayoutMode.Split;

    // Selected machine state
    private MonoBehaviour selectedMachineBlock;
    private IInventoryProvider selectedInventoryProvider;
    private ICraftingProvider selectedCraftingProvider;

    public IInventoryProvider SelectedInventoryProvider => selectedInventoryProvider;
    public ICraftingProvider SelectedCraftingProvider => selectedCraftingProvider;

    // Highlighting dictionary to restore original colors
    private Dictionary<SpriteRenderer, Color> highlightedRenderers = new Dictionary<SpriteRenderer, Color>();

    private void Awake()
    {
        Instance = this;

        inputActions = new InputSystem_Actions();
        inputActions.UI.OpenInventory.performed += _ => 
        {
            BuildManager buildManager = FindFirstObjectByType<BuildManager>();
            bool isBuilding = buildManager != null && (buildManager.IsBuildMode || buildManager.IsEditMode);
            if (!isBuilding)
            {
                ToggleWindow();
            }
        };

        ValidateReferences();
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
        if (rootPanel != null) rootPanel.SetActive(false);
        if (dragIconTransform != null) dragIconTransform.gameObject.SetActive(false);
        if (previewOverlay != null) previewOverlay.gameObject.SetActive(false);

        // Initialize panels
        if (leftPanel != null) leftPanel.Initialize(this, PanelTabType.Factory);
        if (rightPanel != null) rightPanel.Initialize(this, PanelTabType.Foraging);

        SetLayoutMode(InventoryLayoutMode.Split);
    }

    private void OnDestroy()
    {
        if (isWindowOpen)
        {
            UnsubscribeEvents();
        }
        UnsubscribeFromMachineEvents();
    }

    public void ToggleWindow()
    {
        if (isWindowOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (rootPanel == null) return;

        // Close other conflicting windows if they exist
        if (PauseMenu.Instance != null && PauseMenu.Instance.IsPaused) return;

        rootPanel.SetActive(true);
        isWindowOpen = true;

        SubscribeEvents();
        SetLayoutMode(currentLayoutMode); // Re-apply current layout mode
        UpdateMachineHighlightState();
    }

    public void Close()
    {
        if (rootPanel == null) return;

        rootPanel.SetActive(false);
        isWindowOpen = false;

        UnsubscribeEvents();

        if (dragIconTransform != null) dragIconTransform.gameObject.SetActive(false);
        if (previewOverlay != null) previewOverlay.gameObject.SetActive(false);
        currentlyDraggingSlot = null;

        UpdateMachineHighlightState();
    }

    private void SubscribeEvents()
    {
        if (PlayerInventory.Instance != null)
        {
            if (PlayerInventory.Instance.FactoryInventory != null)
                PlayerInventory.Instance.FactoryInventory.OnInventoryChanged += RefreshWindow;
            if (PlayerInventory.Instance.ForagingInventory != null)
                PlayerInventory.Instance.ForagingInventory.OnInventoryChanged += RefreshWindow;
        }

        if (PlayerModuleManager.Instance != null)
        {
            PlayerModuleManager.Instance.OnModuleEquipStatusChanged += OnModuleStatusChanged;
        }

        SubscribeToMachineEvents();
    }

    private void UnsubscribeEvents()
    {
        if (PlayerInventory.Instance != null)
        {
            if (PlayerInventory.Instance.FactoryInventory != null)
                PlayerInventory.Instance.FactoryInventory.OnInventoryChanged -= RefreshWindow;
            if (PlayerInventory.Instance.ForagingInventory != null)
                PlayerInventory.Instance.ForagingInventory.OnInventoryChanged -= RefreshWindow;
        }

        if (PlayerModuleManager.Instance != null)
        {
            PlayerModuleManager.Instance.OnModuleEquipStatusChanged -= OnModuleStatusChanged;
        }

        UnsubscribeFromMachineEvents();
    }

    private void OnModuleStatusChanged(PlayerModule module, bool isEquipped)
    {
        RefreshWindow();
    }

    public void RefreshWindow()
    {
        if (leftPanel != null && leftPanel.gameObject.activeSelf) leftPanel.RefreshView();
        if (rightPanel != null && rightPanel.gameObject.activeSelf) rightPanel.RefreshView();
    }



    private void ValidateReferences()
    {
        List<string> missingFields = new List<string>();

        if (rootPanel == null) missingFields.Add("PlayerInventoryWindow.rootPanel");
        if (leftPanel == null) missingFields.Add("PlayerInventoryWindow.leftPanel");
        if (rightPanel == null) missingFields.Add("PlayerInventoryWindow.rightPanel");

        if (leftPanel != null) ValidatePanelReferences(leftPanel, "LeftPanel", missingFields);
        if (rightPanel != null) ValidatePanelReferences(rightPanel, "RightPanel", missingFields);

        if (missingFields.Count > 0)
        {
            string errorMessage = "<b>[Inventory UI Error]</b> Missing critical references in Inspector:\n" + 
                                  string.Join("\n", missingFields.ConvertAll(field => "  - <color=red>" + field + "</color>"));
            Debug.LogError(errorMessage, this);
        }
        else
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    canvas = Object.FindFirstObjectByType<Canvas>();
                }
            }
        }
    }

    private void ValidatePanelReferences(InventoryPanelUI panel, string panelName, List<string> missingFields)
    {
        if (panel.itemSlotPrefab == null) missingFields.Add($"{panelName}.itemSlotPrefab");
        if (panel.moduleSlotPrefab == null) missingFields.Add($"{panelName}.moduleSlotPrefab");

        if (panel.factorySlotsContainer == null) missingFields.Add($"{panelName}.factorySlotsContainer");
        if (panel.foragingSlotsContainer == null) missingFields.Add($"{panelName}.foragingSlotsContainer");
        if (panel.modulesSlotsContainer == null) missingFields.Add($"{panelName}.modulesSlotsContainer");

        if (panel.factoryView == null) missingFields.Add($"{panelName}.factoryView");
        if (panel.foragingView == null) missingFields.Add($"{panelName}.foragingView");
        if (panel.modulesView == null) missingFields.Add($"{panelName}.modulesView");
        if (panel.machineView == null) missingFields.Add($"{panelName}.machineView");
    }
    #endregion

    #region Selected Machine Management

    public void SelectMachine(MonoBehaviour machineBlock)
    {
        // Unsubscribe from previous events
        UnsubscribeFromMachineEvents();
        // Remove highlight from previous machine
        SetMachineHighlight(selectedMachineBlock, false);

        selectedMachineBlock = machineBlock;

        if (machineBlock != null)
        {
            selectedInventoryProvider = machineBlock.GetComponent<IInventoryProvider>();
            selectedCraftingProvider = machineBlock.GetComponent<ICraftingProvider>();
        }
        else
        {
            selectedInventoryProvider = null;
            selectedCraftingProvider = null;
        }

        // Swap panel UI if the machine block has a custom UI prefab
        GameObject targetPrefab = defaultPanelPrefab;
        if (machineBlock != null)
        {
            FactoryBlock block = machineBlock.GetComponent<FactoryBlock>();
            if (block != null && block.customInventoryPanelPrefab != null)
            {
                targetPrefab = block.customInventoryPanelPrefab;
            }
        }

        if (targetPrefab != instantiatedRightPanelPrefabSource)
        {
            if (rightPanel != null)
            {
                Destroy(rightPanel.gameObject);
            }

            GameObject rightPanelGo = null;
            if (targetPrefab != null)
            {
                rightPanelGo = Instantiate(targetPrefab, rootPanel.transform);
            }
            else
            {
                Debug.LogWarning("[PlayerInventoryWindow] No targetPrefab or defaultPanelPrefab set for right panel swap!");
            }

            if (rightPanelGo != null)
            {
                rightPanelGo.name = "RightPanel";
                rightPanelGo.transform.SetSiblingIndex(1);
                rightPanel = rightPanelGo.GetComponent<InventoryPanelUI>();
                instantiatedRightPanelPrefabSource = targetPrefab;

                if (rightPanel != null)
                {
                    rightPanel.Initialize(this, PanelTabType.Machine);
                    rightPanel.gameObject.SetActive(true);
                }
            }
        }

        // Subscribe to new events
        SubscribeToMachineEvents();
        // Apply highlight based on tab visibility
        UpdateMachineHighlightState();
        
        RefreshWindow();
    }

    private void SubscribeToMachineEvents()
    {
        if (selectedInventoryProvider != null && selectedInventoryProvider.Inventory != null)
        {
            selectedInventoryProvider.Inventory.OnInventoryChanged += RefreshWindow;
        }
        if (selectedCraftingProvider != null)
        {
            if (selectedCraftingProvider.InputInventory != null)
                selectedCraftingProvider.InputInventory.OnInventoryChanged += RefreshWindow;
            if (selectedCraftingProvider.OutputInventory != null)
                selectedCraftingProvider.OutputInventory.OnInventoryChanged += RefreshWindow;
        }
    }

    private void UnsubscribeFromMachineEvents()
    {
        if (selectedInventoryProvider != null && selectedInventoryProvider.Inventory != null)
        {
            selectedInventoryProvider.Inventory.OnInventoryChanged -= RefreshWindow;
        }
        if (selectedCraftingProvider != null)
        {
            if (selectedCraftingProvider.InputInventory != null)
                selectedCraftingProvider.InputInventory.OnInventoryChanged -= RefreshWindow;
            if (selectedCraftingProvider.OutputInventory != null)
                selectedCraftingProvider.OutputInventory.OnInventoryChanged -= RefreshWindow;
        }
    }

    public void UpdateMachineHighlightState()
    {
        bool isMachineTabVisible = false;
        
        if (isWindowOpen)
        {
            if (leftPanel != null && leftPanel.gameObject.activeSelf && leftPanel.CurrentTab == PanelTabType.Machine)
            {
                isMachineTabVisible = true;
            }
            if (rightPanel != null && rightPanel.gameObject.activeSelf && rightPanel.CurrentTab == PanelTabType.Machine)
            {
                isMachineTabVisible = true;
            }
        }

        SetMachineHighlight(selectedMachineBlock, isMachineTabVisible);
    }

    private void SetMachineHighlight(MonoBehaviour machine, bool highlight)
    {
        if (machine == null) return;

        SpriteRenderer[] renderers = machine.GetComponentsInChildren<SpriteRenderer>();

        if (highlight)
        {
            Color highlightColor = new Color(0.6f, 0.8f, 1f, 1f); // Cyan highlight tint
            
            foreach (var sr in renderers)
            {
                if (!highlightedRenderers.ContainsKey(sr))
                {
                    highlightedRenderers[sr] = sr.color; // Save original color
                }
                sr.color = highlightColor;
            }
        }
        else
        {
            foreach (var sr in renderers)
            {
                if (highlightedRenderers.TryGetValue(sr, out Color originalColor))
                {
                    if (sr != null) sr.color = originalColor; // Restore original color
                }
            }
            // Clear highlights for the restored renderers
            foreach (var sr in renderers)
            {
                highlightedRenderers.Remove(sr);
            }
        }
    }

    #endregion

    #region Layout Management & Tab Docking

    public void SetLayoutMode(InventoryLayoutMode mode)
    {
        currentLayoutMode = mode;
        
        if (leftPanel == null || rightPanel == null) return;

        switch (currentLayoutMode)
        {
            case InventoryLayoutMode.Split:
                leftPanel.gameObject.SetActive(true);
                rightPanel.gameObject.SetActive(true);
                break;
            case InventoryLayoutMode.MaximizedLeft:
                leftPanel.gameObject.SetActive(true);
                rightPanel.gameObject.SetActive(false);
                break;
            case InventoryLayoutMode.MaximizedRight:
                leftPanel.gameObject.SetActive(false);
                rightPanel.gameObject.SetActive(true);
                break;
        }

        // Active tab visibility changed, update highlighting
        UpdateMachineHighlightState();
        RefreshWindow();
    }

    public void OnTabDragBegin(TabDragHandler dragHandler)
    {
        if (previewOverlay != null)
        {
            previewOverlay.gameObject.SetActive(false);
        }
    }

    public void OnTabDragUpdate(Vector2 screenPosition, PanelTabType tabType, InventoryPanelUI sourcePanel)
    {
        if (previewOverlay == null || canvas == null || rootPanel == null) return;

        RectTransform windowRect = rootPanel.GetComponent<RectTransform>();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(windowRect, screenPosition, canvas.worldCamera, out Vector2 localPoint))
        {
            previewOverlay.gameObject.SetActive(false);
            return;
        }

        float width = windowRect.rect.width;
        float normalizedX = (localPoint.x + (width * 0.5f)) / width;

        previewOverlay.gameObject.SetActive(true);

        // Adjust anchor boundaries based on dragging zone
        if (normalizedX < 0.25f)
        {
            // Dock Left (takes 50% left)
            previewOverlay.anchorMin = new Vector2(0f, 0f);
            previewOverlay.anchorMax = new Vector2(0.5f, 1f);
        }
        else if (normalizedX > 0.75f)
        {
            // Dock Right (takes 50% right)
            previewOverlay.anchorMin = new Vector2(0.5f, 0f);
            previewOverlay.anchorMax = new Vector2(1f, 1f);
        }
        else
        {
            // Center (maximize 100%)
            previewOverlay.anchorMin = Vector2.zero;
            previewOverlay.anchorMax = Vector2.one;
        }

        previewOverlay.offsetMin = Vector2.zero;
        previewOverlay.offsetMax = Vector2.zero;
    }

    public void OnTabDragEnd(Vector2 screenPosition, PanelTabType tabType, InventoryPanelUI sourcePanel)
    {
        if (previewOverlay != null)
        {
            previewOverlay.gameObject.SetActive(false);
        }

        if (rootPanel == null) return;

        RectTransform windowRect = rootPanel.GetComponent<RectTransform>();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(windowRect, screenPosition, canvas.worldCamera, out Vector2 localPoint))
        {
            return;
        }

        float width = windowRect.rect.width;
        float normalizedX = (localPoint.x + (width * 0.5f)) / width;

        if (normalizedX < 0.25f)
        {
            // Dragged tab splits screen to Left
            SetLayoutMode(InventoryLayoutMode.Split);
            if (leftPanel != null)
            {
                leftPanel.SetTab(tabType);
                // Smart tab swap: prevent displaying the same inventory on both sides if possible
                if (rightPanel != null && rightPanel.CurrentTab == tabType)
                {
                    rightPanel.SetTab(GetAlternativeTab(tabType));
                }
            }
        }
        else if (normalizedX > 0.75f)
        {
            // Dragged tab splits screen to Right
            SetLayoutMode(InventoryLayoutMode.Split);
            if (rightPanel != null)
            {
                rightPanel.SetTab(tabType);
                // Smart tab swap: prevent displaying the same inventory on both sides if possible
                if (leftPanel != null && leftPanel.CurrentTab == tabType)
                {
                    leftPanel.SetTab(GetAlternativeTab(tabType));
                }
            }
        }
        else
        {
            // Maximizes panel containing the dragged tab
            if (sourcePanel == leftPanel)
            {
                SetLayoutMode(InventoryLayoutMode.MaximizedLeft);
                if (leftPanel != null) leftPanel.SetTab(tabType);
            }
            else if (sourcePanel == rightPanel)
            {
                SetLayoutMode(InventoryLayoutMode.MaximizedRight);
                if (rightPanel != null) rightPanel.SetTab(tabType);
            }
        }
    }

    private PanelTabType GetAlternativeTab(PanelTabType tab)
    {
        // Cycles to alternative tab to avoid overlap
        switch (tab)
        {
            case PanelTabType.Factory: return PanelTabType.Foraging;
            case PanelTabType.Foraging: return PanelTabType.Modules;
            case PanelTabType.Modules: return PanelTabType.Machine;
            default: return PanelTabType.Factory;
        }
    }

    #endregion

    #region Module Equipment Actions

    public void TryEquipModuleFromInventory(ModuleSlotUI inventorySlot)
    {
        PlayerModule mod = inventorySlot.AssignedModule;
        if (mod == null || PlayerModuleManager.Instance == null) return;

        // Auto-unequip module of the same type if already equipped
        PlayerModule alreadyEquipped = null;
        foreach (var equipped in PlayerModuleManager.Instance.EquipedModules)
        {
            if (equipped.moduleType == mod.moduleType)
            {
                alreadyEquipped = equipped;
                break;
            }
        }

        if (alreadyEquipped != null)
        {
            bool unequipSuccess = PlayerModuleManager.Instance.UnequipModule(alreadyEquipped);
            if (unequipSuccess)
            {
                PlayerModuleManager.Instance.AddOwnedModule(alreadyEquipped);
            }
            else
            {
                Debug.LogWarning($"Failed to unequip module of type {mod.moduleType} during swap.");
                return;
            }
        }

        // Equip the new module
        bool success = PlayerModuleManager.Instance.EquipModule(mod);
        if (success)
        {
            PlayerModuleManager.Instance.RemoveOwnedModule(mod);
            RefreshWindow();
        }
        else
        {
            Debug.LogWarning($"Failed to equip {mod.title}. Check conflicts.");
        }
    }

    public void TryUnequipModule(ModuleSlotUI equipmentSlot)
    {
        PlayerModule mod = equipmentSlot.AssignedModule;
        if (mod == null || PlayerModuleManager.Instance == null) return;

        bool success = PlayerModuleManager.Instance.UnequipModule(mod);
        if (success)
        {
            PlayerModuleManager.Instance.AddOwnedModule(mod);
            RefreshWindow();
        }
    }

    #endregion

    #region Drag and Drop Handling (Modules)

    public void StartDrag(ModuleSlotUI slot)
    {
        if (dragIconTransform == null || dragIconImage == null) return;

        currentlyDraggingSlot = slot;
        dragIconImage.sprite = slot.AssignedModule.sprite;
        dragIconTransform.gameObject.SetActive(true);
    }

    public void OnDragUpdate(PointerEventData eventData)
    {
        if (dragIconTransform == null || canvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform,
            eventData.position,
            canvas.worldCamera,
            out Vector2 localPoint
        );
        dragIconTransform.anchoredPosition = localPoint;
    }

    public void EndDrag(PointerEventData eventData)
    {
        if (dragIconTransform != null) dragIconTransform.gameObject.SetActive(false);

        if (currentlyDraggingSlot == null) return;

        // Find module slot under cursor
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            ModuleSlotUI targetSlot = result.gameObject.GetComponentInParent<ModuleSlotUI>();
            if (targetSlot != null && targetSlot != currentlyDraggingSlot)
            {
                // Dragged owned module from inventory onto equipped slot
                if (!currentlyDraggingSlot.IsEquippedSlot && targetSlot.IsEquippedSlot)
                {
                    if (currentlyDraggingSlot.AssignedModule.moduleType == targetSlot.SlotTypeConstraint)
                    {
                        TryEquipModuleFromInventory(currentlyDraggingSlot);
                    }
                }
                // Dragged equipped module out to owned list
                else if (currentlyDraggingSlot.IsEquippedSlot && !targetSlot.IsEquippedSlot)
                {
                    TryUnequipModule(currentlyDraggingSlot);
                }
                break;
            }
        }

        currentlyDraggingSlot = null;
    }

    #endregion
}
