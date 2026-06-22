using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ModuleEquipmentWindow : MonoBehaviour
{
    [Header("References")]
    public PlayerModuleController playerController;
    public GameObject rootPanel;

    [Header("Equipment Slots")]
    public ModuleSlotUI cannonSlot;
    public ModuleSlotUI bodySlot;
    public ModuleSlotUI moveSlot;

    [Header("Inventory Settings")]
    public Transform inventoryContent;
    public ModuleSlotUI inventorySlotPrefab;

    [Header("Drag and Drop (Optional)")]
    public Canvas canvas;
    public RectTransform dragIconTransform;
    public Image dragIconImage;

    private List<ModuleSlotUI> generatedInventorySlots = new List<ModuleSlotUI>();
    private ModuleSlotUI currentlyDraggingSlot;

    private void Start()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
        if (dragIconTransform != null) dragIconTransform.gameObject.SetActive(false);

        // Initialize equipment slots with types
        if (cannonSlot != null) cannonSlot.SetupEquipmentSlot(ModuleType.Cannon, this);
        if (bodySlot != null) bodySlot.SetupEquipmentSlot(ModuleType.Body, this);
        if (moveSlot != null) moveSlot.SetupEquipmentSlot(ModuleType.Move, this);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) // Hotkey M for Modules
        {
            if (rootPanel.activeSelf) Close();
            else Open();
        }
    }

    public void Open()
    {
        if (PlayerModuleManager.Instance == null)
        {
            Debug.LogWarning("ModuleEquipmentWindow: Не могу открыть окно, так как на сцене нет объекта с PlayerModuleManager! Убедитесь, что Bootstrapper загрузил его.");
            return;
        }

        rootPanel.SetActive(true);
        RefreshWindow();
    }

    public void Close()
    {
        rootPanel.SetActive(false);
        if (dragIconTransform != null) dragIconTransform.gameObject.SetActive(false);
    }

    public void RefreshWindow()
    {
        RefreshEquipmentSlots();
        RefreshInventorySlots();
    }

    private void RefreshEquipmentSlots()
    {
        // Clear current visual slots
        if (cannonSlot != null) cannonSlot.SetModule(null);
        if (bodySlot != null) bodySlot.SetModule(null);
        if (moveSlot != null) moveSlot.SetModule(null);

        if (PlayerModuleManager.Instance == null) return;

        // Fill based on equipped modules
        foreach (var mod in PlayerModuleManager.Instance.EquipedModules)
        {
            if (mod.moduleType == ModuleType.Cannon && cannonSlot != null) cannonSlot.SetModule(mod);
            else if (mod.moduleType == ModuleType.Body && bodySlot != null) bodySlot.SetModule(mod);
            else if (mod.moduleType == ModuleType.Move && moveSlot != null) moveSlot.SetModule(mod);
        }
    }

    private void RefreshInventorySlots()
    {
        if (PlayerModuleManager.Instance == null) return;

        // Clear all child objects inside the inventory to avoid ghost objects from the editor
        foreach (Transform child in inventoryContent)
        {
            Destroy(child.gameObject);
        }
        generatedInventorySlots.Clear();

        // Create new slots
        foreach (var mod in PlayerModuleManager.Instance.OwnedModules)
        {
            ModuleSlotUI newSlot = Instantiate(inventorySlotPrefab, inventoryContent);
            newSlot.SetupInventorySlot(mod, this);
            generatedInventorySlots.Add(newSlot);
        }
    }

    public void TryEquipModuleFromInventory(ModuleSlotUI inventorySlot)
    {
        PlayerModule mod = inventorySlot.AssignedModule;
        if (mod == null || PlayerModuleManager.Instance == null) return;

        // Find the right equipment slot based on module type
        ModuleSlotUI targetSlot = null;
        if (mod.moduleType == ModuleType.Cannon) targetSlot = cannonSlot;
        else if (mod.moduleType == ModuleType.Body) targetSlot = bodySlot;
        else if (mod.moduleType == ModuleType.Move) targetSlot = moveSlot;

        if (targetSlot == null) return;

        // If there's already a module equipped there, unequip it first
        if (targetSlot.AssignedModule != null)
        {
            TryUnequipModule(targetSlot);
        }

        // Try to equip
        bool success = PlayerModuleManager.Instance.EquipModule(mod);
        if (success)
        {
            // Move from owned to equipped
            PlayerModuleManager.Instance.RemoveOwnedModule(mod);
            RefreshWindow();
        }
        else
        {
            Debug.Log($"Failed to equip {mod.title}. Check conflicts.");
        }
    }

    public void TryUnequipModule(ModuleSlotUI equipmentSlot)
    {
        PlayerModule mod = equipmentSlot.AssignedModule;
        if (mod == null || PlayerModuleManager.Instance == null) return;

        bool success = PlayerModuleManager.Instance.UnequipModule(mod);
        if (success)
        {
            // Move from equipped to owned
            PlayerModuleManager.Instance.AddOwnedModule(mod);
            RefreshWindow();
        }
    }

    #region Drag and Drop Handling

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

        // Check what we dropped on
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            ModuleSlotUI targetSlot = result.gameObject.GetComponentInParent<ModuleSlotUI>();
            if (targetSlot != null && targetSlot != currentlyDraggingSlot)
            {
                // Dropped inventory item onto equipment slot
                if (!currentlyDraggingSlot.IsEquippedSlot && targetSlot.IsEquippedSlot)
                {
                    if (currentlyDraggingSlot.AssignedModule.moduleType == targetSlot.SlotTypeConstraint)
                    {
                        TryEquipModuleFromInventory(currentlyDraggingSlot);
                    }
                }
                // Dropped equipment item onto inventory area
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
