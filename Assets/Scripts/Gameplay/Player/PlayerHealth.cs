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

    private void OnHealthChanged(float current, float max)
    {
        Debug.Log("Player HP: " + current.ToString("F1") + "/" + max.ToString("F1"));
    }

    private void OnDied(GameObject killer)
    {
        Debug.Log("Player Died");
    }
}
