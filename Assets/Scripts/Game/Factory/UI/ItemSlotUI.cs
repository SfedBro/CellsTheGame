using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ItemSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Inventory inventory;
    public int slotIndex;
    
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Image highlight;

    public void Initialize(Inventory inv, int index)
    {
        inventory = inv;
        slotIndex = index;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (inventory == null || inventory.slots == null || slotIndex >= inventory.slots.Length) return;
        
        var slot = inventory.slots[slotIndex];
        if (slot.IsEmpty)
        {
            if (icon != null) icon.enabled = false;
            if (amountText != null) amountText.text = "";
        }
        else
        {
            if (icon != null)
            {
                icon.enabled = true;
                icon.sprite = ResourcesManager.instance.getResourceSprite(slot.type);
            }
            if (amountText != null)
            {
                amountText.text = slot.amount > 1 ? slot.amount.ToString() : "";
            }
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        var draggedItem = eventData.pointerDrag?.GetComponent<DraggableItemUI>();
        if (draggedItem != null && draggedItem.sourceSlot != null)
        {
            Inventory sourceInventory = draggedItem.sourceSlot.inventory;
            int sourceIndex = draggedItem.sourceSlot.slotIndex;
            
            InventoryUtils.SwapOrMergeSlots(sourceInventory, sourceIndex, inventory, slotIndex);
            
            draggedItem.sourceSlot.UpdateUI();
            this.UpdateUI();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlight != null) highlight.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlight != null) highlight.enabled = false;
    }
}
