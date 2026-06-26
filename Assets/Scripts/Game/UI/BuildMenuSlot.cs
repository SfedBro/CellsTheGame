using UnityEngine;
using UnityEngine.UI;

public class BuildMenuSlot : MonoBehaviour
{
    public Image iconImage;
    public Button button;

    private BuildingData buildingData;
    private BuildMenuWindow menuWindow;

    public void Setup(BuildingData data, BuildMenuWindow window)
    {
        buildingData = data;
        menuWindow = window;

        if (iconImage != null)
        {
            if (data.icon != null)
            {
                iconImage.sprite = data.icon;
            }
            else if (data.prefab != null)
            {
                // Fallback к спрайту префаба (даже если он на дочернем объекте)
                SpriteRenderer sr = data.prefab.GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    iconImage.sprite = sr.sprite;
                }
            }
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (buildingData != null && menuWindow != null)
        {
            menuWindow.OnBuildingSelected(buildingData);
        }
    }
}
