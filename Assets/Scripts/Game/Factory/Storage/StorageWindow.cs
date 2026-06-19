using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StorageWindow : MonoBehaviour
{
    public static StorageWindow Instance;

    [Header("UI References")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText; // optional
    
    [Header("Slot Containers (Assign in Inspector)")]
    [SerializeField] private ItemSlotUI slotPrefab;
    [SerializeField] private Transform storageSlotsContainer;
    [SerializeField] private Transform playerSlotsContainer;

    private MachineStorage currentStorage;
    private List<ItemSlotUI> storageSlots = new List<ItemSlotUI>();
    private List<ItemSlotUI> playerSlots = new List<ItemSlotUI>();

    private void Awake()
    {
        Instance = this;
        root.SetActive(false);
    }

    public void Open(MachineStorage storage)
    {
        currentStorage = storage;
        if (titleText != null) titleText.text = "Склад";
        
        GenerateSlots();
        Refresh();
        root.SetActive(true);
    }

    public void Close()
    {
        root.SetActive(false);
        currentStorage = null;
    }

    private void GenerateSlots()
    {
        if (slotPrefab == null) return;

        // Generate Storage Slots
        if (storageSlotsContainer != null && currentStorage != null)
        {
            foreach (Transform child in storageSlotsContainer) Destroy(child.gameObject);
            storageSlots.Clear();
            for (int i = 0; i < currentStorage.Inventory.slotCount; i++)
            {
                var slot = Instantiate(slotPrefab, storageSlotsContainer);
                slot.Initialize(currentStorage.Inventory, i);
                storageSlots.Add(slot);
            }
        }

        // Generate Player Slots
        if (playerSlotsContainer != null && PlayerInventory.Instance != null)
        {
            foreach (Transform child in playerSlotsContainer) Destroy(child.gameObject);
            playerSlots.Clear();
            for (int i = 0; i < PlayerInventory.Instance.Inventory.slotCount; i++)
            {
                var slot = Instantiate(slotPrefab, playerSlotsContainer);
                slot.Initialize(PlayerInventory.Instance.Inventory, i);
                playerSlots.Add(slot);
            }
        }
    }

    public void Refresh()
    {
        if (currentStorage == null) return;

        foreach (var slot in storageSlots) slot.UpdateUI();
        foreach (var slot in playerSlots) slot.UpdateUI();
    }

    private void Update()
    {
        if (root.activeSelf && currentStorage == null)
        {
            Close();
        }
    }
}
