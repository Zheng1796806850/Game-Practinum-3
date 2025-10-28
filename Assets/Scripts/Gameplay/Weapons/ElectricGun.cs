using System.Collections;
using UnityEngine;

public class ElectricGun : MonoBehaviour
{
    public Weapon weapon;
    public Transform firePoint;
    public GameObject owner;
    public float maxChargeTime = 5f;
    public float cooldown = 2f;
    public float fullFreezeDuration = 4f;
    public float halfFreezeDuration = 2f;
    public float bonusDamageIfFrozen = 0f;
    public float generatorFullChargePercent = 100f;
    public float generatorHalfChargePercent = 50f;
    public ParticleSystem chargeParticlesPrefab;
    public ParticleSystem releaseParticlesPrefab;

    private bool isCharging;
    private float chargeT;
    private bool isOnCooldown;
    private float cooldownRemain;
    private ParticleSystem chargeInstance;

    public void BeginCharge()
    {
        if (isOnCooldown) return;
        if (isCharging) return;
        if (weapon == null || firePoint == null || owner == null) return;
        isCharging = true;
        chargeT = 0f;
        if (chargeParticlesPrefab != null)
        {
            chargeInstance = Instantiate(chargeParticlesPrefab, firePoint.position, firePoint.rotation, firePoint);
            var main = chargeInstance.main;
            main.loop = true;
            chargeInstance.Play();
        }
    }

    public void HoldCharge(float dt)
    {
        if (!isCharging) return;
        chargeT += dt;
        if (chargeT > maxChargeTime) chargeT = maxChargeTime;
        if (chargeInstance != null)
        {
            chargeInstance.transform.position = firePoint.position;
            chargeInstance.transform.rotation = firePoint.rotation;
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

        if (k <= 0.20f)
        {
            isCharging = false;
            chargeT = 0f;
            StartCoroutine(CooldownTimer());
            return;
        }

        bool halfEffect = k < 0.81f;

        if (weapon != null)
        {
            if (weapon.TryFireReturnProjectile(firePoint.right, out var proj, out var go))
            {
                if (proj != null)
                {
                    proj.projectileType = Projectile.ProjectileType.Ice;
                    proj.iceFreezeDuration = halfEffect ? halfFreezeDuration : fullFreezeDuration;
                    proj.bonusDamageIfTargetFrozen = bonusDamageIfFrozen;
                    var payload = go.GetComponent<GeneratorChargePayload>();
                    if (payload == null) payload = go.AddComponent<GeneratorChargePayload>();
                    payload.chargePercent = halfEffect ? generatorHalfChargePercent : generatorFullChargePercent;
                }
            }
        }

        if (releaseParticlesPrefab != null)
        {
            var ps = Instantiate(releaseParticlesPrefab, firePoint.position, firePoint.rotation);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ps.transform.right = firePoint.right;
            ps.transform.parent = null;
            ps.Play();
        }

        isCharging = false;
        chargeT = 0f;
        StartCoroutine(CooldownTimer());
    }

    public float Charge01
    {
        get
        {
            return Mathf.Clamp01(chargeT / Mathf.Max(0.0001f, maxChargeTime));
        }
    }

    public float CooldownWheel01
    {
        get
        {
            if (!isOnCooldown || cooldown <= 0f) return 0f;
            return Mathf.Clamp01(cooldownRemain / Mathf.Max(0.0001f, cooldown));
        }
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
}
