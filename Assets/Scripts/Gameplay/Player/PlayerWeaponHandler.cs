using UnityEngine;

[RequireComponent(typeof(Weapon))]
public class PlayerWeaponHandler : MonoBehaviour
{
    private Weapon weapon;

    [Header("Bullet Prefabs")]
    public GameObject fireBulletPrefab;
    public GameObject iceBulletPrefab;

    [Header("Controls")]
    public KeyCode switchKey = KeyCode.Q;
    public string fireAxis = "Fire1";

    [Header("UI")]
    public BulletUI bulletUI;

    [Header("Aim")]
    public Transform weaponPivot;

    [Header("Fire Ammo (Bar)")]
    public float fireAmmoMax = 5f;
    public float fireRegenPerSecond = 1.5f;
    public float fireShotCost = 1f;

    [Header("Ice Ammo (Discrete)")]
    public int iceAmmoMax = 4;
    public float iceRechargeInterval = 2f;

    private bool useFire = true;
    private float fireAmmoCurrent;
    private int iceAmmoCurrent;
    private float iceRechargeTimer;

    void Start()
    {
        weapon = GetComponent<Weapon>();
        if (weapon.owner == null) weapon.owner = gameObject;

        useFire = true;
        if (fireBulletPrefab != null) weapon.bulletPrefab = fireBulletPrefab;

        fireAmmoCurrent = Mathf.Max(0f, fireAmmoMax);
        iceAmmoCurrent = Mathf.Max(0, iceAmmoMax);
        iceRechargeTimer = 0f;

        if (weaponPivot == null)
        {
            if (weapon != null && weapon.firePoint != null && weapon.firePoint.parent != null)
                weaponPivot = weapon.firePoint.parent;
            else
                weaponPivot = transform;
        }

        if (bulletUI != null)
        {
            bulletUI.UpdateIcon(useFire);
            bulletUI.UpdateFireBar(fireAmmoCurrent, fireAmmoMax);
            bulletUI.UpdateIceDots(iceAmmoCurrent, iceAmmoMax);
        }
    }

    void Update()
    {
        AimGunToMouse();

        RegenerateFireAmmo();
        RegenerateIceAmmo();

        if (Input.GetKeyDown(switchKey))
        {
            ToggleBulletType();
        }

        if (Input.GetButton(fireAxis))
        {
            Vector2 fireDir = GetMouseDirFromFirePoint();
            TryShoot(fireDir);
        }
    }

    private void RegenerateFireAmmo()
    {
        if (fireAmmoCurrent < fireAmmoMax)
        {
            fireAmmoCurrent += fireRegenPerSecond * Time.deltaTime;
            if (fireAmmoCurrent > fireAmmoMax) fireAmmoCurrent = fireAmmoMax;
            if (bulletUI != null) bulletUI.UpdateFireBar(fireAmmoCurrent, fireAmmoMax);
        }
    }

    private void RegenerateIceAmmo()
    {
        if (iceAmmoCurrent >= iceAmmoMax) return;

        iceRechargeTimer += Time.deltaTime;
        while (iceRechargeTimer >= iceRechargeInterval && iceAmmoCurrent < iceAmmoMax)
        {
            iceAmmoCurrent += 1;
            iceRechargeTimer -= iceRechargeInterval;
            if (bulletUI != null) bulletUI.UpdateIceDots(iceAmmoCurrent, iceAmmoMax);
        }
    }

    private void TryShoot(Vector2 fireDir)
    {
        if (weapon == null) return;

        if (useFire)
        {
            if (fireAmmoCurrent + 1e-4f >= fireShotCost && weapon.TryFire(fireDir))
            {
                fireAmmoCurrent -= fireShotCost;
                if (fireAmmoCurrent < 0f) fireAmmoCurrent = 0f;
                if (bulletUI != null) bulletUI.UpdateFireBar(fireAmmoCurrent, fireAmmoMax);
            }
        }
        else
        {
            if (iceAmmoCurrent > 0 && weapon.TryFire(fireDir))
            {
                iceAmmoCurrent -= 1;
                iceRechargeTimer = 0f;
                if (bulletUI != null) bulletUI.UpdateIceDots(iceAmmoCurrent, iceAmmoMax);
            }
        }
    }

    private void ToggleBulletType()
    {
        useFire = !useFire;

        if (useFire) weapon.bulletPrefab = fireBulletPrefab;
        else weapon.bulletPrefab = iceBulletPrefab;

        if (bulletUI != null) bulletUI.UpdateIcon(useFire);
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

    private Vector2 GetMouseDirFromFirePoint()
    {
        if (Camera.main == null || weapon == null || weapon.firePoint == null)
            return Vector2.right;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = weapon.firePoint.position.z;
        Vector2 dir = (mouseWorld - weapon.firePoint.position);
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
    }
}
