using UnityEngine;

public class FloorElementZone : MonoBehaviour, IFreezable
{
    public enum ElementType { Water, Lava }

    [Header("Type")]
    [SerializeField] private ElementType elementType = ElementType.Water;

    [Header("Appearance")]
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Color unfrozenColor = new Color(0.08f, 0.22f, 0.55f, 1f);
    [SerializeField] private Color frozenColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField, Range(0f, 1f)] private float unfrozenAlpha = 0.7f;
    [SerializeField, Range(0f, 1f)] private float frozenAlpha = 1f;

    [Header("Colliders")]
    [SerializeField] private BoxCollider2D detectionTrigger;
    [SerializeField] private BoxCollider2D solidColliderWhenFrozen = null;

    [Header("Freeze/Melt Rules")]
    [SerializeField] private bool freezeByIce = true;
    [SerializeField] private bool freezeByFire = false;
    [SerializeField] private bool meltByFire = true;
    [SerializeField] private bool meltByIce = false;
    [SerializeField] private bool enableSolidWhenFrozen = true;

    [Header("Lava Only")]
    [SerializeField] private bool lavaAutoMelt = true;
    [SerializeField] private float lavaFreezeDuration = 3f;
    [SerializeField] private float lavaDamagePerSecond = 0f;
    [SerializeField] private string damageTargetTag = "Player";

    private bool isFrozen;
    private float meltTimer;

    void Awake()
    {
        if (visual != null)
        {
            Color c = unfrozenColor;
            c.a = Mathf.Clamp01(unfrozenAlpha);
            visual.color = c;
        }
        if (detectionTrigger != null) detectionTrigger.isTrigger = true;
        if (solidColliderWhenFrozen != null)
        {
            solidColliderWhenFrozen.enabled = false;
            solidColliderWhenFrozen.isTrigger = false;
        }
        isFrozen = false;
        meltTimer = 0f;
    }

    void Update()
    {
        if (isFrozen && elementType == ElementType.Lava && lavaAutoMelt)
        {
            if (meltTimer > 0f)
            {
                meltTimer -= Time.deltaTime;
                if (meltTimer <= 0f)
                {
                    MeltNow();
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<Projectile>(out var proj)) return;

        if (!isFrozen)
        {
            bool doFreeze = (proj.projectileType == Projectile.ProjectileType.Ice && freezeByIce) ||
                            (proj.projectileType == Projectile.ProjectileType.Fire && freezeByFire);
            if (doFreeze) FreezeNow();
        }
        else
        {
            bool doMelt = (proj.projectileType == Projectile.ProjectileType.Fire && meltByFire) ||
                          (proj.projectileType == Projectile.ProjectileType.Ice && meltByIce);
            if (doMelt) MeltNow();
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (elementType != ElementType.Lava) return;
        if (isFrozen) return;
        if (lavaDamagePerSecond <= 0f) return;
        if (!string.IsNullOrEmpty(damageTargetTag) && !other.CompareTag(damageTargetTag)) return;

        if (other.TryGetComponent<IDamageable>(out var dmg))
        {
            dmg.ApplyDamage(new DamageInfo(lavaDamagePerSecond * Time.deltaTime, DamageType.Normal, gameObject));
        }
    }

    public void ApplyFreeze(float duration)
    {
        if (!isFrozen)
        {
            if ((elementType == ElementType.Water && freezeByIce) || (elementType == ElementType.Lava && freezeByIce))
            {
                FreezeNow();
            }
        }
        else
        {
            if (elementType == ElementType.Lava && lavaAutoMelt)
            {
                meltTimer = Mathf.Max(meltTimer, duration > 0f ? duration : lavaFreezeDuration);
            }
        }
    }

    private void FreezeNow()
    {
        isFrozen = true;
        if (visual != null)
        {
            Color c = frozenColor;
            c.a = Mathf.Clamp01(frozenAlpha);
            visual.color = c;
        }
        if (solidColliderWhenFrozen != null) solidColliderWhenFrozen.enabled = enableSolidWhenFrozen;
        if (elementType == ElementType.Lava && lavaAutoMelt)
        {
            meltTimer = Mathf.Max(0.01f, lavaFreezeDuration);
        }
    }

    private void MeltNow()
    {
        isFrozen = false;
        if (visual != null)
        {
            Color c = unfrozenColor;
            c.a = Mathf.Clamp01(unfrozenAlpha);
            visual.color = c;
        }
        if (solidColliderWhenFrozen != null) solidColliderWhenFrozen.enabled = false;
        meltTimer = 0f;
    }
}
