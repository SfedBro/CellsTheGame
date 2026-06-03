using UnityEngine;

public class EnemyRam : EnemyBase
{
    [Header("Base")]
    [SerializeField] private Sprite sprite;

    [Header("Movement")]
    [SerializeField] private float changeDirectionTime = 5.0f;
    [SerializeField] private float pauseTime = 0.8f;

    private float changeDirectionTimer = 0.0f;

    [Header("Attack")]
    [SerializeField] private float ramSpeedMultipler = 3.0f;
    [SerializeField] private float attackTime = 2.0f;
    [SerializeField] private float basetriggerRadius = 5.0f;
    [SerializeField] private float seekTriggerRadius = 10.0f;
    [SerializeField] private float attackCharge = 1.5f;
    [SerializeField] private CircleCollider2D trigger;

    private bool attacking = false;
    private float attackChargeTimer = 0.0f;
    private float attackTimer = 0.0f;

    protected new void OnStartSeek()
    {
        trigger.radius = seekTriggerRadius;
    }

    protected new void OnStopSeek()
    {
        trigger.radius = basetriggerRadius;
    }

    new void Start()
    {
        base.Start();
        sr.sprite = sprite;
        trigger.radius = basetriggerRadius;
    }

    void Update()
    {
        if (attacking)
        {
            if (attackChargeTimer < attackCharge)
            {
                attackChargeTimer += Time.deltaTime;
                float color = 1 - (attackChargeTimer / attackCharge);
                sr.color = new Color(1, color, color);
            }
            else if (attackTimer < attackTime)
            {
                attackTimer += Time.deltaTime;
                rb.linearVelocity = moveDirection * moveSpeed * ramSpeedMultipler;
                float color = attackChargeTimer / attackCharge;
                sr.color = new Color(1, color, color);
            }
            else
            {
                sr.color = new Color(1, 1, 1);
                rb.linearVelocity = Vector2.zero;
                moveDirection = Vector2.zero;
                attackChargeTimer = 0;
                attackTimer = 0;
                attacking = false;
            }
        }
        else
        {
            if (playerFound)
            {
                attacking = true;
                moveDirection = (player.position - transform.position).normalized;
                rb.linearVelocity = Vector2.zero;
                return;
            }

            changeDirectionTimer += Time.deltaTime;
            if (changeDirectionTimer > changeDirectionTime)
            {
                moveDirection = Random.insideUnitCircle.normalized;
                changeDirectionTimer = 0;
            }
            else if (changeDirectionTimer > changeDirectionTime * pauseTime)
            {
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                rb.linearVelocity = moveDirection * moveSpeed;
            }
        }
    }

}
