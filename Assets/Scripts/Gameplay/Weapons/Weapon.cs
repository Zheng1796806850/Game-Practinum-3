using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 10f;
    public float damage = 1f;
    public GameObject owner;

    [Header("Fire Cooldown")]
    public float fireCooldown = 0.2f;

    private float _cooldownTimer;

    void Update()
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
    }

    public bool CanFire()
    {
        return _cooldownTimer <= 0f;
    }

    public bool TryFire(Vector2 direction)
    {
        if (!CanFire()) return false;
        if (bulletPrefab == null || firePoint == null) return false;

        var bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        var rb = bullet.GetComponent<Rigidbody2D>();
        var proj = bullet.GetComponent<Projectile>();

        Vector2 vel = direction.normalized * bulletSpeed;
        if (rb != null) rb.linearVelocity = vel;
        if (proj != null) proj.Init(owner: owner, damage: damage, velocity: vel);

        _cooldownTimer = fireCooldown;
        return true;
    }

    public void Fire(Vector2 direction)
    {
        TryFire(direction);
    }
}
