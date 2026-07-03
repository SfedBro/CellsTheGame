using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SaveManagerUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentContainer; // Where save slot prefabs are spawned
    public GameObject saveSlotPrefab;  // Prefab for a save slot
    public InputField newSaveInput;    // Input field for new save name
    public string gameSceneName = "GameScene"; // Change to your actual game scene name

    private void OnEnable()
    {
        RefreshSaveList();
    }

    public void RefreshSaveList()
    {
        if (contentContainer == null || saveSlotPrefab == null) return;

        // Clear existing slots
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        List<string> saves = SaveManager.GetAvailableSaves();

        foreach (string saveName in saves)
        {
            GameObject slotGo = Instantiate(saveSlotPrefab, contentContainer);
            
            // Assuming your prefab has a text component for the name
            Text textComp = slotGo.GetComponentInChildren<Text>();
            if (textComp != null)
                textComp.text = saveName;

            // Assuming your prefab has some buttons: Load and Delete
            // You can find them by name or create a dedicated SaveSlotUI script, 
            // but for simplicity, we find buttons dynamically if you use standard naming:
            Button[] buttons = slotGo.GetComponentsInChildren<Button>();
            
            foreach (var btn in buttons)
            {
                if (btn.gameObject.name.ToLower().Contains("load") || btn.gameObject.name.ToLower().Contains("play"))
                {
                    btn.onClick.AddListener(() => LoadSave(saveName));
                }
                else if (btn.gameObject.name.ToLower().Contains("delete"))
                {
                    btn.onClick.AddListener(() => DeleteSave(saveName));
                }
            }
        }
    }

    public void CreateNewSave()
    {
        if (newSaveInput == null || string.IsNullOrWhiteSpace(newSaveInput.text))
        {
            Debug.LogWarning("Save name is empty!");
            return;
        }

        string saveName = newSaveInput.text.Trim();
        // Set as current and load scene
        SaveManager.CurrentSaveName = saveName;
        SceneManager.LoadSceneAsync(gameSceneName);
    }

    public void LoadSave(string saveName)
    {
        SaveManager.CurrentSaveName = saveName;
        SceneManager.LoadSceneAsync(gameSceneName);
    }

    public void DeleteSave(string saveName)
    {
        SaveManager.DeleteSave(saveName);
        RefreshSaveList();
    }
}
