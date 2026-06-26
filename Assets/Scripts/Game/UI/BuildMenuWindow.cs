using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BuildMenuWindow : MonoBehaviour
{
    public static BuildMenuWindow Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject uiPanel;
    public Transform gridContainer;
    public GameObject slotPrefab;

    [Header("Optional Toggles")]
    public bool closeOnSelect = true;

    private List<GameObject> activeSlots = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
            
        uiPanel.SetActive(false);
    }

    public void ToggleWindow()
    {
        if (uiPanel.activeSelf)
            Close();
        else
            Open();
    }

    public void Open()
    {
        uiPanel.SetActive(true);
        PopulateGrid();
    }

    public void Close()
    {
        uiPanel.SetActive(false);
    }

    private void PopulateGrid()
    {
        // Очищаем старые слоты
        foreach (var slot in activeSlots)
        {
            Destroy(slot);
        }
        activeSlots.Clear();

        if (BuildManager.Instance == null || BuildManager.Instance.availableBuildings == null)
            return;

        // Создаём новые
        foreach (var buildingData in BuildManager.Instance.availableBuildings)
        {
            // Пропускаем "служебные" здания или дубликаты, если нужно (например, углы)
            // Но пока выводим все.
            if (buildingData.buildingName.Contains("Corner")) 
                continue; // Не показываем углы в меню, они строятся автоматически

            GameObject slotGO = Instantiate(slotPrefab, gridContainer);
            activeSlots.Add(slotGO);

            if (slotGO.TryGetComponent<BuildMenuSlot>(out var slotScript))
            {
                slotScript.Setup(buildingData, this);
            }
        }
    }

    public void OnBuildingSelected(BuildingData data)
    {
        if (BuildManager.Instance != null)
        {
            BuildManager.Instance.SetSelectedBuilding(data);
        }

        if (closeOnSelect)
        {
            Close();
        }
    }
}
