using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float basicSpeed = 1.0f;
    public float changeDirectionTime = 5.0f;
    public float pauseTime = 0.8f;

    private Rigidbody2D rb;
    private float changeDirectionTimer = 0.0f;
    private Vector2 moveDirection;

    [Header("Attack")]
    public float ramSpeed = 10.0f;
    public float attackTime = 2.0f;
    public float triggerRadius = 5.0f;
    public float attackCharge = 1.5f;
    public CircleCollider2D trigger;

    private bool attacking = false;
    private bool playerFound = false;
    private Transform player;
    private float attackChargeTimer = 0.0f;
    private float attackTimer = 0.0f;

    [Header("Stats")]
    public float statDiversity = 0.5f;
    public int hp = 3;

    private SpriteRenderer sr;

    private void Start()
    {
        // Randomize stats
        basicSpeed *= Random.Range(1 - statDiversity, 1 + statDiversity);
        changeDirectionTime *= Random.Range(1 - statDiversity, 1 + statDiversity);
        pauseTime *= Random.Range(1 - statDiversity, 1 + statDiversity);
        ramSpeed *= Random.Range(1 - statDiversity, 1 + statDiversity);
        attackTime *= Random.Range(1 - statDiversity, 1 + statDiversity);
        triggerRadius *= Random.Range(1 - statDiversity, 1 + statDiversity);
        attackCharge *= Random.Range(1 - statDiversity, 1 + statDiversity);

        moveDirection = new Vector2(Random.Range(-1, 1), Random.Range(-1, 1));
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        trigger.radius = triggerRadius;
    }

    private void Update()
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
                rb.linearVelocity = moveDirection * ramSpeed;
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
                moveDirection = (new Vector2(Random.Range(-1, 1), Random.Range(-1, 1))).normalized;
                changeDirectionTimer = 0;
            }
            else if (changeDirectionTimer > changeDirectionTime * pauseTime)
            {
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                rb.linearVelocity = moveDirection * basicSpeed;
            }
        }
    }

    public void OnPlayerFound()
    {
        playerFound = true;
    }

    public void OnPlayerLost()
    {
        playerFound = false;
    }

    public void GetDamage(int dmg)
    {
        hp -= dmg;

        if (hp <= 0)
        {
            transform.parent.SendMessage("onKilled", transform.position);
            Destroy(gameObject);
        }
    }
}
