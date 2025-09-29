using UnityEngine;

public enum DamageType
{
    Normal,
    Fire,
    Ice
}

public struct DamageInfo
{
    public float amount;
    public DamageType type;
    public GameObject source;

    public DamageInfo(float amount, DamageType type, GameObject source)
    {
        this.amount = amount;
        this.type = type;
        this.source = source;
    }
}

public interface IDamageable
{
    void ApplyDamage(DamageInfo info);
}
