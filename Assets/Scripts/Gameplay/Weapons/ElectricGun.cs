using System.Collections;
using UnityEngine;

public class ElectricGun : MonoBehaviour
{
    public Weapon weapon;
    public Transform firePoint;
    public GameObject owner;
    public float maxChargeTime = 5f;
    public float cooldown = 2f;
    public float baseFreezeDuration = 3f;
    public float minEffectiveCharge01 = 0.2f;
    public float highTierThreshold01 = 0.8f;
    public float highTierDamageMultiplier = 1.5f;
    public ParticleSystem chargeParticlesPrefab;
    public ParticleSystem releaseParticlesPrefab;

    private bool isCharging;
    private float chargeT;
    private bool isOnCooldown;
    private float cooldownRemain;
    private ParticleSystem chargeInstance;
    private Vector2 aimDir = Vector2.right;

    public float Charge01
    {
        get { return Mathf.Clamp01(chargeT / Mathf.Max(0.0001f, maxChargeTime)); }
    }

    public float CooldownWheel01
    {
        get
        {
            if (!isOnCooldown || cooldown <= 0f) return 0f;
            return Mathf.Clamp01(cooldownRemain / cooldown);
        }
    }

    public void SetAimDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude > 1e-6f) aimDir = dir.normalized;
        if (chargeInstance != null)
        {
            chargeInstance.transform.position = firePoint != null ? firePoint.position : transform.position;
            chargeInstance.transform.right = aimDir;
        }
    }

    public void BeginCharge()
    {
        if (isOnCooldown) return;
        if (weapon == null || firePoint == null) return;
        if (isCharging) return;
        isCharging = true;
        chargeT = 0f;
        if (chargeParticlesPrefab != null)
        {
            chargeInstance = Instantiate(chargeParticlesPrefab, firePoint.position, Quaternion.identity);
            var main = chargeInstance.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            chargeInstance.transform.right = aimDir;
            chargeInstance.transform.parent = null;
            chargeInstance.Play();
        }
    }

    public void HoldCharge(float dt)
    {
        if (!isCharging) return;
        chargeT += Mathf.Max(0f, dt);
        if (chargeT > maxChargeTime) chargeT = maxChargeTime;
        if (chargeInstance != null && firePoint != null)
        {
            chargeInstance.transform.position = firePoint.position;
            chargeInstance.transform.right = aimDir;
        }
    }

    public void ReleaseAndFire()
    {
        if (!isCharging) return;
        float k = Mathf.Clamp01(chargeT / Mathf.Max(0.0001f, maxChargeTime));
        if (chargeInstance != null)
        {
            Destroy(chargeInstance.gameObject);
            chargeInstance = null;
        }

        if (k <= minEffectiveCharge01)
        {
            isCharging = false;
            chargeT = 0f;
            StartCoroutine(CooldownTimer());
            return;
        }

        bool highTier = k >= highTierThreshold01;
        float baseDamage = weapon != null ? weapon.damage : 1f;
        float damageToUse = highTier ? baseDamage * highTierDamageMultiplier : baseDamage * k;
        float payloadPercent = highTier ? 100f : Mathf.Clamp01(k) * 100f;
        float freezeDuration = Mathf.Max(0f, baseFreezeDuration * Mathf.Clamp01(k));

        if (weapon != null)
        {
            if (weapon.TryFireReturnProjectile(aimDir, damageToUse, out var proj, out var go))
            {
                if (proj != null)
                {
                    proj.projectileType = Projectile.ProjectileType.Ice;
                    proj.iceFreezeDuration = freezeDuration;
                    var payload = go.GetComponent<GeneratorChargePayload>();
                    if (payload == null) payload = go.AddComponent<GeneratorChargePayload>();
                    payload.chargePercent = payloadPercent;
                }
            }
        }

        if (releaseParticlesPrefab != null && firePoint != null)
        {
            var ps = Instantiate(releaseParticlesPrefab, firePoint.position, Quaternion.identity);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ps.transform.right = aimDir;
            ps.transform.parent = null;
            ps.Play();
        }

        isCharging = false;
        chargeT = 0f;
        StartCoroutine(CooldownTimer());
    }

    private IEnumerator CooldownTimer()
    {
        if (cooldown <= 0f)
        {
            isOnCooldown = false;
            cooldownRemain = 0f;
            yield break;
        }
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

    private void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(firePoint.position, 0.08f);
        Vector3 forward = aimDir.sqrMagnitude > 1e-6f ? (Vector3)aimDir.normalized : Vector3.right;
        Gizmos.DrawLine(firePoint.position, firePoint.position + forward * 3f);
    }
}
