using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeSelectionPopup : MonoBehaviour
{
    public static RecipeSelectionPopup Instance { get; private set; }

    [Header("References")]
    [SerializeField] private RectTransform popupRect;
    [SerializeField] private Transform buttonsContainer;
    [SerializeField] private GameObject recipeButtonPrefab;
    [SerializeField] private Canvas canvas;

    [Header("Positioning Settings")]
    [SerializeField] private Vector2 positionOffset = new Vector2(0, 120);

    private Action<RecipeData> onRecipeSelectedCallback;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Auto-locate components if not set
        if (popupRect == null) popupRect = GetComponent<RectTransform>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();

        // Hide by default
        gameObject.SetActive(false);
    }

    public void OpenPopup(RectTransform buttonRect, List<RecipeData> recipes, RecipeData currentRecipe, Action<RecipeData> onSelect)
    {
        if (buttonRect == null || recipes == null || recipes.Count == 0)
        {
            Close();
            return;
        }

        onRecipeSelectedCallback = onSelect;
        gameObject.SetActive(true);

        // 1. Position popup above the UI button
        PositionAboveButton(buttonRect);

        // 2. Populate recipe buttons
        PopulateButtons(recipes, currentRecipe);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        onRecipeSelectedCallback = null;
    }

    private void PositionAboveButton(RectTransform buttonRect)
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null || buttonRect == null) return;

        // Get screen position of the button
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, buttonRect.position);

        // Convert screen space to local position inside canvas RectTransform
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, canvas.worldCamera, out Vector2 localPoint))
        {
            // Apply offset (place it above the button)
            localPoint += positionOffset;

            // Clamp popup position to keep it fully on screen
            float halfWidth = popupRect.rect.width * 0.5f;
            float halfHeight = popupRect.rect.height * 0.5f;

            float minX = canvasRect.rect.xMin + halfWidth;
            float maxX = canvasRect.rect.xMax - halfWidth;
            float minY = canvasRect.rect.yMin + halfHeight;
            float maxY = canvasRect.rect.yMax - halfHeight;

            localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
            localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

            // Apply position
            popupRect.anchoredPosition = localPoint;
        }
    }

    private void PopulateButtons(List<RecipeData> recipes, RecipeData currentRecipe)
    {
        if (buttonsContainer == null || recipeButtonPrefab == null) return;

        // Clear old buttons
        foreach (Transform child in buttonsContainer)
        {
            Destroy(child.gameObject);
        }

        // Spawn a button for each recipe
        foreach (var recipe in recipes)
        {
            if (recipe == null) continue;

            GameObject btnGo = Instantiate(recipeButtonPrefab, buttonsContainer);
            btnGo.name = $"Recipe_{recipe.RecipeName}";

            // Set up button label
            TMP_Text label = btnGo.GetComponentInChildren<TMP_Text>();
            if (label == null) label = btnGo.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = recipe.RecipeName;
            }

            // Set up visual selection state
            Image img = btnGo.GetComponent<Image>();
            if (img != null)
            {
                bool isSelected = (recipe == currentRecipe);
                img.color = isSelected ? new Color(0.2f, 0.6f, 0.3f, 1f) : new Color(0.25f, 0.25f, 0.28f, 1f);
            }

            // Bind click action
            Button btn = btnGo.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    onRecipeSelectedCallback?.Invoke(recipe);
                    Close();
                });
            }
        }
    }
}
