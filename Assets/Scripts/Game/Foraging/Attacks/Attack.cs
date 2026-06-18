using System;
using UnityEngine;

interface IAttack
{
    public void Initialize(AttackData data, Transform parent, float rotation);
}


[Serializable]
public class AttackData
{
    public int dmg;
    public float timeToLive;
    public float speed;
}
