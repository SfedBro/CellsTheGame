using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTutorialSequence", menuName = "Tutorial/Sequence Data")]
public class TutorialSequenceData : ScriptableObject
{
    public List<TutorialStepData> steps = new List<TutorialStepData>();
}
