using UnityEngine;
using UnityEngine.UI;

public class ShieldBar : MonoBehaviour
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
            health.OnShieldChanged += OnShieldChanged;
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

        if (health != null && slider != null)
        {
            slider.maxValue = Mathf.Max(1f, health.ShieldCurrent > 0f ? health.ShieldCurrent : 1f);
            slider.value = health.ShieldCurrent;
            if (canvas != null)
            {
                if (alwaysVisible) canvas.enabled = true;
                else canvas.enabled = health.ShieldCurrent < slider.maxValue && health.ShieldCurrent > 0f;
            }
        }
    }

    private void OnShieldChanged(float current, float max)
    {
        if (slider != null)
        {
            slider.maxValue = max <= 0f ? 1f : max;
            slider.value = current;
        }

        if (canvas != null)
        {
            if (alwaysVisible) canvas.enabled = true;
            else canvas.enabled = current > 0f && current < (max <= 0f ? 1f : max);
        }
    }

    private void OnDied(GameObject killer)
    {
        if (canvas != null) canvas.enabled = false;
    }
}
