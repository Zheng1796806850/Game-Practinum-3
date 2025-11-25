using UnityEngine;

public class GunPickup : MonoBehaviour
{
    public enum GunType
    {
        Boop,
        Electric
    }

    [Header("Pickup")]
    public GunType gunType = GunType.Boop;
    public string playerTag = "Player";
    public bool destroyOnPickup = true;

    [Header("Pickup FX")]
    public AudioSource pickupAudioSource;
    public AudioClip pickupClip;
    [Range(0f, 1f)] public float pickupVolume = 1f;
    public GameObject pickupVfxPrefab;

    [Header("Destroy Somethings")]
    public GameObject[] objectsToDestroyOnPickup;

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

        if (pickupClip != null)
        {
            if (pickupAudioSource != null)
            {
                pickupAudioSource.PlayOneShot(pickupClip, pickupVolume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(pickupClip, transform.position, pickupVolume);
            }
        }

        if (pickupVfxPrefab != null)
        {
            Instantiate(pickupVfxPrefab, transform.position, Quaternion.identity);
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
            foreach (GameObject obj in objectsToDestroyOnPickup)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
