using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHP = 1f;
    public float Current { get; private set; }

    public System.Action<float, float> OnChanged;
    public System.Action<GameObject> OnDied;

    public bool IsFrozen { get; private set; } = false;
    public System.Action<bool> OnFreezeStateChanged;

    [Header("Shield")]
    [SerializeField] private bool useShield = false;
    [SerializeField] GameObject shieldBar;
    [SerializeField] private float maxShield = 0f;
    public float ShieldCurrent { get; private set; }
    public System.Action<float, float> OnShieldChanged;
    public System.Action OnShieldDepleted;

    public System.Action<DamageInfo> OnHit;

    private Coroutine fireDotRoutine;
    private Coroutine freezeRoutine;

    void Awake()
    {
        Current = maxHP;
        ShieldCurrent = useShield ? maxShield : 0f;
        if (!useShield) shieldBar.SetActive(false);
    }

    public void ApplyDamage(DamageInfo info)
    {
        OnHit?.Invoke(info);
        if (Current <= 0f) return;

        if (useShield && ShieldCurrent > 0f)
        {
            if (info.type == DamageType.Fire && info.amount > 0f)
            {
                ShieldCurrent = Mathf.Max(0f, ShieldCurrent - info.amount);
                OnShieldChanged?.Invoke(ShieldCurrent, maxShield);
                if (ShieldCurrent <= 0f) OnShieldDepleted?.Invoke();
                if (ShieldCurrent <= 0f && shieldBar.activeSelf) shieldBar.SetActive(false);
            }
            return;
        }

        float amt = Mathf.Max(0f, info.amount);

        if (amt > 0f)
        {
            Current -= amt;
            OnChanged?.Invoke(Current, maxHP);
            if (Current <= 0f) OnDied?.Invoke(info.source);
        }
    }

    public void ApplyDot(float damage, float duration, float interval, GameObject source)
    {
        if (IsFrozen && freezeRoutine != null)
        {
            StopCoroutine(freezeRoutine);
            IsFrozen = false;
            OnFreezeStateChanged?.Invoke(false);
        }

        if (fireDotRoutine != null) StopCoroutine(fireDotRoutine);

        fireDotRoutine = StartCoroutine(DotRoutine(damage, duration, interval, source));
    }

    private IEnumerator DotRoutine(float damage, float duration, float interval, GameObject source)
    {
        float timer = 0f;
        while (timer < duration && Current > 0)
        {
            ApplyDamage(new DamageInfo(damage, DamageType.Fire, source));
            yield return new WaitForSeconds(interval);
            timer += interval;
        }
        fireDotRoutine = null;
    }

    public void ApplyFreeze(float duration)
    {
        if (fireDotRoutine != null)
        {
            StopCoroutine(fireDotRoutine);
            fireDotRoutine = null;
        }

        if (freezeRoutine != null) StopCoroutine(freezeRoutine);

        freezeRoutine = StartCoroutine(FreezeRoutine(duration));
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        IsFrozen = true;
        OnFreezeStateChanged?.Invoke(true);

        yield return new WaitForSeconds(duration);

        IsFrozen = false;
        OnFreezeStateChanged?.Invoke(false);
        freezeRoutine = null;
    }
}
