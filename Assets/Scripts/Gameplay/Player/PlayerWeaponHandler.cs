using UnityEngine;

[RequireComponent(typeof(Weapon))]
public class PlayerWeaponHandler : MonoBehaviour
{
    private Weapon weapon;
    private PlayerController playerController;

    [Header("Bullet Prefabs")]
    public GameObject fireBulletPrefab;
    public GameObject iceBulletPrefab;

    [Header("Controls")]
    public KeyCode switchKey = KeyCode.Q;

    [Header("UI")]
    public BulletUI bulletUI;

    private bool useFire = true;

    void Start()
    {
        weapon = GetComponent<Weapon>();
        playerController = GetComponent<PlayerController>();

        if (fireBulletPrefab != null)
            weapon.bulletPrefab = fireBulletPrefab;

        if (bulletUI != null)
            bulletUI.UpdateIcon(useFire);
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            Vector2 direction = playerController.IsFacingRight() ? Vector2.right : Vector2.left;
            weapon.Fire(direction);
        }

        if (Input.GetKeyDown(switchKey))
        {
            ToggleBulletType();
        }
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
