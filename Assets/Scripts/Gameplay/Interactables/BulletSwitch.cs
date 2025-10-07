using UnityEngine;

public class BulletSwitch : MonoBehaviour
{
    [SerializeField] private string activateTag = "Fire";
    [SerializeField] private string deactivateTag = "Ice";
    [SerializeField] private SpriteRenderer indicator;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private bool toggleable = false;
    [SerializeField] private float retriggerCooldown = 0.05f;

    [Header("Auto Deactivate")]
    [SerializeField] private bool autoDeactivateAfterCountdown = false;
    [SerializeField] private float deactivateDelay = 3f;

    public bool IsActivated { get; private set; }

    private int lastToggleFrame = -1;
    private float lastToggleTime = -999f;
    private float deactivateTimer = -1f;

    void Awake()
    {
        RefreshColor();
    }

    void Update()
    {
        if (autoDeactivateAfterCountdown && IsActivated && deactivateTimer > 0f)
        {
            deactivateTimer -= Time.deltaTime;
            if (deactivateTimer <= 0f)
            {
                IsActivated = false;
                RefreshColor();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<Projectile>(out _)) return;
        if (Time.frameCount == lastToggleFrame) return;
        if (Time.time - lastToggleTime < retriggerCooldown) return;

        bool matchedActivate = !string.IsNullOrEmpty(activateTag) && other.CompareTag(activateTag);
        bool matchedDeactivate = !string.IsNullOrEmpty(deactivateTag) && other.CompareTag(deactivateTag);

        if (toggleable)
        {
            if (matchedActivate || matchedDeactivate)
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
            if (matchedActivate)
            {
                if (IsActivated) { lastToggleFrame = Time.frameCount; lastToggleTime = Time.time; return; }
                IsActivated = true;

                if (autoDeactivateAfterCountdown)
                    deactivateTimer = deactivateDelay;
            }
            else if (matchedDeactivate)
            {
                if (!IsActivated) { lastToggleFrame = Time.frameCount; lastToggleTime = Time.time; return; }
                IsActivated = false;
                deactivateTimer = -1f;
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

    private void RefreshColor()
    {
        if (indicator != null)
            indicator.color = IsActivated ? activeColor : inactiveColor;
    }
}
