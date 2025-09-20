using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Slider slider;
    [SerializeField] private Canvas canvas;
    [SerializeField] private bool alwaysVisible = false;

    void Awake()
    {
        if (health == null) health = GetComponentInParent<Health>();
        if (health != null)
        {
            health.OnChanged += OnHealthChanged;
            health.OnDied += OnDied;
        }
    }

    void Start()
    {
        if (canvas != null)
        {
            if (alwaysVisible)
            {
                canvas.enabled = true;
                if (slider != null)
                {
                    slider.maxValue = 1f;
                    slider.value = 1f;
                }
            }
            else
            {
                canvas.enabled = false;
            }
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        if (slider != null)
        {
            slider.maxValue = max;
            slider.value = current;
        }

        if (canvas != null)
        {
            if (alwaysVisible) canvas.enabled = true;
            else canvas.enabled = current < max;
        }
    }

    private void OnDied(GameObject killer)
    {
        if (canvas != null) canvas.enabled = false;
    }
}
