using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class UpgradeSaveData
{
    public string statType;
    public int curLevel;
}

[Serializable]
public class BuildingSaveData
{
    public string blockId;
    public Vector3Int position;
    public int zRotation;
    public string customDataJson;
}

[Serializable]
public class GameSaveData
{
    public Inventory playerInventory;
    public List<BuildingSaveData> buildings = new List<BuildingSaveData>();
    public List<UpgradeSaveData> upgrades = new List<UpgradeSaveData>();
}

public class SaveManager : MonoBehaviour, IGameService
{
    public static SaveManager Instance;

    public bool autoLoadOnStart = true;
    public bool autoSaveOnQuit = true;

    private string SavePath => Application.persistentDataPath + "/save.json";

    public void InitializeService()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private bool isInitialized = false;

    public void StartService()
    {
        if (Instance != this) return;

        if (autoLoadOnStart)
        {
            LoadGame();
        }
        isInitialized = true;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only reload buildings if we are returning to the Factory scene
        // and if it's not the initial load (StartService handles initial load)
        if (scene.name != "Foraging" && Instance == this && isInitialized)
        {
            if (autoLoadOnStart)
            {
                LoadGame(false);
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) SaveGame();
        if (Input.GetKeyDown(KeyCode.F9)) LoadGame();
    }

    private void OnApplicationQuit()
    {
        if (autoSaveOnQuit)
        {
            SaveGame();
        }
    }

    [ContextMenu("Save Game")]
    public void SaveGame()
    {
        GameSaveData data = new GameSaveData();

        // 1. Save Player Inventory
        if (PlayerInventory.Instance != null)
        {
            data.playerInventory = PlayerInventory.Instance.Inventory;
        }

        // 2. Save Factory
        if (GridManager.Instance != null)
        {
            // Use distinct to avoid saving multi-cell buildings multiple times
            var uniqueBuildings = new HashSet<FactoryBlock>();

            // Accessing buildings via GridManager (assuming we iterate through something, but we need all buildings)
            // Since buildings dictionary is private, we can just FindObjectsByType
            FactoryBlock[] allBlocks = FindObjectsByType<FactoryBlock>(FindObjectsSortMode.None);
            foreach (var block in allBlocks)
            {
                if (string.IsNullOrEmpty(block.blockId)) continue; // Can't save blocks without ID

                BuildingSaveData bsd = new BuildingSaveData
                {
                    blockId = block.blockId,
                    position = block.GridPosition,
                    zRotation = Mathf.RoundToInt(block.transform.eulerAngles.z),
                    customDataJson = block.GetSaveState()
                };
                data.buildings.Add(bsd);
            }
        }

        // 3. Save Upgrades
        if (PlayerLevelManager.instance != null)
        {
            foreach (var upg in PlayerLevelManager.instance.getPlayerUpgrades())
            {
                data.upgrades.Add(new UpgradeSaveData
                {
                    statType = upg.GetStatType().ToString(),
                    curLevel = upg.curLevel
                });
            }
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log("Game Saved to: " + SavePath);
    }

    [ContextMenu("Load Game")]
    public void LoadGame(bool loadInventory = true)
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("Save file not found!");
            return;
        }

        string json = File.ReadAllText(SavePath);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

        // 1. Load Player Inventory
        if (loadInventory && PlayerInventory.Instance != null && data.playerInventory != null)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(data.playerInventory), PlayerInventory.Instance.Inventory);
        }

        // 2. Load Upgrades
        if (PlayerLevelManager.instance != null)
        {
            foreach (var upgSave in data.upgrades)
            {
                var match = PlayerLevelManager.instance.getPlayerUpgrades().Find(u => u.GetStatType().ToString() == upgSave.statType);
                if (match != null)
                {
                    match.curLevel = upgSave.curLevel;
                }
            }

            // Force UpgradingManager to update UI and Player stats
            UpgradingManager um = FindFirstObjectByType<UpgradingManager>();
            if (um != null)
            {
                um.ForceUpdateAfterLoad();
            }
        }

        // 3. Load Factory
        BuildManager buildManager = FindFirstObjectByType<BuildManager>();
        if (buildManager != null)
        {
            // Destroy existing blocks
            FactoryBlock[] allBlocks = FindObjectsByType<FactoryBlock>(FindObjectsSortMode.None);
            foreach (var block in allBlocks)
            {
                block.OnRemoved();
                Destroy(block.gameObject);
            }

            // Spawn from save
            foreach (var bsd in data.buildings)
            {
                buildManager.SpawnBuildingFromSave(bsd.blockId, bsd.position, bsd.zRotation, bsd.customDataJson);
            }
        }

        Debug.Log("Game Loaded successfully.");
    }
}
