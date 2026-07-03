using System;
using UnityEngine;

[Serializable]
public class TutorialSaveData
{
    public bool isTutorialCompleted;
    public int currentStepIndex;
}

public class TutorialManager : MonoBehaviour, IGameService
{
    public static TutorialManager Instance { get; private set; }

    [SerializeField]
    private TutorialSequenceData mainSequence;

    private TutorialSaveData saveData;
    private string saveKey = "TutorialProgressSave";

    public event Action<TutorialStepData> OnStepChanged;
    public event Action OnTutorialCompleted;
    
    public bool IsTutorialCompleted => saveData != null && saveData.isTutorialCompleted;

    public void InitializeService()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void StartService()
    {
        if (Instance != this) return;
        Load();
        
        if (!saveData.isTutorialCompleted && mainSequence != null && mainSequence.steps.Count > 0)
        {
            // Trigger UI for the current step
            OnStepChanged?.Invoke(mainSequence.steps[saveData.currentStepIndex]);
        }
    }

    public void NotifyAction(string actionId)
    {
        if (saveData == null || saveData.isTutorialCompleted || mainSequence == null) return;
        if (saveData.currentStepIndex >= mainSequence.steps.Count) return;

        var currentStep = mainSequence.steps[saveData.currentStepIndex];
        if (currentStep.completionType == TutorialCompletionType.PerformAction &&
            string.Equals(currentStep.targetActionId, actionId, StringComparison.OrdinalIgnoreCase))
        {
            AdvanceStep();
        }
    }

    public void NextButtonClicked()
    {
        if (saveData == null || saveData.isTutorialCompleted || mainSequence == null) return;
        if (saveData.currentStepIndex >= mainSequence.steps.Count) return;

        var currentStep = mainSequence.steps[saveData.currentStepIndex];
        if (currentStep.completionType == TutorialCompletionType.ClickNext)
        {
            AdvanceStep();
        }
    }

    public void SkipTutorial()
    {
        if (saveData == null || saveData.isTutorialCompleted) return;
        saveData.isTutorialCompleted = true;
        Save();
        OnTutorialCompleted?.Invoke();
    }

    private void AdvanceStep()
    {
        saveData.currentStepIndex++;
        Save();

        if (saveData.currentStepIndex >= mainSequence.steps.Count)
        {
            saveData.isTutorialCompleted = true;
            Save();
            OnTutorialCompleted?.Invoke();
        }
        else
        {
            OnStepChanged?.Invoke(mainSequence.steps[saveData.currentStepIndex]);
        }
    }

    public void Save()
    {
        if (saveData == null) return;
        SaveManager.Save(saveKey, saveData);
    }

    public void Load()
    {
        saveData = SaveManager.Load<TutorialSaveData>(saveKey);
        if (saveData == null)
        {
            saveData = new TutorialSaveData { isTutorialCompleted = false, currentStepIndex = 0 };
        }
    }
}
