using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class SaveManager
{
    public static string CurrentSaveName = "Default";

    public static string GetSavesDirectory()
    {
        string dir = Path.Combine(Application.persistentDataPath, "Saves");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return dir;
    }

    public static string GetCurrentSaveDirectory()
    {
        string dir = Path.Combine(GetSavesDirectory(), CurrentSaveName);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return dir;
    }

    public static List<string> GetAvailableSaves()
    {
        string dir = GetSavesDirectory();
        return Directory.GetDirectories(dir).Select(d => new DirectoryInfo(d).Name).ToList();
    }

    public static void DeleteSave(string saveName)
    {
        string path = Path.Combine(GetSavesDirectory(), saveName);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            Debug.Log($"[SaveManager] Deleted save '{saveName}'");
        }
    }

    private static string GetFilePath(string key)
    {
        return Path.Combine(GetCurrentSaveDirectory(), key + ".json");
    }

    public static void Save<T>(string key, T saveData)
    {
        string jsonDataString = JsonUtility.ToJson(saveData, true);
        string path = GetFilePath(key);
        File.WriteAllText(path, jsonDataString);
        Debug.Log($"[SaveManager] Saved {key} to {path}");
    }

    public static T Load<T>(string key) where T : new()
    {
        string path = GetFilePath(key);
        if (File.Exists(path))
        {
            string loadedString = File.ReadAllText(path);
            return JsonUtility.FromJson<T>(loadedString);
        }
        else
        {
            Debug.Log($"[SaveManager] No save found for {key} at {path}. Creating default.");
            return new T();
        }
    }
}
//Encryptor can be created later

/*
How to use:
create a class(better to use the same format/name
//className - one of the class names in SaveData, data - example of a name, type var ~ auto, but better and not dynamic, but adaptive

private SaveData.[className] GetSaveSnapshot()
{
    var data = new SaveData.[className]()
    {
        dataFromSaveData = dataFromScript;
        otherDataFromSave = otherDataFromScript;
    };
    return data;
}

private void Save() {
    SaveManager.Save(saveKey, GetSaveSnapshot());
}

private void Load()
{
    var data = SaveManager.Load<SaveData.[className]>(saveKey);
    //apply like:
    dataFromScript.SetValueWithoutNotify(data.dataFromSaveData);
    //or:
    otherDataFromScript = data.otherDataFromSave;
}
*/
