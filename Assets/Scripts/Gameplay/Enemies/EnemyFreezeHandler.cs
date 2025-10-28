using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyFreezeHandler : MonoBehaviour
{
    public Rigidbody2D rb;
    public EnemyCombatController combat;
    public float massMultiplier = 5f;
    [SerializeField] private GameObject iceVisual;
    public bool useFrozenVisuals = false;
    public bool keepTouchDamageWhenFrozen = false;

    private Health health;
    private float baseMass;
    private bool cachedUseTouchDamage;
    private bool inited;

    void Awake()
    {
        health = GetComponent<Health>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (combat != null) cachedUseTouchDamage = combat.useTouchDamage;
        if (rb != null) baseMass = rb.mass;
        health.OnFreezeStateChanged += OnFreezeChanged;
        inited = true;
    }

    void OnEnable()
    {
        if (!inited)
        {
            health = GetComponent<Health>();
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (combat != null) cachedUseTouchDamage = combat.useTouchDamage;
            if (rb != null) baseMass = rb.mass;
            health.OnFreezeStateChanged += OnFreezeChanged;
            inited = true;
        }
    }

    void OnDestroy()
    {
        if (health != null)
        {
            health.OnFreezeStateChanged -= OnFreezeChanged;
        }
    }

    private void OnFreezeChanged(bool frozen)
    {
        if (rb != null)
        {
            rb.mass = frozen ? baseMass * Mathf.Max(1f, massMultiplier) : baseMass;
            if (!frozen) rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.25f, rb.linearVelocity.y);
        }

        if (combat != null)
        {
            if (frozen)
            {
                if (!keepTouchDamageWhenFrozen)
                {
                    cachedUseTouchDamage = combat.useTouchDamage;
                    combat.useTouchDamage = false;
                }
            }
            else
            {
                if (!keepTouchDamageWhenFrozen)
                {
                    combat.useTouchDamage = cachedUseTouchDamage;
                }
            }
        }

        if (iceVisual != null)
        {
            if (useFrozenVisuals) iceVisual.SetActive(frozen);
            else iceVisual.SetActive(false);
        }
    }
}
