using UnityEngine;
public static class SaveManager
{
    public static void Save<T>(string key, T saveData)
    {
        string jsonDataString = JsonUtility.ToJson(saveData, true);
        PlayerPrefs.SetString(key, jsonDataString);
    }
    public static T Load<T>(string key) where T : new()
    {
        if (PlayerPrefs.HasKey(key))
        {
            string loadedString = PlayerPrefs.GetString(key);
            return JsonUtility.FromJson<T>(loadedString);
        }
        else
        {
            Debug.Log("Horrible case");
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
