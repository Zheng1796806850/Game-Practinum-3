using UnityEngine;

public class PlayerGunsController : MonoBehaviour
{
    public enum GunMode { Boop, Electric }

    public GunMode mode = GunMode.Boop;
    public KeyCode switchKey = KeyCode.Q;
    public string fireAxis = "Fire1";
    public string altFireAxis = "Fire2";
    public Transform weaponPivot;
    public Transform sharedFirePoint;
    public float firePointDistance = 0.4f;
    public BulletUI bulletUI;

    [Header("Visual Flip")]
    public bool flipSpriteByAim = true;
    public bool invertFlipX = false;
    public SpriteRenderer[] spritesToFlip;

    [Header("Boop")]
    public BoopGun boopGun;
    public float boopBarMax = 1f;
    public float boopRegenPerSecond = 0.75f;
    public float boopShotCost = 0.75f;
    private float boopBarCurrent;

    [Header("Electric")]
    public ElectricGun electricGun;

    private PlayerController playerController;

    void Start()
    {
        if (weaponPivot == null) weaponPivot = transform;
        playerController = GetComponentInParent<PlayerController>();

        if (boopGun != null)
        {
            boopGun.owner = gameObject;
            if (boopGun.firePoint == null && sharedFirePoint != null) boopGun.firePoint = sharedFirePoint;
        }
        if (electricGun != null)
        {
            electricGun.owner = gameObject;
            if (electricGun.firePoint == null && sharedFirePoint != null) electricGun.firePoint = sharedFirePoint;
        }

        if (sharedFirePoint != null && weaponPivot != null && firePointDistance <= 0.001f)
        {
            firePointDistance = Vector2.Distance(sharedFirePoint.position, weaponPivot.position);
            if (firePointDistance < 0.05f) firePointDistance = 0.4f;
        }

        boopBarCurrent = 0f;
        if (bulletUI != null)
        {
            if (mode == GunMode.Boop)
            {
                bulletUI.ShowBoopOnly();
                bulletUI.UpdateBoopBar(boopBarCurrent, boopBarMax);
            }
            else
            {
                bulletUI.ShowElectricOnly();
                bulletUI.UpdateElectricBar01(0f);
            }
            bulletUI.UpdateCooldown01(0f);
        }
    }

    void Update()
    {
        Vector2 aimDir = GetMouseDirFromPivot();
        ApplyPivotAndFirePoint(aimDir);
        ApplySpriteFlip(aimDir);

        if (Input.GetKeyDown(switchKey)) ToggleMode();

        if (mode == GunMode.Boop)
        {
            if (boopGun != null) boopGun.SetAimDirection(aimDir);
            RegenerateBoopBar();

            if (Input.GetButtonDown(fireAxis))
            {
                if (boopBarCurrent + 1e-4f >= boopShotCost && boopGun != null && boopGun.CanStartWindup())
                {
                    boopGun.FireBoop();
                    boopBarCurrent -= boopShotCost;
                    if (boopBarCurrent < 0f) boopBarCurrent = 0f;
                    if (bulletUI != null) bulletUI.UpdateBoopBar(boopBarCurrent, boopBarMax);
                }
            }

            if (!string.IsNullOrEmpty(altFireAxis) && Input.GetButtonDown(altFireAxis))
            {
                if (boopBarCurrent + 1e-4f >= boopShotCost && boopGun != null && boopGun.CanStartWindup())
                {
                    boopGun.FireBoopPull();
                    boopBarCurrent -= boopShotCost;
                    if (boopBarCurrent < 0f) boopBarCurrent = 0f;
                    if (bulletUI != null) bulletUI.UpdateBoopBar(boopBarCurrent, boopBarMax);
                }
            }

            if (bulletUI != null) bulletUI.UpdateCooldown01(boopGun != null ? boopGun.CooldownWheel01 : 0f);
        }
        else
        {
            if (electricGun != null) electricGun.SetAimDirection(aimDir);
            if (Input.GetButtonDown(fireAxis)) if (electricGun != null) electricGun.BeginCharge();
            if (Input.GetButton(fireAxis))
            {
                if (electricGun != null) electricGun.HoldCharge(Time.deltaTime);
                if (bulletUI != null) bulletUI.UpdateElectricBar01(electricGun != null ? electricGun.Charge01 : 0f);
            }
            if (Input.GetButtonUp(fireAxis))
            {
                if (electricGun != null) electricGun.ReleaseAndFire();
                if (bulletUI != null) bulletUI.UpdateElectricBar01(0f);
            }
            if (bulletUI != null) bulletUI.UpdateCooldown01(electricGun != null ? electricGun.CooldownWheel01 : 0f);
        }
    }

    private void RegenerateBoopBar()
    {
        if (boopBarCurrent < boopBarMax)
        {
            boopBarCurrent += boopRegenPerSecond * Time.deltaTime;
            if (boopBarCurrent > boopBarMax) boopBarCurrent = boopBarMax;
            if (bulletUI != null) bulletUI.UpdateBoopBar(boopBarCurrent, boopBarMax);
        }
    }

    private void ToggleMode()
    {
        if (mode == GunMode.Boop)
        {
            mode = GunMode.Electric;
            boopBarCurrent = 0f;
            if (bulletUI != null)
            {
                bulletUI.ShowElectricOnly();
                bulletUI.UpdateBoopBar(boopBarCurrent, boopBarMax);
                bulletUI.UpdateElectricBar01(0f);
                bulletUI.UpdateCooldown01(electricGun != null ? electricGun.CooldownWheel01 : 0f);
            }
        }
        else
        {
            mode = GunMode.Boop;
            if (bulletUI != null)
            {
                bulletUI.ShowBoopOnly();
                bulletUI.UpdateBoopBar(boopBarCurrent, boopBarMax);
                bulletUI.UpdateCooldown01(boopGun != null ? boopGun.CooldownWheel01 : 0f);
            }
        }
    }

    private Vector2 GetMouseDirFromPivot()
    {
        if (Camera.main == null || weaponPivot == null) return Vector2.right;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = weaponPivot.position.z;
        Vector2 dir = (mouseWorld - weaponPivot.position);
        if (dir.sqrMagnitude < 0.0001f) return Vector2.right;
        return dir.normalized;
    }

    private void ApplyPivotAndFirePoint(Vector2 dir)
    {
        if (weaponPivot != null) weaponPivot.right = dir;
        if (sharedFirePoint != null && weaponPivot != null)
        {
            sharedFirePoint.position = weaponPivot.position + (Vector3)(dir * firePointDistance);
            sharedFirePoint.right = dir;
        }
    }

    private void ApplySpriteFlip(Vector2 dir)
    {
        if (!flipSpriteByAim || spritesToFlip == null) return;
        bool flip = dir.x < 0f;
        if (invertFlipX) flip = !flip;
        for (int i = 0; i < spritesToFlip.Length; i++)
        {
            var sr = spritesToFlip[i];
            if (sr != null) sr.flipX = flip;
        }
    }

    public void AddRecoilToPlayer(Vector2 v)
    {
        if (playerController != null) playerController.AddRecoil(v);
    }
}
