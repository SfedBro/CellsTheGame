using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum PanelTabType
{
    Factory,
    Foraging,
    Modules,
    Machine // The new Mechanism tab!
}

[ExecuteAlways]
public class InventoryPanelUI : MonoBehaviour
{
    [Header("Tab Buttons")]
    public Button factoryTabButton;
    public Button foragingTabButton;
    public Button modulesTabButton;
    public Button machineTabButton; // Button for the Mechanism tab

    [Header("Views")]
    public GameObject factoryView;
    public GameObject foragingView;
    public GameObject modulesView;
    public GameObject machineView; // Container for the Mechanism view

    [Header("Machine Sub-Views")]
    public GameObject machineNoSelectionPlaceholder; // Text placeholder when no machine is selected
    public GameObject machineInventoryContent; // Container shown when a machine IS selected

    [Header("Containers")]
    public Transform factorySlotsContainer;
    public Transform foragingSlotsContainer;
    public Transform modulesSlotsContainer;
    
    [Header("Machine Containers")]
    public Transform machineSingleSlotsContainer; // For standard single-inventory storage
    public Transform machineInputSlotsContainer;  // For crafting machine inputs
    public Transform machineOutputSlotsContainer; // For crafting machine outputs

    [Header("Prefabs")]
    public ItemSlotUI itemSlotPrefab;
    public ModuleSlotUI moduleSlotPrefab;

    [Header("Module Equipment Slots")]
    public ModuleSlotUI cannonSlot;
    public ModuleSlotUI bodySlot;
    public ModuleSlotUI moveSlot;

    private PlayerInventoryWindow parentWindow;
    private PanelTabType currentTab;
    public PanelTabType CurrentTab => currentTab;

    private List<ItemSlotUI> factorySlots = new List<ItemSlotUI>();
    private List<ItemSlotUI> foragingSlots = new List<ItemSlotUI>();
    private List<ModuleSlotUI> modulesSlots = new List<ModuleSlotUI>();

    // Dynamic slots for selected machine
    private List<ItemSlotUI> machineSingleSlots = new List<ItemSlotUI>();
    private List<ItemSlotUI> machineInputSlots = new List<ItemSlotUI>();
    private List<ItemSlotUI> machineOutputSlots = new List<ItemSlotUI>();

    public void Initialize(PlayerInventoryWindow parent, PanelTabType initialTab)
    {
        parentWindow = parent;
        currentTab = initialTab;

        if (factoryTabButton != null)
        {
            factoryTabButton.onClick.AddListener(() => SetTab(PanelTabType.Factory));
            var drag = factoryTabButton.gameObject.GetComponent<TabDragHandler>();
            if (drag == null) drag = factoryTabButton.gameObject.AddComponent<TabDragHandler>();
            drag.Initialize(parent, this, PanelTabType.Factory);
        }
        if (foragingTabButton != null)
        {
            foragingTabButton.onClick.AddListener(() => SetTab(PanelTabType.Foraging));
            var drag = foragingTabButton.gameObject.GetComponent<TabDragHandler>();
            if (drag == null) drag = foragingTabButton.gameObject.AddComponent<TabDragHandler>();
            drag.Initialize(parent, this, PanelTabType.Foraging);
        }
        if (modulesTabButton != null)
        {
            modulesTabButton.onClick.AddListener(() => SetTab(PanelTabType.Modules));
            var drag = modulesTabButton.gameObject.GetComponent<TabDragHandler>();
            if (drag == null) drag = modulesTabButton.gameObject.AddComponent<TabDragHandler>();
            drag.Initialize(parent, this, PanelTabType.Modules);
        }
        if (machineTabButton != null)
        {
            machineTabButton.onClick.AddListener(() => SetTab(PanelTabType.Machine));
            var drag = machineTabButton.gameObject.GetComponent<TabDragHandler>();
            if (drag == null) drag = machineTabButton.gameObject.AddComponent<TabDragHandler>();
            drag.Initialize(parent, this, PanelTabType.Machine);
        }
    }

    public void SetTab(PanelTabType tabType)
    {
        currentTab = tabType;
        RefreshView();
        
        // Notify parent window to update highlights since active tab changed
        if (parentWindow != null)
        {
            parentWindow.UpdateMachineHighlightState();
        }
    }

    public void RefreshView()
    {
        // Toggle view containers
        if (factoryView != null) factoryView.SetActive(currentTab == PanelTabType.Factory);
        if (foragingView != null) foragingView.SetActive(currentTab == PanelTabType.Foraging);
        if (modulesView != null) modulesView.SetActive(currentTab == PanelTabType.Modules);
        if (machineView != null) machineView.SetActive(currentTab == PanelTabType.Machine);

        switch (currentTab)
        {
            case PanelTabType.Factory:
                RefreshFactoryInventory();
                break;
            case PanelTabType.Foraging:
                RefreshForagingInventory();
                break;
            case PanelTabType.Modules:
                RefreshModules();
                break;
            case PanelTabType.Machine:
                RefreshMachineInventory();
                break;
        }
    }

    private void RefreshFactoryInventory()
    {
        if (PlayerInventory.Instance == null || PlayerInventory.Instance.FactoryInventory == null) return;

        var inv = PlayerInventory.Instance.FactoryInventory;

        if (factorySlots.Count != inv.slotCount)
        {
            GenerateSlots(inv, factorySlotsContainer, factorySlots);
        }

        foreach (var slot in factorySlots)
        {
            slot.UpdateUI();
        }
    }

    private void RefreshForagingInventory()
    {
        if (PlayerInventory.Instance == null || PlayerInventory.Instance.ForagingInventory == null) return;

        var inv = PlayerInventory.Instance.ForagingInventory;

        if (foragingSlots.Count != inv.slotCount)
        {
            GenerateSlots(inv, foragingSlotsContainer, foragingSlots);
        }

        foreach (var slot in foragingSlots)
        {
            slot.UpdateUI();
        }
    }

    private void GenerateSlots(Inventory inv, Transform container, List<ItemSlotUI> slotList)
    {
        if (itemSlotPrefab == null || container == null) return;

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
        slotList.Clear();

        for (int i = 0; i < inv.slotCount; i++)
        {
            var newSlot = Instantiate(itemSlotPrefab, container);
            newSlot.Initialize(inv, i);
            slotList.Add(newSlot);
        }
    }

    private void RefreshModules()
    {
        if (PlayerModuleManager.Instance == null) return;

        if (cannonSlot != null) cannonSlot.SetupEquipmentSlot(ModuleType.Cannon, parentWindow);
        if (bodySlot != null) bodySlot.SetupEquipmentSlot(ModuleType.Body, parentWindow);
        if (moveSlot != null) moveSlot.SetupEquipmentSlot(ModuleType.Move, parentWindow);

        if (cannonSlot != null) cannonSlot.SetModule(null);
        if (bodySlot != null) bodySlot.SetModule(null);
        if (moveSlot != null) moveSlot.SetModule(null);

        foreach (var mod in PlayerModuleManager.Instance.EquipedModules)
        {
            if (mod.moduleType == ModuleType.Cannon && cannonSlot != null) cannonSlot.SetModule(mod);
            else if (mod.moduleType == ModuleType.Body && bodySlot != null) bodySlot.SetModule(mod);
            else if (mod.moduleType == ModuleType.Move && moveSlot != null) moveSlot.SetModule(mod);
        }

        if (modulesSlotsContainer != null && moduleSlotPrefab != null)
        {
            foreach (Transform child in modulesSlotsContainer)
            {
                Destroy(child.gameObject);
            }
            modulesSlots.Clear();

            foreach (var mod in PlayerModuleManager.Instance.OwnedModules)
            {
                var newSlot = Instantiate(moduleSlotPrefab, modulesSlotsContainer);
                newSlot.SetupInventorySlot(mod, parentWindow);
                modulesSlots.Add(newSlot);
            }
        }
    }

    private void RefreshMachineInventory()
    {
        if (parentWindow == null) return;

        // Check if there is a selected machine
        var selectedProvider = parentWindow.SelectedInventoryProvider;
        var selectedCrafting = parentWindow.SelectedCraftingProvider;

        if (selectedProvider == null && selectedCrafting == null)
        {
            if (machineNoSelectionPlaceholder != null) machineNoSelectionPlaceholder.SetActive(true);
            if (machineInventoryContent != null) machineInventoryContent.SetActive(false);
            return;
        }

        if (machineNoSelectionPlaceholder != null) machineNoSelectionPlaceholder.SetActive(false);
        if (machineInventoryContent != null) machineInventoryContent.SetActive(true);

        // Scenario 1: Standard Single-Inventory Block (e.g., storage chest)
        if (selectedProvider != null)
        {
            if (machineSingleSlotsContainer != null) machineSingleSlotsContainer.gameObject.SetActive(true);
            if (machineInputSlotsContainer != null) machineInputSlotsContainer.gameObject.SetActive(false);
            if (machineOutputSlotsContainer != null) machineOutputSlotsContainer.gameObject.SetActive(false);

            var inv = selectedProvider.Inventory;
            if (inv != null)
            {
                if (machineSingleSlots.Count != inv.slotCount || (machineSingleSlots.Count > 0 && machineSingleSlots[0].inventory != inv))
                {
                    GenerateSlots(inv, machineSingleSlotsContainer, machineSingleSlots);
                }
                foreach (var slot in machineSingleSlots)
                {
                    slot.UpdateUI();
                }
            }
        }
        // Scenario 2: Crafting Machine Block (Input + Output inventories)
        else if (selectedCrafting != null)
        {
            if (machineSingleSlotsContainer != null) machineSingleSlotsContainer.gameObject.SetActive(false);
            if (machineInputSlotsContainer != null) machineInputSlotsContainer.gameObject.SetActive(true);
            if (machineOutputSlotsContainer != null) machineOutputSlotsContainer.gameObject.SetActive(true);

            var inputInv = selectedCrafting.InputInventory;
            if (inputInv != null)
            {
                if (machineInputSlots.Count != inputInv.slotCount || (machineInputSlots.Count > 0 && machineInputSlots[0].inventory != inputInv))
                {
                    GenerateSlots(inputInv, machineInputSlotsContainer, machineInputSlots);
                }
                foreach (var slot in machineInputSlots)
                {
                    slot.UpdateUI();
                }
            }

            var outputInv = selectedCrafting.OutputInventory;
            if (outputInv != null)
            {
                if (machineOutputSlots.Count != outputInv.slotCount || (machineOutputSlots.Count > 0 && machineOutputSlots[0].inventory != outputInv))
                {
                    GenerateSlots(outputInv, machineOutputSlotsContainer, machineOutputSlots);
                }
                foreach (var slot in machineOutputSlots)
                {
                    slot.UpdateUI();
                }
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += EnsureEditorPreview;
        }
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            EnsureEditorPreview();
        }
    }

    private void EnsureEditorPreview()
    {
        if (this == null) return;
        if (UnityEditor.EditorUtility.IsPersistent(this)) return;
        
        EnsurePreviewForContainer(factorySlotsContainer, 24);
        EnsurePreviewForContainer(foragingSlotsContainer, 12);
    }

    private void EnsurePreviewForContainer(Transform container, int count)
    {
        if (container == null || itemSlotPrefab == null) return;

        if (container.childCount > 0) return;

        for (int i = 0; i < count; i++)
        {
            GameObject newSlotObj = null;
            try
            {
                newSlotObj = UnityEditor.PrefabUtility.InstantiatePrefab(itemSlotPrefab.gameObject, container) as GameObject;
            }
            catch (System.Exception)
            {
                newSlotObj = Instantiate(itemSlotPrefab.gameObject, container);
            }

            if (newSlotObj != null)
            {
                newSlotObj.hideFlags = HideFlags.DontSave;
                
                var text = newSlotObj.GetComponentInChildren<TMPro.TMP_Text>();
                if (text != null)
                {
                    text.text = (i + 1).ToString();
                }
            }
        }
    }
#endif
}
