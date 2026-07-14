using System;
using UnityEngine;

interface IAttack
{
    public void Initialize(AttackData data, Transform parent);
}


[Serializable]
public class AttackData
{
    [Header("Attack settinfgs")]
    public float dmg;
    public float speed;
    public float range;
}
