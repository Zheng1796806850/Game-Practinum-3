using System.Collections;
using UnityEngine;

public class ElectricGun : MonoBehaviour
{
    public Weapon weapon;
    public Transform firePoint;
    public GameObject owner;
    public float maxChargeTime = 1.5f;
    public float cooldown = 2f;
    public float baseFreezeDuration = 3f;
    [Range(0f, 1f)] public float tierThreshold01 = 0.5f;
    public float highTierDamageMultiplier = 1.5f;
    public ParticleSystem chargeParticlesPrefab;
    public ParticleSystem releaseParticlesPrefab;

    [Header("Audio")]
    public AudioClip chargeLoopClip;
    public AudioClip fireClip;
    [Range(0f, 1f)] public float chargeVolume = 1f;
    [Range(0f, 1f)] public float fireVolume = 1f;

    [Header("Switch Filter")]
    public bool useSwitchTagFilter = true;
    public string switchTag = "ElectricSwitch";

    private bool isCharging;
    private float chargeT;
    private bool isOnCooldown;
    private float cooldownRemain;
    private ParticleSystem chargeInstance;
    private Vector2 aimDir = Vector2.right;
    private AudioSource chargeAudio;
    private AudioSource fireAudio;

    void Awake()
    {
        if (chargeAudio == null)
        {
            chargeAudio = gameObject.AddComponent<AudioSource>();
            chargeAudio.playOnAwake = false;
            chargeAudio.loop = true;
            chargeAudio.spatialBlend = 0f;
        }
        if (fireAudio == null)
        {
            fireAudio = gameObject.AddComponent<AudioSource>();
            fireAudio.playOnAwake = false;
            fireAudio.loop = false;
            fireAudio.spatialBlend = 0f;
        }
    }

    void OnDisable()
    {
        if (chargeAudio != null && chargeAudio.isPlaying) chargeAudio.Stop();
    }

    public float Charge01
    {
        get { return Mathf.Clamp01(chargeT / Mathf.Max(0.0001f, maxChargeTime)); }
    }

    public float CooldownWheel01
    {
        get
        {
            if (!isOnCooldown || cooldown <= 0f) return 0f;
            return Mathf.Clamp01(cooldownRemain / Mathf.Max(0.0001f, cooldown));
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

        if (chargeLoopClip != null && chargeAudio != null)
        {
            chargeAudio.clip = chargeLoopClip;
            chargeAudio.volume = chargeVolume;
            chargeAudio.loop = true;
            chargeAudio.Play();
        }

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
        if (chargeT >= maxChargeTime)
        {
            chargeT = maxChargeTime;
            ReleaseAndFire();
            return;
        }
        if (chargeInstance != null && firePoint != null)
        {
            chargeInstance.transform.position = firePoint.position;
            chargeInstance.transform.right = aimDir;
        }
    }

    public void ReleaseAndFire()
    {
        if (!isCharging) return;
        float charge01 = Mathf.Clamp01(chargeT / Mathf.Max(0.0001f, maxChargeTime));

        if (chargeAudio != null && chargeAudio.isPlaying) chargeAudio.Stop();

        if (chargeInstance != null)
        {
            Destroy(chargeInstance.gameObject);
            chargeInstance = null;
        }

        FireShot(charge01);

        if (releaseParticlesPrefab != null && firePoint != null)
        {
            var ps = Instantiate(releaseParticlesPrefab, firePoint.position, Quaternion.identity);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ps.transform.right = aimDir;
            ps.transform.parent = null;
            ps.Play();
        }

        if (fireClip != null && fireAudio != null)
        {
            fireAudio.volume = fireVolume;
            fireAudio.PlayOneShot(fireClip, fireVolume);
        }

        isCharging = false;
        chargeT = 0f;
        StartCoroutine(CooldownTimer());
    }

    private void FireShot(float charge01)
    {
        if (weapon == null) return;

        float baseDamage = weapon != null ? weapon.damage : 1f;
        bool hasCharge = charge01 > 0f;
        bool highTier = hasCharge && charge01 >= tierThreshold01;

        float damageToUse;
        float freezeDuration;

        if (!hasCharge)
        {
            damageToUse = baseDamage;
            freezeDuration = 0f;
        }
        else
        {
            if (!highTier)
            {
                damageToUse = baseDamage;
                freezeDuration = Mathf.Max(0f, baseFreezeDuration * charge01);
            }
            else
            {
                damageToUse = baseDamage * highTierDamageMultiplier;
                freezeDuration = 0f;
            }
        }

        if (weapon.TryFireReturnProjectile(aimDir, damageToUse, out var proj, out var go))
        {
            if (proj != null)
            {
                proj.projectileType = hasCharge ? Projectile.ProjectileType.Ice : Projectile.ProjectileType.Normal;
                proj.iceFreezeDuration = freezeDuration;

                proj.clearFreezeOnHit = hasCharge && highTier;
                proj.releaseCapturedEnemyOnHit = hasCharge && highTier;

                proj.useSwitchTagFilter = useSwitchTagFilter;
                proj.switchTag = switchTag;

                bool activateMode = !highTier;

                proj.activateSwitchOnHit = activateMode;
                proj.deactivateSwitchOnHit = !activateMode;

                var payload = go.GetComponent<GeneratorChargePayload>();
                if (payload == null) payload = go.AddComponent<GeneratorChargePayload>();
                payload.chargePercent = -1f;
                payload.chargeSign = activateMode ? 1 : -1;
            }
        }
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
