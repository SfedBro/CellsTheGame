using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class TutorialUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI stepNameText;
    public TextMeshProUGUI descriptionText;
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
    private bool canvasWasAdded = false;
    private GameObject currentTargetObj = null;

    private void HandleHighlight(string tagOrName)
    {
        // First find the new target
        GameObject newTargetObj = null;
        if (!string.IsNullOrEmpty(tagOrName))
        {
            try 
            {
                newTargetObj = GameObject.FindGameObjectWithTag(tagOrName);
            }
            catch (UnityException) {}

            if (newTargetObj == null)
            {
                Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
                foreach(var t in allTransforms)
                {
                    if (t.name == tagOrName && t.gameObject.scene.isLoaded)
                    {
                        newTargetObj = t.gameObject;
                        break;
                    }
                }
            }
        }

        // If it's the exact same object as before, don't recreate the Canvas
        if (newTargetObj != null && newTargetObj == currentTargetObj)
        {
            if (maskOverlay != null) maskOverlay.SetActive(true);
            return;
        }

        // Cleanup previous highlight
        if (targetCanvasOverride != null)
        {
            if (canvasWasAdded)
            {
                Destroy(targetCanvasOverride.GetComponent<GraphicRaycaster>());
                Destroy(targetCanvasOverride);
            }
            else
            {
                targetCanvasOverride.overrideSorting = false;
                targetCanvasOverride.sortingOrder = originalSortingOrder;
            }
            targetCanvasOverride = null;
            canvasWasAdded = false;
        }
        
        currentTargetObj = newTargetObj;

        if (maskOverlay != null)
        {
            maskOverlay.SetActive(!string.IsNullOrEmpty(tagOrName));
        }

        if (string.IsNullOrEmpty(tagOrName)) return;

        GameObject targetObj = newTargetObj;

        if (targetObj != null)
        {
            Debug.Log($"[TutorialUI] Нашли объект для подсветки: {targetObj.name}. Добавляем Canvas...");
            // Add canvas override to pop it in front of the mask
            Canvas existingCanvas = targetObj.GetComponent<Canvas>();
            if (existingCanvas == null)
            {
                targetCanvasOverride = targetObj.AddComponent<Canvas>();
                targetCanvasOverride.overrideSorting = true;
                
                // Скопируем слой у родительского канваса, чтобы точно быть выше маски в том же слое
                Canvas parentCanvas = targetObj.transform.parent != null ? targetObj.transform.parent.GetComponentInParent<Canvas>() : null;
                if (parentCanvas != null)
                {
                    targetCanvasOverride.sortingLayerID = parentCanvas.sortingLayerID;
                }
                
                targetCanvasOverride.sortingOrder = 30000; // ensure it's above the mask
                targetObj.AddComponent<GraphicRaycaster>(); // ensure it can be clicked
                canvasWasAdded = true;
            }
            else
            {
                Debug.Log($"[TutorialUI] У объекта {targetObj.name} уже есть Canvas! Изменяем его Sorting Order.");
                existingCanvas.overrideSorting = true;
                originalSortingOrder = existingCanvas.sortingOrder;
                existingCanvas.sortingOrder = 30000;
                targetCanvasOverride = existingCanvas;
                canvasWasAdded = false;
            }
        }
        else
        {
            Debug.LogWarning($"[TutorialUI] Объект с именем или тегом '{tagOrName}' НЕ НАЙДЕН на сцене!");
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
