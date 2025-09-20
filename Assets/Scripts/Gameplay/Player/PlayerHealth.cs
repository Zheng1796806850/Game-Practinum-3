using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerHealth : MonoBehaviour
{
    private Health health;

    void Awake()
    {
        health = GetComponent<Health>();
        health.OnChanged += OnHealthChanged;
        health.OnDied += OnDied;
    }

    private void OnHealthChanged(int current, int max)
    {
        Debug.Log("Player HP: " + current + "/" + max);
    }

    private void OnDied(GameObject killer)
    {
        Debug.Log("Player Died");
    }
}
