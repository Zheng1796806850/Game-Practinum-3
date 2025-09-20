using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    public enum ProjectileType { Normal, Fire, Ice }

    [Header("Stats")]
    [SerializeField] private float baseDamage = 1;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private int maxPierce = 0;
    [SerializeField] private LayerMask hitMask;

    [Header("Effect")]
    public ProjectileType projectileType = ProjectileType.Normal;
    public float fireDotDuration = 3f;
    public float fireDotInterval = 1f;
    public float fireDotDamage = 1;
    public float iceFreezeDuration = 2f;

    private Rigidbody2D rb;
    private GameObject owner;
    private int remainingPierce;
    private float lifeTimer;

    public void Init(GameObject owner, float damage, Vector2 velocity)
    {
        this.owner = owner;
        this.baseDamage = damage;
        rb.linearVelocity = velocity;
        remainingPierce = maxPierce;
        lifeTimer = lifeTime;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        lifeTimer = lifeTime;
        remainingPierce = maxPierce;
    }

    void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Despawn();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hitMask.value != 0 && ((1 << other.gameObject.layer) & hitMask.value) == 0)
            return;

        if (owner != null)
        {
            var otherRoot = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
            if (otherRoot == owner || otherRoot.transform.root == owner.transform.root) return;
        }

        if (other.TryGetComponent<IDamageable>(out var dmg))
        {
            dmg.ApplyDamage(baseDamage, owner);

            if (other.TryGetComponent<Health>(out var health))
            {
                if (projectileType == ProjectileType.Fire)
                {
                    health.ApplyDot(fireDotDamage, fireDotDuration, fireDotInterval, owner);
                }
                else if (projectileType == ProjectileType.Ice)
                {
                    health.ApplyFreeze(iceFreezeDuration);
                }
            }
        }

        if (remainingPierce > 0)
        {
            remainingPierce--;
            if (remainingPierce <= 0 && destroyOnHit) Despawn();
        }
        else if (destroyOnHit)
        {
            Despawn();
        }
    }

    private void Despawn()
    {
        Destroy(gameObject);
    }
}
