using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoopGun : MonoBehaviour
{
    public Transform firePoint;
    public GameObject owner;
    public float windupTime = 1f;
    public float cooldown = 1.2f;
    public float sectorRadius = 5f;
    public float sectorAngleDeg = 80f;
    public float damage = 1f;
    public float pushImpulse = 12f;
    public float recoilVelocity = 5f;
    public LayerMask hitMask;
    public ParticleSystem shootParticles;
    public bool allowMultipleHitsPerFire = true;

    public bool spawnGeneratorProxyProjectile = true;
    public GameObject generatorProxyProjectilePrefab;
    public LayerMask generatorMask;
    public float generatorProxySpeed = 20f;

    private bool isWinding;
    private bool isOnCooldown;
    private float cooldownRemain;
    private Vector2 aimDir = Vector2.right;

    public void SetAimDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude > 1e-6f) aimDir = dir.normalized;
    }

    public bool CanStartWindup()
    {
        return !isWinding && !isOnCooldown;
    }

    public float CooldownWheel01
    {
        get
        {
            if (!isOnCooldown || cooldown <= 0f) return 0f;
            return Mathf.Clamp01(cooldownRemain / Mathf.Max(0.0001f, cooldown));
        }
    }

    public void FireBoop()
    {
        if (firePoint == null) return;
        if (!CanStartWindup()) return;
        StartCoroutine(WindupThenBoop());
    }

    private IEnumerator WindupThenBoop()
    {
        isWinding = true;
        float t = 0f;
        while (t < windupTime)
        {
            t += Time.deltaTime;
            yield return null;
        }
        isWinding = false;
        DoBoop();
        StartCoroutine(CooldownTimer());
    }

    private IEnumerator CooldownTimer()
    {
        isOnCooldown = true;
        cooldownRemain = cooldown;
        while (cooldownRemain > 0f)
        {
            cooldownRemain -= Time.deltaTime;
            yield return null;
        }
        cooldownRemain = 0f;
        isOnCooldown = false;
    }

    private void DoBoop()
    {
        if (shootParticles != null && firePoint != null)
        {
            var ps = Instantiate(shootParticles, firePoint.position, Quaternion.identity);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ps.transform.right = aimDir;
            ps.transform.parent = null;
            ps.Play();
        }

        List<Collider2D> results = new List<Collider2D>();
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(hitMask);
        filter.useTriggers = true;
        Collider2D[] temp = new Collider2D[64];
        int count = Physics2D.OverlapCircle(firePoint.position, sectorRadius, filter, temp);
        for (int i = 0; i < count; i++)
        {
            var c = temp[i];
            if (c == null) continue;
            if (IsInsideSector(c.bounds.ClosestPoint(firePoint.position)))
            {
                results.Add(c);
            }
        }

        HashSet<Transform> processed = new HashSet<Transform>();
        for (int i = 0; i < results.Count; i++)
        {
            var col = results[i];
            var root = col.attachedRigidbody ? col.attachedRigidbody.transform : col.transform;
            if (!allowMultipleHitsPerFire)
            {
                if (processed.Contains(root)) continue;
                processed.Add(root);
            }

            Vector2 dir = ((Vector2)root.position - (Vector2)firePoint.position).normalized;

            if (col.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.ApplyDamage(new DamageInfo(damage, DamageType.Normal, owner));
            }

            if (col.attachedRigidbody != null)
            {
                col.attachedRigidbody.AddForce(dir * pushImpulse, ForceMode2D.Impulse);
            }
        }

        if (owner != null)
        {
            var pc = owner.GetComponentInParent<PlayerController>();
            if (pc != null) pc.AddRecoil(-aimDir * recoilVelocity);
        }

        if (spawnGeneratorProxyProjectile && generatorProxyProjectilePrefab != null)
        {
            Collider2D[] gens = Physics2D.OverlapCircleAll(firePoint.position, sectorRadius, generatorMask);
            for (int i = 0; i < gens.Length; i++)
            {
                var g = gens[i];
                if (!IsInsideSector(g.bounds.ClosestPoint(firePoint.position))) continue;
                var go = Instantiate(generatorProxyProjectilePrefab, firePoint.position, Quaternion.identity);
                var rbp = go.GetComponent<Rigidbody2D>();
                var proj = go.GetComponent<Projectile>();
                Vector2 dir = ((Vector2)g.bounds.ClosestPoint(firePoint.position) - (Vector2)firePoint.position).normalized;
                if (rbp != null) rbp.linearVelocity = dir * generatorProxySpeed;
                if (proj != null) proj.Init(owner, 0f, dir * generatorProxySpeed);
            }
        }
    }

    private bool IsInsideSector(Vector2 point)
    {
        Vector2 to = point - (Vector2)firePoint.position;
        if (to.sqrMagnitude < 0.0001f) return true;
        float ang = Vector2.Angle(aimDir, to);
        return ang <= sectorAngleDeg * 0.5f;
    }

    private void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(firePoint.position, 0.08f);
        Vector3 forward = aimDir.sqrMagnitude > 1e-6f ? (Vector3)aimDir.normalized : Vector3.right;
        Gizmos.DrawLine(firePoint.position, firePoint.position + forward * sectorRadius);
        float half = sectorAngleDeg * 0.5f;
        Quaternion q1 = Quaternion.AngleAxis(half, Vector3.forward);
        Quaternion q2 = Quaternion.AngleAxis(-half, Vector3.forward);
        Vector3 dir1 = q1 * forward;
        Vector3 dir2 = q2 * forward;
        Gizmos.DrawLine(firePoint.position, firePoint.position + dir1.normalized * sectorRadius);
        Gizmos.DrawLine(firePoint.position, firePoint.position + dir2.normalized * sectorRadius);
    }
}
