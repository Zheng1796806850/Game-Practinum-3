using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHP = 1;
    public int Current { get; private set; }

    public System.Action<int, int> OnChanged;
    public System.Action<GameObject> OnDied;

    void Awake() => Current = maxHP;

    public void ApplyDamage(int amount, GameObject source)
    {
        if (Current <= 0) return;
        Current -= amount;
        OnChanged?.Invoke(Current, maxHP);
        if (Current <= 0) OnDied?.Invoke(source);
    }
}
