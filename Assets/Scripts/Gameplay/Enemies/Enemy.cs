using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Weapon))]
public class Enemy : MonoBehaviour
{
    public Transform player;
    public float attackRange = 8f;
    public float attackInterval = 2f;
    public Transform graphics;

    private Health health;
    private Weapon weapon;
    private float attackTimer;
    private bool facingRight = true;

    void Awake()
    {
        health = GetComponent<Health>();
        weapon = GetComponent<Weapon>();
        health.OnDied += OnDeath;
    }

    void Start()
    {
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }
    }

    void Update()
    {
        if (health != null && health.IsFrozen)
        {
            return;
        }

        if (player == null) return;
        attackTimer -= Time.deltaTime;
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackRange && attackTimer <= 0f)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            if ((dir.x > 0 && !facingRight) || (dir.x < 0 && facingRight)) Flip();
            weapon.Fire(dir);
            attackTimer = attackInterval;
        }
    }

    private void OnDeath(GameObject killer)
    {
        Destroy(gameObject);
    }

    private void Flip()
    {
        facingRight = !facingRight;
        if (graphics != null)
        {
            var s = graphics.localScale;
            s.x *= -1f;
            graphics.localScale = s;
        }
        else
        {
            var s = transform.localScale;
            s.x *= -1f;
            transform.localScale = s;
        }
    }
}
