using UnityEngine;

public class ElementalGenerator : MonoBehaviour
{
    public enum RequiredElement { Fire, Ice }
    [SerializeField] private RequiredElement required = RequiredElement.Ice;
    [SerializeField] private bool toggleable = false;
    [SerializeField] private bool allowDeactivateByOpposite = false;
    [SerializeField] private SpriteRenderer indicator;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.cyan;
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
        if (!other.TryGetComponent<Projectile>(out var proj)) return;
        if (Time.frameCount == lastToggleFrame) return;
        if (Time.time - lastToggleTime < retriggerCooldown) return;

        bool match = (required == RequiredElement.Fire && proj.projectileType == Projectile.ProjectileType.Fire) ||
                     (required == RequiredElement.Ice && proj.projectileType == Projectile.ProjectileType.Ice);

        if (toggleable)
        {
            if (match)
            {
                IsActivated = !IsActivated;
            }
            else if (allowDeactivateByOpposite && IsOpposite(proj))
            {
                IsActivated = !IsActivated;
            }
            else
            {
                return;
            }
        }
        else
        {
            if (match)
            {
                if (IsActivated) { lastToggleFrame = Time.frameCount; lastToggleTime = Time.time; return; }
                IsActivated = true;
            }
            else if (allowDeactivateByOpposite && IsOpposite(proj))
            {
                if (!IsActivated) { lastToggleFrame = Time.frameCount; lastToggleTime = Time.time; return; }
                IsActivated = false;
            }
            else
            {
                return;
            }
        }

        lastToggleFrame = Time.frameCount;
        lastToggleTime = Time.time;
        RefreshColor();
    }

    private bool IsOpposite(Projectile proj)
    {
        if (required == RequiredElement.Fire) return proj.projectileType == Projectile.ProjectileType.Ice;
        return proj.projectileType == Projectile.ProjectileType.Fire;
    }

    private void RefreshColor()
    {
        if (indicator != null)
            indicator.color = IsActivated ? activeColor : inactiveColor;
    }
}
