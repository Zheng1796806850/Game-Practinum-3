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

    [Header("UI")]
    public BulletUI bulletUI;

    [Header("Aim")]
    public Transform weaponPivot;

    private bool useFire = true;

    void Start()
    {
        weapon = GetComponent<Weapon>();

        if (weapon.owner == null) weapon.owner = gameObject;

        if (fireBulletPrefab != null)
            weapon.bulletPrefab = fireBulletPrefab;

        if (bulletUI != null)
            bulletUI.UpdateIcon(useFire);

        if (weaponPivot == null)
        {
            if (weapon != null && weapon.firePoint != null && weapon.firePoint.parent != null)
                weaponPivot = weapon.firePoint.parent;
            else
                weaponPivot = transform;
        }
    }

    void Update()
    {
        AimGunToMouse();

        if (Input.GetButtonDown("Fire1"))
        {
            Vector2 fireDir = GetMouseDirFromFirePoint();
            weapon.Fire(fireDir);
        }

        if (Input.GetKeyDown(switchKey))
        {
            ToggleBulletType();
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

    private Vector2 GetMouseDirFromFirePoint()
    {
        if (Camera.main == null || weapon == null || weapon.firePoint == null)
            return Vector2.right;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = weapon.firePoint.position.z;
        Vector2 dir = (mouseWorld - weapon.firePoint.position);
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
    }

    private void ToggleBulletType()
    {
        useFire = !useFire;

        if (useFire)
        {
            weapon.bulletPrefab = fireBulletPrefab;
        }
        else
        {
            weapon.bulletPrefab = iceBulletPrefab;
        }

        if (bulletUI != null)
            bulletUI.UpdateIcon(useFire);
    }
}
