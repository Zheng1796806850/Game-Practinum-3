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

    [Header("Boop Trigger")]
    [SerializeField] private bool allowBoopTrigger = true;

    public bool IsActivated { get; private set; }

    private int lastToggleFrame = -9999;
    private float lastToggleTime = -9999f;
    private float deactivateClock = 0f;

    void Awake()
    {
        RefreshColor();
    }

    void Update()
    {
        if (autoDeactivateAfterCountdown && IsActivated)
        {
            if (deactivateClock > 0f)
            {
                deactivateClock -= Time.deltaTime;
                if (deactivateClock <= 0f)
                {
                    SetActivated(false);
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || other.gameObject == null) return;
        string t = other.tag;

        if (!string.IsNullOrEmpty(activateTag) && t == activateTag)
        {
            TryToggleOrActivate(true);
        }
        else if (!string.IsNullOrEmpty(deactivateTag) && t == deactivateTag)
        {
            TryToggleOrActivate(false);
        }
    }

    public void ActivateByBoop()
    {
        if (!allowBoopTrigger) return;
        TryToggleOrActivate(true);
    }

    public void DeactivateExtern()
    {
        SetActivated(false);
    }

    public void ActivateExtern()
    {
        SetActivated(true);
    }

    private void TryToggleOrActivate(bool isActivate)
    {
        if (Time.frameCount == lastToggleFrame) return;
        if (Time.time - lastToggleTime < retriggerCooldown) return;

        if (toggleable)
        {
            SetActivated(!IsActivated);
        }
        else
        {
            if (isActivate) SetActivated(true);
            else SetActivated(false);
        }
    }

    private void SetActivated(bool v)
    {
        if (IsActivated == v)
        {
            lastToggleFrame = Time.frameCount;
            lastToggleTime = Time.time;
            if (IsActivated && autoDeactivateAfterCountdown) deactivateClock = deactivateDelay;
            RefreshColor();
            return;
        }

        IsActivated = v;

        if (IsActivated && autoDeactivateAfterCountdown) deactivateClock = deactivateDelay;

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
