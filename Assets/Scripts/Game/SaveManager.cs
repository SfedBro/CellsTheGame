using System.IO;
using UnityEngine;

public static class SaveManager
{
    private static string GetFilePath(string key)
    {
        return Path.Combine(Application.persistentDataPath, key + ".json");
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
