using UnityEngine;

public class EnemyTurret : EnemyBase
{
    [Header("Base")]
    [SerializeField] private Sprite sprite;

    [Header("Rotation")]
    [SerializeField] private float changeDirectionTime = 3f;
    private float changeDirectionTimer = 0f;
    private float currentAngle;
    private float targetAngle;


    [Header("Attack")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private AttackData attackData;
    [SerializeField] private float attackCoolDown = 2f;
    [SerializeField] private float basetriggerRadius = 5.0f;
    [SerializeField] private float seekTriggerRadius = 10.0f;
    [SerializeField] private CircleCollider2D trigger;
    private float nextShootTime = 0.0f;

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

        currentAngle = rb.rotation;
        targetAngle = currentAngle;
        rb.linearVelocity = Vector2.zero;

        attackData.dmg = dmg;
        attackData.speed = curHP;
        attackData.range = moveSpeed;
    }

    void Update()
    {
        if (player == null) playerFound = false;

        if (playerFound)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        }
        else
        {
            changeDirectionTimer += Time.deltaTime;

            if (changeDirectionTimer > changeDirectionTime)
            {
                changeDirectionTimer = 0f;
                Vector2 direction = Random.insideUnitCircle;
                targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 360f;
            }
        }

        currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, moveSpeed * Time.deltaTime);
        rb.MoveRotation(currentAngle);

        if (playerFound) TryShoot();
    }

    private void TryShoot()
    {
        if (Time.time >= nextShootTime)
        {
            nextShootTime = Time.time + attackCoolDown;

            Bullet bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity).GetComponent<Bullet>();
            bullet.Initialize(attackData, transform);
            bullet.enemy = this;
        }
    }

}
