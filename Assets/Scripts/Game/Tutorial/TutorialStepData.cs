using UnityEngine;

public enum TutorialCompletionType
{
    ClickNext,      // Сlick Next
    PerformAction   // Perform a specific action in the game
}

[CreateAssetMenu(fileName = "NewTutorialStep", menuName = "Tutorial/Step Data")]
public class TutorialStepData : ScriptableObject
{
    public string stepName;
    [TextArea(3, 5)]
    public string description;
    
    public TutorialCompletionType completionType;
    
    [Tooltip("If CompletionType is PerformAction, what is the ID of the action we wait for? (e.g. Build_Miner)")]
    public string targetActionId;

    [Tooltip("The tag or path of the UI element to highlight/unmask. Leave empty for no highlight.")]
    public string highlightElementTag;
}
