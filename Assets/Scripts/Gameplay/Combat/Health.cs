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

    private Coroutine fireDotRoutine;
    private Coroutine freezeRoutine;

    void Awake() => Current = maxHP;

    public void ApplyDamage(float amount, GameObject source)
    {
        if (Current <= 0f) return;
        Current -= amount;
        OnChanged?.Invoke(Current, maxHP);
        if (Current <= 0f) OnDied?.Invoke(source);
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
            ApplyDamage(damage, source);
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
