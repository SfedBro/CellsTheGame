// using UnityEngine;

// [CreateAssetMenu(fileName = "NewMine", menuName = "Structures/Mine")]
// public class MineData : ScriptableObject
// {
//     [Header("Self")]
//     [SerializeField] private CollectableData collectable;
//     [SerializeField] private Sprite sprite;
//     [SerializeField] private int allowedRadiusMin;
//     [SerializeField] private int allowedRadiusMax;

//     [Header("Spawn rules")]


//     // public ItemType GetItemType() => type;
//     // public Sprite GetSprite() => sprite;
//     public Vector2 getNewPosition()
//     {
//         Vector2 vec = Random.insideUnitCircle;
//         return vec * (allowedRadiusMin + vec.magnitude * (allowedRadiusMax - allowedRadiusMin));
//     }
// }
