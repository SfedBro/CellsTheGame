using System;
using UnityEngine;

interface IAttack
{
    public void Initialize(AttackData data, Transform parent, float rotation);
    public void Activate();
}


[Serializable]
public class AttackData
{
    [Header("Attack settinfgs")]
    public float dmg;
    public float timeToLive;
    public float speed;

    [Header("Cannon settings")]
    public float attakCoolDown;
    public float activationTime;
}
