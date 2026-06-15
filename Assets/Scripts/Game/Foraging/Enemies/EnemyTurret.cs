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
    [SerializeField] private float reloadTime = 2.0f;
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
    }

    void Update()
    {
        if (player == null) playerFound = false;

        if (playerFound)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 360f;
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
            nextShootTime = Time.time + reloadTime;

            GameObject b = Instantiate(bulletPrefab, transform.position, transform.rotation);
            b.transform.parent = transform;
            b.transform.localScale = new Vector3((dmg + 3f) / 8f, (dmg + 3f) / 8f, 1);
            Bullet bullet = b.GetComponent<Bullet>();
            bullet.dmg = dmg;
            bullet.enemy = this;
        }
    }

}
