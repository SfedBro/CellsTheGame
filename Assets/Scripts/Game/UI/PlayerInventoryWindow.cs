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

        // Automatically build and configure the entire UI at runtime ONLY if panels are not already assigned in inspector!
        if (leftPanel == null || rightPanel == null)
        {
            BuildRuntimeUI();
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

    #region Procedural Runtime UI Builder

    private void BuildRuntimeUI()
    {
        // 1. Destroy any existing editor UI child elements inside the prefab instance
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in transform)
        {
            childrenToDestroy.Add(child.gameObject);
        }
        foreach (var go in childrenToDestroy)
        {
            DestroyImmediate(go);
        }

        // 2. Locate active canvas
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = Object.FindFirstObjectByType<Canvas>();
            }
        }

        // Setup root rect transform to stretch full screen
        RectTransform myRect = GetComponent<RectTransform>();
        if (myRect != null)
        {
            myRect.anchorMin = Vector2.zero;
            myRect.anchorMax = Vector2.one;
            myRect.offsetMin = Vector2.zero;
            myRect.offsetMax = Vector2.zero;
        }

        // 3. Create RootPanel
        GameObject rootPanelGo = new GameObject("RootPanel", typeof(RectTransform));
        rootPanelGo.transform.SetParent(transform, false);
        rootPanel = rootPanelGo;
        
        RectTransform rootPanelRect = rootPanelGo.GetComponent<RectTransform>();
        rootPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootPanelRect.sizeDelta = new Vector2(950, 600); // Widescreen size

        Image rootBg = rootPanelGo.AddComponent<Image>();
        rootBg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f); // Beautiful dark theme base

        HorizontalLayoutGroup rootLayout = rootPanelGo.AddComponent<HorizontalLayoutGroup>();
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = true;
        rootLayout.spacing = 15;
        rootLayout.padding = new RectOffset(15, 15, 15, 15);

        // 4. Load prefabs from Resources
        if (defaultPanelPrefab == null)
        {
            defaultPanelPrefab = Resources.Load<GameObject>("UI/Inventory Panel");
        }
        ItemSlotUI itemSlotPrefab = Resources.Load<GameObject>("UI/Inventory Slot")?.GetComponent<ItemSlotUI>();
        ModuleSlotUI moduleSlotPrefab = Resources.Load<GameObject>("UI/Module Slot")?.GetComponent<ModuleSlotUI>();

        // 5. Create Panels
        GameObject leftPanelGo;
        if (defaultPanelPrefab != null)
        {
            leftPanelGo = Instantiate(defaultPanelPrefab, rootPanelGo.transform);
            leftPanelGo.name = "LeftPanel";
        }
        else
        {
            leftPanelGo = CreatePanel("LeftPanel", rootPanelGo.transform, itemSlotPrefab, moduleSlotPrefab);
        }
        leftPanel = leftPanelGo.GetComponent<InventoryPanelUI>();

        GameObject rightPanelGo;
        if (defaultPanelPrefab != null)
        {
            rightPanelGo = Instantiate(defaultPanelPrefab, rootPanelGo.transform);
            rightPanelGo.name = "RightPanel";
        }
        else
        {
            rightPanelGo = CreatePanel("RightPanel", rootPanelGo.transform, itemSlotPrefab, moduleSlotPrefab);
        }
        rightPanel = rightPanelGo.GetComponent<InventoryPanelUI>();
        instantiatedRightPanelPrefabSource = defaultPanelPrefab;

        // 6. Create DockPreviewOverlay
        GameObject previewOverlayGo = new GameObject("DockPreviewOverlay", typeof(RectTransform));
        previewOverlayGo.transform.SetParent(rootPanelGo.transform, false);
        previewOverlayGo.transform.SetAsLastSibling();
        
        RectTransform previewOverlayRect = previewOverlayGo.GetComponent<RectTransform>();
        previewOverlayRect.anchorMin = Vector2.zero;
        previewOverlayRect.anchorMax = Vector2.one;
        previewOverlayRect.offsetMin = Vector2.zero;
        previewOverlayRect.offsetMax = Vector2.zero;

        Image previewImg = previewOverlayGo.AddComponent<Image>();
        previewImg.color = new Color(0f, 0.5f, 1f, 0.25f);
        previewImg.raycastTarget = false;
        previewOverlay = previewOverlayRect;
        previewOverlayGo.SetActive(false);

        // 7. Create DragIcon for module drag visuals
        GameObject dragIconGo = new GameObject("DragIcon", typeof(RectTransform));
        dragIconGo.transform.SetParent(transform, false);
        
        RectTransform dragIconRect = dragIconGo.GetComponent<RectTransform>();
        dragIconRect.sizeDelta = new Vector2(50, 50);

        dragIconImage = dragIconGo.AddComponent<Image>();
        dragIconImage.raycastTarget = false;
        dragIconTransform = dragIconRect;
        dragIconGo.SetActive(false);
    }

    private GameObject CreatePanel(string panelName, Transform parent, ItemSlotUI itemSlotPref, ModuleSlotUI moduleSlotPref)
    {
        GameObject panelGo = new GameObject(panelName, typeof(RectTransform));
        panelGo.transform.SetParent(parent, false);

        Image img = panelGo.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.17f, 1f); // Dark panel bg

        LayoutElement layoutElement = panelGo.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;

        InventoryPanelUI panelUI = panelGo.AddComponent<InventoryPanelUI>();
        panelUI.itemSlotPrefab = itemSlotPref;
        panelUI.moduleSlotPrefab = moduleSlotPref;

        VerticalLayoutGroup layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 10;
        layout.padding = new RectOffset(10, 10, 10, 10);

        // Tab Header
        GameObject tabHeaderGo = new GameObject("TabHeader", typeof(RectTransform));
        tabHeaderGo.transform.SetParent(panelGo.transform, false);
        LayoutElement headerLayout = tabHeaderGo.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 35;

        HorizontalLayoutGroup headerGroup = tabHeaderGo.AddComponent<HorizontalLayoutGroup>();
        headerGroup.childControlWidth = true;
        headerGroup.childControlHeight = true;
        headerGroup.childForceExpandWidth = true;
        headerGroup.childForceExpandHeight = true;
        headerGroup.spacing = 5;

        // Buttons
        Button factoryBtn = CreateTabButton("FactoryTabButton", tabHeaderGo.transform, "Завод");
        Button foragingBtn = CreateTabButton("ForagingTabButton", tabHeaderGo.transform, "Вылазка");
        Button modulesBtn = CreateTabButton("ModulesTabButton", tabHeaderGo.transform, "Модули");
        Button machineBtn = CreateTabButton("MachineTabButton", tabHeaderGo.transform, "Механизм");

        panelUI.factoryTabButton = factoryBtn;
        panelUI.foragingTabButton = foragingBtn;
        panelUI.modulesTabButton = modulesBtn;
        panelUI.machineTabButton = machineBtn;

        // Views Container
        GameObject viewsContainerGo = new GameObject("ContentViews", typeof(RectTransform));
        viewsContainerGo.transform.SetParent(panelGo.transform, false);
        LayoutElement viewsLayout = viewsContainerGo.AddComponent<LayoutElement>();
        viewsLayout.flexibleHeight = 1f;

        // Factory View
        GameObject factoryViewGo = CreateGridView("FactoryView", viewsContainerGo.transform);
        panelUI.factoryView = factoryViewGo;
        panelUI.factorySlotsContainer = factoryViewGo.transform;

        // Foraging View
        GameObject foragingViewGo = CreateGridView("ForagingView", viewsContainerGo.transform);
        panelUI.foragingView = foragingViewGo;
        panelUI.foragingSlotsContainer = foragingViewGo.transform;

        // Modules View
        GameObject modulesViewGo = new GameObject("ModulesView", typeof(RectTransform));
        modulesViewGo.transform.SetParent(viewsContainerGo.transform, false);
        StretchRect(modulesViewGo.GetComponent<RectTransform>());
        panelUI.modulesView = modulesViewGo;

        VerticalLayoutGroup modulesLayout = modulesViewGo.AddComponent<VerticalLayoutGroup>();
        modulesLayout.childControlWidth = true;
        modulesLayout.childControlHeight = true;
        modulesLayout.childForceExpandWidth = true;
        modulesLayout.childForceExpandHeight = false;
        modulesLayout.spacing = 10;

        // Equipment Container
        GameObject equipContainerGo = new GameObject("EquipmentSlots", typeof(RectTransform));
        equipContainerGo.transform.SetParent(modulesViewGo.transform, false);
        LayoutElement equipLayout = equipContainerGo.AddComponent<LayoutElement>();
        equipLayout.preferredHeight = 70;

        HorizontalLayoutGroup equipGroup = equipContainerGo.AddComponent<HorizontalLayoutGroup>();
        equipGroup.childControlWidth = true;
        equipGroup.childControlHeight = true;
        equipGroup.childForceExpandWidth = true;
        equipGroup.childForceExpandHeight = true;
        equipGroup.spacing = 10;

        panelUI.cannonSlot = CreateModuleSlot("CannonSlot", equipContainerGo.transform, ModuleType.Cannon, moduleSlotPref);
        panelUI.bodySlot = CreateModuleSlot("BodySlot", equipContainerGo.transform, ModuleType.Body, moduleSlotPref);
        panelUI.moveSlot = CreateModuleSlot("MoveSlot", equipContainerGo.transform, ModuleType.Move, moduleSlotPref);

        // Modules Grid
        GameObject modulesGridGo = CreateGridView("ModulesGrid", modulesViewGo.transform);
        LayoutElement modulesGridLayout = modulesGridGo.AddComponent<LayoutElement>();
        modulesGridLayout.flexibleHeight = 1f;
        panelUI.modulesSlotsContainer = modulesGridGo.transform;

        // Machine View
        GameObject machineViewGo = new GameObject("MachineView", typeof(RectTransform));
        machineViewGo.transform.SetParent(viewsContainerGo.transform, false);
        StretchRect(machineViewGo.GetComponent<RectTransform>());
        panelUI.machineView = machineViewGo;

        // Machine Placeholder
        GameObject placeholderGo = new GameObject("NoSelectionPlaceholder", typeof(RectTransform));
        placeholderGo.transform.SetParent(machineViewGo.transform, false);
        StretchRect(placeholderGo.GetComponent<RectTransform>());
        
        var placeholderText = placeholderGo.AddComponent<TextMeshProUGUI>();
        placeholderText.text = "Выберите механизм в мире, чтобы открыть его инвентарь";
        placeholderText.alignment = TextAlignmentOptions.Center;
        placeholderText.fontSize = 16;
        placeholderText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        panelUI.machineNoSelectionPlaceholder = placeholderGo;

        // Machine Active Content
        GameObject machineContentGo = new GameObject("InventoryContent", typeof(RectTransform));
        machineContentGo.transform.SetParent(machineViewGo.transform, false);
        StretchRect(machineContentGo.GetComponent<RectTransform>());
        panelUI.machineInventoryContent = machineContentGo;

        VerticalLayoutGroup machineContentLayout = machineContentGo.AddComponent<VerticalLayoutGroup>();
        machineContentLayout.childControlWidth = true;
        machineContentLayout.childControlHeight = true;
        machineContentLayout.childForceExpandWidth = true;
        machineContentLayout.childForceExpandHeight = false;
        machineContentLayout.spacing = 10;

        // Single Slots Grid
        GameObject singleGridGo = CreateGridView("SingleSlotsContainer", machineContentGo.transform);
        LayoutElement singleLayout = singleGridGo.AddComponent<LayoutElement>();
        singleLayout.flexibleHeight = 1f;
        panelUI.machineSingleSlotsContainer = singleGridGo.transform;

        // Recipe Selection Block
        GameObject recipeBlockGo = new GameObject("RecipeBlock", typeof(RectTransform));
        recipeBlockGo.transform.SetParent(machineContentGo.transform, false);
        VerticalLayoutGroup recipeBlockLayout = recipeBlockGo.AddComponent<VerticalLayoutGroup>();
        recipeBlockLayout.childControlWidth = true;
        recipeBlockLayout.childControlHeight = true;
        recipeBlockLayout.childForceExpandWidth = true;
        recipeBlockLayout.childForceExpandHeight = false;

        GameObject recipeLabelGo = new GameObject("RecipeLabel", typeof(RectTransform));
        recipeLabelGo.transform.SetParent(recipeBlockGo.transform, false);
        var recipeLabelText = recipeLabelGo.AddComponent<TextMeshProUGUI>();
        recipeLabelText.text = "Выберите рецепт:";
        recipeLabelText.fontSize = 14;
        recipeLabelText.color = Color.white;

        GameObject recipeButtonsGo = new GameObject("RecipeButtonsContainer", typeof(RectTransform));
        recipeButtonsGo.transform.SetParent(recipeBlockGo.transform, false);
        HorizontalLayoutGroup recipeButtonsLayout = recipeButtonsGo.AddComponent<HorizontalLayoutGroup>();
        recipeButtonsLayout.childControlWidth = true;
        recipeButtonsLayout.childControlHeight = true;
        recipeButtonsLayout.childForceExpandWidth = false;
        recipeButtonsLayout.childForceExpandHeight = true;
        recipeButtonsLayout.spacing = 8;
        
        LayoutElement recipeButtonsLayoutElement = recipeButtonsGo.AddComponent<LayoutElement>();
        recipeButtonsLayoutElement.preferredHeight = 40;

        panelUI.machineRecipeButtonsContainer = recipeButtonsGo.transform;

        // Input Grid
        GameObject inputBlockGo = new GameObject("InputBlock", typeof(RectTransform));
        inputBlockGo.transform.SetParent(machineContentGo.transform, false);
        VerticalLayoutGroup inputBlockLayout = inputBlockGo.AddComponent<VerticalLayoutGroup>();
        inputBlockLayout.childControlWidth = true;
        inputBlockLayout.childControlHeight = true;
        inputBlockLayout.childForceExpandWidth = true;
        inputBlockLayout.childForceExpandHeight = false;

        GameObject inputLabelGo = new GameObject("InputLabel", typeof(RectTransform));
        inputLabelGo.transform.SetParent(inputBlockGo.transform, false);
        var inputLabelText = inputLabelGo.AddComponent<TextMeshProUGUI>();
        inputLabelText.text = "Входные ресурсы:";
        inputLabelText.fontSize = 14;
        inputLabelText.color = Color.white;

        GameObject inputGridGo = CreateGridView("InputSlotsContainer", inputBlockGo.transform);
        panelUI.machineInputSlotsContainer = inputGridGo.transform;

        // Output Grid
        GameObject outputBlockGo = new GameObject("OutputBlock", typeof(RectTransform));
        outputBlockGo.transform.SetParent(machineContentGo.transform, false);
        VerticalLayoutGroup outputBlockLayout = outputBlockGo.AddComponent<VerticalLayoutGroup>();
        outputBlockLayout.childControlWidth = true;
        outputBlockLayout.childControlHeight = true;
        outputBlockLayout.childForceExpandWidth = true;
        outputBlockLayout.childForceExpandHeight = false;

        GameObject outputLabelGo = new GameObject("OutputLabel", typeof(RectTransform));
        outputLabelGo.transform.SetParent(outputBlockGo.transform, false);
        var outputLabelText = outputLabelGo.AddComponent<TextMeshProUGUI>();
        outputLabelText.text = "Выходные ресурсы:";
        outputLabelText.fontSize = 14;
        outputLabelText.color = Color.white;

        GameObject outputGridGo = CreateGridView("OutputSlotsContainer", outputBlockGo.transform);
        panelUI.machineOutputSlotsContainer = outputGridGo.transform;

        // Hide all views by default
        factoryViewGo.SetActive(false);
        foragingViewGo.SetActive(false);
        modulesViewGo.SetActive(false);
        machineViewGo.SetActive(false);

        return panelGo;
    }

    private Button CreateTabButton(string buttonName, Transform parent, string label)
    {
        GameObject btnGo = new GameObject(buttonName, typeof(RectTransform));
        btnGo.transform.SetParent(parent, false);

        Image img = btnGo.AddComponent<Image>();
        img.color = new Color(0.24f, 0.24f, 0.28f, 1f);

        Button btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;

        GameObject txtGo = new GameObject("Label", typeof(RectTransform));
        txtGo.transform.SetParent(btnGo.transform, false);
        StretchRect(txtGo.GetComponent<RectTransform>());

        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 13;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btn;
    }

    private GameObject CreateGridView(string gridName, Transform parent)
    {
        GameObject gridGo = new GameObject(gridName, typeof(RectTransform));
        gridGo.transform.SetParent(parent, false);
        StretchRect(gridGo.GetComponent<RectTransform>());

        GridLayoutGroup grid = gridGo.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(48, 48);
        grid.spacing = new Vector2(6, 6);
        grid.padding = new RectOffset(5, 5, 5, 5);
        grid.childAlignment = TextAnchor.UpperCenter;

        return gridGo;
    }

    private ModuleSlotUI CreateModuleSlot(string slotName, Transform parent, ModuleType type, ModuleSlotUI prefab)
    {
        if (prefab != null)
        {
            ModuleSlotUI slot = Instantiate(prefab, parent);
            slot.name = slotName;
            slot.SetupEquipmentSlot(type, this);
            return slot;
        }

        try
        {
            GameObject slotGo = new GameObject(slotName, typeof(RectTransform));
            slotGo.transform.SetParent(parent, false);

            Image bgImg = slotGo.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            ModuleSlotUI slot = slotGo.AddComponent<ModuleSlotUI>();

            // Create a child object for the icon to avoid multiple Image components on the same GameObject
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(slotGo.transform, false);
            StretchRect(iconGo.GetComponent<RectTransform>());
            slot.iconImage = iconGo.AddComponent<Image>();
            slot.iconImage.enabled = false;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(slotGo.transform, false);
            StretchRect(labelGo.GetComponent<RectTransform>());
            
            slot.titleText = labelGo.AddComponent<TextMeshProUGUI>();
            slot.titleText.text = $"[ {type} ]";
            slot.titleText.fontSize = 11;
            slot.titleText.alignment = TextAlignmentOptions.Center;
            slot.titleText.color = Color.gray;

            return slot;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception in CreateModuleSlot: {ex}");
            throw;
        }
    }

    private void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
                ItemSlotUI itemSlotPrefab = Resources.Load<GameObject>("UI/Inventory Slot")?.GetComponent<ItemSlotUI>();
                ModuleSlotUI moduleSlotPrefab = Resources.Load<GameObject>("UI/Module Slot")?.GetComponent<ModuleSlotUI>();
                rightPanelGo = CreatePanel("RightPanel", rootPanel.transform, itemSlotPrefab, moduleSlotPrefab);
            }

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
