using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyFreezeHandler : MonoBehaviour
{
    public Rigidbody2D rb;
    public EnemyCombatController combat;
    public float massMultiplier = 5f;

    private Health health;
    private float baseMass;
    private bool cachedUseTouchDamage;

    void Awake()
    {
        health = GetComponent<Health>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (combat == null) combat = GetComponent<EnemyCombatController>();
        baseMass = rb != null ? rb.mass : 1f;
        if (combat != null) cachedUseTouchDamage = combat.useTouchDamage;
        health.OnFreezeStateChanged += OnFreezeChanged;
    }

    void OnDestroy()
    {
        if (health != null) health.OnFreezeStateChanged -= OnFreezeChanged;
    }

    private void OnFreezeChanged(bool frozen)
    {
        if (rb != null) rb.mass = frozen ? baseMass * massMultiplier : baseMass;
        if (combat != null)
        {
            if (frozen)
            {
                cachedUseTouchDamage = combat.useTouchDamage;
                combat.useTouchDamage = false;
            }
            else
            {
                combat.useTouchDamage = cachedUseTouchDamage;
            }
        }
    }
}
