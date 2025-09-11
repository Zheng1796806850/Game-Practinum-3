using UnityEngine;

[RequireComponent(typeof(Weapon))]
public class EnemyWeaponHandler : MonoBehaviour
{
    private Weapon weapon;

    /*
    void Start()
    {
        weapon = GetComponent<Weapon>();
        InvokeRepeating(nameof(AutoShoot), 1f, 2f);
    }

    void AutoShoot()
    {
        Vector2 direction = Vector2.left;
        weapon.Fire(direction);
    }
    */
}
