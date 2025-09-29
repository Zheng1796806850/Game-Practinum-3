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
        if (lifeTimer <= 0f) Despawn();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hitMask.value != 0 && ((1 << other.gameObject.layer) & hitMask.value) == 0)
            return;

        GameObject otherRoot = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        if (owner != null)
        {
            if (otherRoot == owner || otherRoot.transform.root == owner.transform.root) return;
        }

        DamageType dtype = DamageType.Normal;
        if (projectileType == ProjectileType.Fire) dtype = DamageType.Fire;
        else if (projectileType == ProjectileType.Ice) dtype = DamageType.Ice;

        float dmg = baseDamage;

        if (otherRoot.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.ApplyDamage(new DamageInfo(dmg, dtype, owner));
        }

        if (projectileType == ProjectileType.Fire)
        {
            if (otherRoot.TryGetComponent<Health>(out var health))
            {
                health.ApplyDot(fireDotDamage, fireDotDuration, fireDotInterval, owner);
            }

            if (other.TryGetComponent<WaterPlatform>(out var water))
            {
                water.TryMeltFromFire();
            }
            else
            {
                var parentWater = other.GetComponentInParent<WaterPlatform>();
                if (parentWater != null) parentWater.TryMeltFromFire();
            }
        }
        else if (projectileType == ProjectileType.Ice)
        {
            if (otherRoot.TryGetComponent<Health>(out var health))
            {
                health.ApplyFreeze(iceFreezeDuration);
            }
            else if (other.TryGetComponent<IFreezable>(out var freezable))
            {
                freezable.ApplyFreeze(iceFreezeDuration);
            }
            else
            {
                var parentFreezable = other.GetComponentInParent<IFreezable>();
                if (parentFreezable != null) parentFreezable.ApplyFreeze(iceFreezeDuration);
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
