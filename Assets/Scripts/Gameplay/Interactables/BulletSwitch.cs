using UnityEngine;

public class BulletSwitch : MonoBehaviour
{
    [SerializeField] private string requiredTag = "Fire";
    [SerializeField] private SpriteRenderer indicator;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private bool toggleable = false;
    [SerializeField] private float retriggerCooldown = 0.05f;

    public bool IsActivated { get; private set; }

    private int lastToggleFrame = -1;
    private float lastToggleTime = -999f;

    void Awake()
    {
        RefreshColor();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<Projectile>(out _)) return;
        if (!other.CompareTag(requiredTag)) return;
        if (Time.frameCount == lastToggleFrame) return;
        if (Time.time - lastToggleTime < retriggerCooldown) return;

        if (toggleable)
        {
            IsActivated = !IsActivated;
        }
        else
        {
            if (IsActivated) return;
            IsActivated = true;
        }

        lastToggleFrame = Time.frameCount;
        lastToggleTime = Time.time;
        RefreshColor();
    }

    private void RefreshColor()
    {
        if (indicator != null)
            indicator.color = IsActivated ? activeColor : inactiveColor;
    }
}
