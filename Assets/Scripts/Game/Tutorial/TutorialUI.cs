using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject tutorialPanel;
    public Text stepNameText;
    public Text descriptionText;
    public Button nextButton;
    public Button skipButton;

    [Header("Highlight Masking")]
    public GameObject maskOverlay; // A semi-transparent image filling the screen, with raycast target enabled

    private void Start()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnStepChanged += HandleStepChanged;
            TutorialManager.Instance.OnTutorialCompleted += HideTutorial;
        }

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);

        if (skipButton != null)
            skipButton.onClick.AddListener(OnSkipClicked);
            
        HideTutorial();
        
        // If manager already has a step active and we started late
        StartCoroutine(LateInit());
    }
    
    private IEnumerator LateInit()
    {
        yield return null;
        if (TutorialManager.Instance != null && !TutorialManager.Instance.IsTutorialCompleted)
        {
            
        }
    }

    private void OnDestroy()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnStepChanged -= HandleStepChanged;
            TutorialManager.Instance.OnTutorialCompleted -= HideTutorial;
        }
    }

    private void HandleStepChanged(TutorialStepData step)
    {
        tutorialPanel.SetActive(true);
        
        if (stepNameText != null)
            stepNameText.text = step.stepName;
            
        if (descriptionText != null)
            descriptionText.text = step.description;

        if (nextButton != null)
            nextButton.gameObject.SetActive(step.completionType == TutorialCompletionType.ClickNext);

        HandleHighlight(step.highlightElementTag);
    }

    private Canvas targetCanvasOverride;
    private int originalSortingOrder;

    private void HandleHighlight(string tagOrName)
    {
        // Cleanup previous highlight
        if (targetCanvasOverride != null)
        {
            Destroy(targetCanvasOverride.GetComponent<GraphicRaycaster>());
            Destroy(targetCanvasOverride);
            targetCanvasOverride = null;
        }

        if (maskOverlay != null)
        {
            maskOverlay.SetActive(!string.IsNullOrEmpty(tagOrName));
        }

        if (string.IsNullOrEmpty(tagOrName)) return;

        GameObject targetObj = GameObject.FindGameObjectWithTag(tagOrName);
        if (targetObj == null)
        {
            // Fallback to name search
            targetObj = GameObject.Find(tagOrName);
        }

        if (targetObj != null)
        {
            // Add canvas override to pop it in front of the mask
            targetCanvasOverride = targetObj.AddComponent<Canvas>();
            targetCanvasOverride.overrideSorting = true;
            targetCanvasOverride.sortingOrder = 30000; // ensure it's above the mask
            targetObj.AddComponent<GraphicRaycaster>(); // ensure it can be clicked
        }
    }

    private void OnNextClicked()
    {
        TutorialManager.Instance.NextButtonClicked();
    }

    private void OnSkipClicked()
    {
        TutorialManager.Instance.SkipTutorial();
    }

    private void HideTutorial()
    {
        tutorialPanel.SetActive(false);
        HandleHighlight(null);
    }
}
