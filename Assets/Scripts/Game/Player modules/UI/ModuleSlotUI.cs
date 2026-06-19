using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ModuleSlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image iconImage;
    public TMP_Text titleText;
    public TMP_Text chargeText;
    public Image backgroundImage;
    
    [Header("Colors")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color equippedColor = new Color(0.2f, 0.4f, 0.2f, 1f);

    public PlayerModule AssignedModule { get; private set; }
    public bool IsEquippedSlot { get; private set; }
    public ModuleType SlotTypeConstraint { get; private set; } // Only used if IsEquippedSlot is true

    private ModuleEquipmentWindow parentWindow;

    public void SetupInventorySlot(PlayerModule module, ModuleEquipmentWindow window)
    {
        parentWindow = window;
        IsEquippedSlot = false;
        SetModule(module);
    }

    public void SetupEquipmentSlot(ModuleType slotType, ModuleEquipmentWindow window)
    {
        parentWindow = window;
        IsEquippedSlot = true;
        SlotTypeConstraint = slotType;
        SetModule(null); // Initially empty
    }

    public void SetModule(PlayerModule module)
    {
        AssignedModule = module;
        
        if (module != null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = module.sprite;
                iconImage.enabled = true;
            }
            if (titleText != null) titleText.text = module.title;
            if (chargeText != null) 
            {
                chargeText.text = module.isChargable ? $"{module.curCharge}" : "∞";
                chargeText.gameObject.SetActive(true);
            }
            if (backgroundImage != null) backgroundImage.color = IsEquippedSlot ? normalColor : normalColor; // Or equipped color if in inventory but equipped
        }
        else
        {
            if (iconImage != null) iconImage.enabled = false;
            if (titleText != null) titleText.text = IsEquippedSlot ? $"[ {SlotTypeConstraint} ]" : "Empty";
            if (chargeText != null) chargeText.gameObject.SetActive(false);
            if (backgroundImage != null) backgroundImage.color = normalColor;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Right click to equip/unequip fast
            if (AssignedModule != null)
            {
                if (IsEquippedSlot)
                {
                    parentWindow.TryUnequipModule(this);
                }
                else
                {
                    parentWindow.TryEquipModuleFromInventory(this);
                }
            }
        }
    }

    // --- Drag and Drop Logic (Optional, can be implemented later) ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (AssignedModule == null) return;
        parentWindow.StartDrag(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (AssignedModule == null) return;
        parentWindow.OnDragUpdate(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (AssignedModule == null) return;
        parentWindow.EndDrag(eventData);
    }
}
