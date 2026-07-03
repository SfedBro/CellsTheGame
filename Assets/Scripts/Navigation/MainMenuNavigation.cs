using SaveData;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenuNavigation : MonoBehaviour
{
    public void SwitchToScene(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName);
    }
    public void ExitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.R))
        {
            string saveKey = "TechTreeSave";
            var data = new SaveData.TechTreeData();
            data.unlockedNodeIds = new List<string>();
            SaveManager.Save(saveKey, data);
            Debug.Log("Saved Tech Tree");
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            DeleteAllSaves();
        }
    }

    public void DeleteAllSaves()
    {
        string dir = SaveManager.GetSavesDirectory();
        if (System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.Delete(dir, true);
            Debug.Log("Deleted ALL saves directory: " + dir);
        }
    }
}
