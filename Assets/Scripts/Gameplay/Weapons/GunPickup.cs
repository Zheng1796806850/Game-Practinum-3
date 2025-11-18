using UnityEngine;

public class GunPickup : MonoBehaviour
{
    public enum GunType
    {
        Boop,
        Electric
    }

    public GunType gunType = GunType.Boop;
    public string playerTag = "Player";
    public bool destroyOnPickup = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        PlayerGunsController guns = other.GetComponent<PlayerGunsController>();
        if (guns == null) guns = other.GetComponentInChildren<PlayerGunsController>();
        if (guns == null) guns = other.GetComponentInParent<PlayerGunsController>();
        if (guns == null) return;

        if (gunType == GunType.Boop)
        {
            guns.GiveBoopGun();
        }
        else
        {
            guns.GiveElectricGun();
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
