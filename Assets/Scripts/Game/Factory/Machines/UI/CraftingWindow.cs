using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingWindow : MonoBehaviour
{
    public static CraftingWindow Instance;

    [Header("UI References")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text recipeNameText;
    
    [Header("Progress")]
    [SerializeField] private Image progressFill; // Set Image Type to Filled in Inspector

    [Header("Slot Containers (Assign in Inspector)")]
    [SerializeField] private ItemSlotUI slotPrefab;
    [SerializeField] private Transform inputSlotsContainer;
    [SerializeField] private Transform outputSlotsContainer;
    [SerializeField] private Transform playerSlotsContainer;

    private ICraftingProvider currentProvider;
    private List<ItemSlotUI> inputSlots = new List<ItemSlotUI>();
    private List<ItemSlotUI> outputSlots = new List<ItemSlotUI>();
    private List<ItemSlotUI> playerSlots = new List<ItemSlotUI>();

    private void Awake()
    {
        Instance = this;
        root.SetActive(false);
    }

    public bool IsOpen => root.activeSelf;

    public void Open(ICraftingProvider provider)
    {
        if (PauseMenu.Instance != null && PauseMenu.Instance.IsPaused) return;
        if (StorageWindow.Instance != null && StorageWindow.Instance.IsOpen) return;
        if (root.activeSelf) Close();
        
        currentProvider = provider;
        if (titleText != null) titleText.text = "Сборщик";
        
        GenerateSlots();
        Refresh();
        
        if (currentProvider != null)
        {
            if (currentProvider.InputInventory != null)
                currentProvider.InputInventory.OnInventoryChanged += Refresh;
            if (currentProvider.OutputInventory != null)
                currentProvider.OutputInventory.OnInventoryChanged += Refresh;
        }
        
        if (PlayerInventory.Instance != null && PlayerInventory.Instance.Inventory != null)
        {
            PlayerInventory.Instance.Inventory.OnInventoryChanged += Refresh;
        }

        root.SetActive(true);
    }

    public void Close()
    {
        if (currentProvider != null)
        {
            if (currentProvider.InputInventory != null)
                currentProvider.InputInventory.OnInventoryChanged -= Refresh;
            if (currentProvider.OutputInventory != null)
                currentProvider.OutputInventory.OnInventoryChanged -= Refresh;
        }
        
        if (PlayerInventory.Instance != null && PlayerInventory.Instance.Inventory != null)
        {
            PlayerInventory.Instance.Inventory.OnInventoryChanged -= Refresh;
        }
        
        root.SetActive(false);
        currentProvider = null;
    }

    private void GenerateSlots()
    {
        if (slotPrefab == null) return;

        // Generate Input Slots
        if (inputSlotsContainer != null && currentProvider != null && currentProvider.InputInventory != null)
        {
            foreach (Transform child in inputSlotsContainer) Destroy(child.gameObject);
            inputSlots.Clear();
            for (int i = 0; i < currentProvider.InputInventory.slotCount; i++)
            {
                var slot = Instantiate(slotPrefab, inputSlotsContainer);
                slot.Initialize(currentProvider.InputInventory, i);
                inputSlots.Add(slot);
            }
        }

        // Generate Output Slots
        if (outputSlotsContainer != null && currentProvider != null && currentProvider.OutputInventory != null)
        {
            foreach (Transform child in outputSlotsContainer) Destroy(child.gameObject);
            outputSlots.Clear();
            for (int i = 0; i < currentProvider.OutputInventory.slotCount; i++)
            {
                var slot = Instantiate(slotPrefab, outputSlotsContainer);
                slot.Initialize(currentProvider.OutputInventory, i);
                outputSlots.Add(slot);
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
        if (currentProvider == null) return;

        foreach (var slot in inputSlots) slot.UpdateUI();
        foreach (var slot in outputSlots) slot.UpdateUI();
        foreach (var slot in playerSlots) slot.UpdateUI();
    }

    private void Update()
    {
        if (!root.activeSelf) return;

        if (currentProvider == null)
        {
            Close();
            return;
        }

        // Update Progress Bar visually every frame smoothly
        if (progressFill != null)
        {
            progressFill.fillAmount = currentProvider.ProgressPercentage;
        }

        if (recipeNameText != null)
        {
            recipeNameText.text = currentProvider.RecipeName;
        }
    }
}
