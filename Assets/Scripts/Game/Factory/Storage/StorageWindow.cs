using TMPro;
using UnityEngine;

public class StorageWindow : MonoBehaviour
{
    public static StorageWindow Instance;

    [Header("UI References")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text storageText;
    [SerializeField] private TMP_Text playerText;

    [Header("Transfer Configuration")]
    public ItemType selectedItemType = ItemType.OreIron;
    public int transferAmount = 1;

    private MachineStorage currentStorage;

    private void Awake()
    {
        Instance = this;
        root.SetActive(false);
    }

    public void Open(MachineStorage storage)
    {
        currentStorage = storage;
        Refresh();
        root.SetActive(true);
    }

    public void Close()
    {
        root.SetActive(false);
        currentStorage = null;
    }

    public void Refresh()
    {
        if (currentStorage == null)
            return;

        if (storageText != null)
        {
            storageText.text = "�����:\n";
            foreach (var item in currentStorage.Inventory.items)
            {
                if (item.Value > 0)
                {
                    storageText.text += $"{item.Key}: {item.Value}";
                    if (currentStorage.Inventory.MaxCapacity > 0)
                        storageText.text += $" / {currentStorage.Inventory.MaxCapacity}";
                    storageText.text += "\n";
                }
            }
        }

        if (playerText != null && PlayerInventory.Instance != null)
        {
            playerText.text = "�����:\n";
            foreach (var item in PlayerInventory.Instance.Inventory.items)
            {
                if (item.Value > 0)
                    playerText.text += $"{item.Key}: {item.Value}\n";
            }
        }
    }

    public void MoveToStorage()
    {
        if (currentStorage == null || PlayerInventory.Instance == null) return;

        bool success = PlayerInventory.Instance.Inventory.TransferTo(
            currentStorage.Inventory,
            selectedItemType,
            transferAmount);

        if (success) Refresh();
    }

    public void MoveFromStorage()
    {
        if (currentStorage == null || PlayerInventory.Instance == null) return;

        bool success = currentStorage.Inventory.TransferTo(
            PlayerInventory.Instance.Inventory,
            selectedItemType,
            transferAmount);

        if (success) Refresh();
    }

    // ��������������� ������ ��� �������� � ������� UnityEvent (Dropdown, Slider � �.�.)
    public void SetSelectedItemType(int typeIndex)
    {
        selectedItemType = (ItemType)typeIndex;
    }

    public void SetTransferAmount(float amount)
    {
        transferAmount = Mathf.RoundToInt(amount);
    }
}
