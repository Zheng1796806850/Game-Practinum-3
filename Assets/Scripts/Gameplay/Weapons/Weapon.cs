using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 10f;
    public int damage = 1;

    public void Fire(Vector2 direction)
    {
        if (bulletPrefab == null || firePoint == null) return;

        var bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        var rb = bullet.GetComponent<Rigidbody2D>();
        var proj = bullet.GetComponent<Projectile>();

        Vector2 vel = direction.normalized * bulletSpeed;
        if (rb != null) rb.linearVelocity = vel;
        if (proj != null) proj.Init(owner: gameObject, damage: damage, velocity: vel);
    }
}
