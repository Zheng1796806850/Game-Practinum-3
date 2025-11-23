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
    public float bonusDamageIfTargetFrozen = 0f;
    public GameObject onHitParticlePrefab;

    [Header("Freeze / Release")]
    public bool clearFreezeOnHit = false;
    public bool releaseCapturedEnemyOnHit = false;

    [Header("Target Filter")]
    public bool limitDamageToTag = false;
    public string damageOnlyTag = "";

    [Header("Switch Interaction")]
    public bool useSwitchTagFilter = false;
    public string switchTag = "";
    public bool activateSwitchOnHit = false;
    public bool deactivateSwitchOnHit = false;

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

        if (releaseCapturedEnemyOnHit)
        {
            PressurePlateSwitch.NotifyEnemyBoopedPull(other);
        }

        DamageType dtype = DamageType.Normal;
        if (projectileType == ProjectileType.Fire) dtype = DamageType.Fire;
        else if (projectileType == ProjectileType.Ice) dtype = DamageType.Ice;

        float dmg = baseDamage;

        IDamageable damageable = null;
        bool hasDamageable = otherRoot.TryGetComponent<IDamageable>(out damageable);

        if (hasDamageable)
        {
            bool allowDamage = true;
            if (limitDamageToTag && !string.IsNullOrEmpty(damageOnlyTag))
            {
                if (!otherRoot.CompareTag(damageOnlyTag)) allowDamage = false;
            }

            Health h = null;
            bool hasHealth = otherRoot.TryGetComponent<Health>(out h);
            bool frozenBefore = hasHealth && h.IsFrozen;

            if (allowDamage)
            {
                damageable.ApplyDamage(new DamageInfo(dmg, dtype, owner));

                if (projectileType == ProjectileType.Ice)
                {
                    if (clearFreezeOnHit)
                    {
                        if (hasHealth && h != null)
                        {
                            h.ClearFreeze();
                        }
                    }
                    else
                    {
                        if (hasHealth && h != null)
                        {
                            h.ApplyFreeze(iceFreezeDuration);
                            if (bonusDamageIfTargetFrozen > 0f)
                            {
                                if (h.IsFrozen || frozenBefore)
                                {
                                    damageable.ApplyDamage(new DamageInfo(bonusDamageIfTargetFrozen, DamageType.Normal, owner));
                                }
                            }
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
                }
                else if (projectileType == ProjectileType.Fire)
                {
                    if (hasHealth && h != null)
                    {
                        h.ApplyDot(fireDotDamage, fireDotDuration, fireDotInterval, owner);
                    }
                }
            }
        }

        if (projectileType == ProjectileType.Fire)
        {
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
            if (!hasDamageable && !clearFreezeOnHit)
            {
                if (otherRoot.TryGetComponent<Health>(out var h4))
                {
                    h4.ApplyFreeze(iceFreezeDuration);
                }
                else if (other.TryGetComponent<IFreezable>(out var freezable2))
                {
                    freezable2.ApplyFreeze(iceFreezeDuration);
                }
                else
                {
                    var parentFreezable2 = other.GetComponentInParent<IFreezable>();
                    if (parentFreezable2 != null) parentFreezable2.ApplyFreeze(iceFreezeDuration);
                }
            }
        }

        if (activateSwitchOnHit || deactivateSwitchOnHit)
        {
            BulletSwitch sw = otherRoot.GetComponent<BulletSwitch>();
            if (sw == null) sw = otherRoot.GetComponentInChildren<BulletSwitch>();
            if (sw == null) sw = other.GetComponentInParent<BulletSwitch>();
            if (sw != null)
            {
                if (!useSwitchTagFilter || string.IsNullOrEmpty(switchTag) || sw.gameObject.CompareTag(switchTag))
                {
                    if (activateSwitchOnHit) sw.ActivateExtern();
                    if (deactivateSwitchOnHit) sw.DeactivateExtern();
                }
            }
        }

        if (onHitParticlePrefab != null)
        {
            Instantiate(onHitParticlePrefab, transform.position, Quaternion.identity);
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
