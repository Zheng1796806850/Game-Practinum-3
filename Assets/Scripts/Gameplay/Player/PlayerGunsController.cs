using UnityEngine;

public class PlayerGunsController : MonoBehaviour
{
    public enum GunMode { Boop, Electric }

    public GunMode mode = GunMode.Boop;
    public KeyCode switchKey = KeyCode.Q;
    public string fireAxis = "Fire1";
    public Transform weaponPivot;
    public BulletUI bulletUI;

    [Header("Boop")]
    public BoopGun boopGun;
    public float boopBarMax = 1f;
    public float boopRegenPerSecond = 0.75f;
    public float boopShotCost = 0.75f;
    private float boopBarCurrent;

    [Header("Electric")]
    public ElectricGun electricGun;

    void Start()
    {
        if (weaponPivot == null) weaponPivot = transform;
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
        AimGunToMouse();

        if (Input.GetKeyDown(switchKey))
        {
            ToggleMode();
        }

        if (mode == GunMode.Boop)
        {
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
            if (bulletUI != null)
            {
                float v = boopGun != null ? boopGun.CooldownWheel01 : 0f;
                bulletUI.UpdateCooldown01(v);
            }
        }
        else
        {
            if (Input.GetButtonDown(fireAxis))
            {
                if (electricGun != null) electricGun.BeginCharge();
            }
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
            if (bulletUI != null)
            {
                float v = electricGun != null ? electricGun.CooldownWheel01 : 0f;
                bulletUI.UpdateCooldown01(v);
            }
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

    private void AimGunToMouse()
    {
        if (weaponPivot == null) return;
        if (Camera.main == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = weaponPivot.position.z;

        Vector2 dir = (mouseWorld - weaponPivot.position).normalized;
        if (dir.sqrMagnitude < 0.0001f) return;

        if (weaponPivot.lossyScale.x < 0f)
            dir = -dir;

        weaponPivot.right = dir;
    }
}
