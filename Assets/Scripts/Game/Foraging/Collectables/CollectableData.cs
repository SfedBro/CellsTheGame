using UnityEngine;

[CreateAssetMenu(fileName = "NewCollectable", menuName = "Collectable/Resource")]
public class CollectableData : ScriptableObject
{
    [Header("Self")]
    [SerializeField] private ItemType type;
    [SerializeField] private Sprite sprite;
    [SerializeField] private float colliderRadius;
    [SerializeField] private int allowedRadiusMin;
    [SerializeField] private int allowedRadiusMax;

    [Header("Transformation")]
    [SerializeField] private ItemType transformTo;
    [SerializeField] private float chance;

    public ItemType GetItemType() => type;
    public Sprite GetSprite() => sprite;
    public float GetRadius() => colliderRadius;
    public Vector2 getNewPosition()
    {
        Vector2 vec = Random.insideUnitCircle;
        return vec * (allowedRadiusMin + vec.magnitude * (allowedRadiusMax - allowedRadiusMin));
    }

    public ItemType newType()
    {
        if (Random.Range(0, 1) <= chance)
        {
            return transformTo;
        }

        return type;
    }
}

