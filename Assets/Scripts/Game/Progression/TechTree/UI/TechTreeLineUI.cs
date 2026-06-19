using UnityEngine;
using UnityEngine.UI;

public class TechTreeLineUI : MonoBehaviour
{
    public TechTreeNodeUI sourceNode;
    public TechTreeNodeUI targetNode;

    private RectTransform rectTransform;
    private Image image;

    [Header("Colors")]
    public Color hiddenColor = new Color(0, 0, 0, 0); // Transparent
    public Color availableColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color unlockedColor = new Color(1f, 0.8f, 0f, 1f);

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    public void Setup(TechTreeNodeUI source, TechTreeNodeUI target)
    {
        sourceNode = source;
        targetNode = target;

        rectTransform.pivot = new Vector2(0, 0.5f);
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        if (sourceNode == null || targetNode == null) return;

        Vector3 posA = sourceNode.GetComponent<RectTransform>().position;
        Vector3 posB = targetNode.GetComponent<RectTransform>().position;

        rectTransform.position = posA;

        Vector3 dir = posB - posA;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rectTransform.rotation = Quaternion.Euler(0, 0, angle);

        float distance = dir.magnitude;
        rectTransform.sizeDelta = new Vector2(distance, rectTransform.sizeDelta.y); // preserve thickness
    }

    public void Refresh()
    {
        if (sourceNode == null || targetNode == null || TechTreeManager.Instance == null) return;

        bool targetUnlocked = TechTreeManager.Instance.IsNodeUnlocked(targetNode.nodeData.nodeId);
        bool targetAvailable = TechTreeManager.Instance.AreDependenciesMet(targetNode.nodeData);

        if (targetUnlocked)
        {
            image.color = unlockedColor;
        }
        else if (targetAvailable)
        {
            image.color = availableColor;
        }
        else
        {
            image.color = hiddenColor;
        }
    }
}
