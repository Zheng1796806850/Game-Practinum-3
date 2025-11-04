using System;
using UnityEngine;

public class BulletSwitch : MonoBehaviour, ISwitch
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

    [Header("Sequence")]
    [SerializeField] private bool useSequentialMode = false;
    [SerializeField] private MonoBehaviour[] prerequisites;

    public bool IsActivated { get; private set; }
    public event Action<bool> OnActivatedChanged;

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

        if (useSequentialMode && IsActivated && !ArePrerequisitesMet())
        {
            SetActivated(false);
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
        TryToggleOrActivate(true);
    }

    private void TryToggleOrActivate(bool isActivate)
    {
        if (Time.frameCount == lastToggleFrame) return;
        if (Time.time - lastToggleTime < retriggerCooldown) return;

        if (toggleable)
        {
            if (!useSequentialMode || ArePrerequisitesMet())
                SetActivated(!IsActivated);
            else
                TouchRetriggerClocks();
        }
        else
        {
            if (isActivate)
            {
                if (!useSequentialMode || ArePrerequisitesMet())
                    SetActivated(true);
                else
                    TouchRetriggerClocks();
            }
            else
            {
                SetActivated(false);
            }
        }
    }

    private void SetActivated(bool v)
    {
        if (IsActivated == v)
        {
            TouchRetriggerClocks();
            if (IsActivated && autoDeactivateAfterCountdown) deactivateClock = deactivateDelay;
            RefreshColor();
            return;
        }

        if (v && useSequentialMode && !ArePrerequisitesMet())
        {
            TouchRetriggerClocks();
            RefreshColor();
            return;
        }

        IsActivated = v;
        if (IsActivated && autoDeactivateAfterCountdown) deactivateClock = deactivateDelay;
        TouchRetriggerClocks();
        RefreshColor();
        OnActivatedChanged?.Invoke(IsActivated);
    }

    private void TouchRetriggerClocks()
    {
        lastToggleFrame = Time.frameCount;
        lastToggleTime = Time.time;
    }

    private bool ArePrerequisitesMet()
    {
        if (prerequisites == null || prerequisites.Length == 0) return true;
        for (int i = 0; i < prerequisites.Length; i++)
        {
            var mb = prerequisites[i];
            if (mb == null) return false;
            if (mb is ISwitch sw)
            {
                if (!sw.IsActivated) return false;
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    private void RefreshColor()
    {
        if (indicator != null)
            indicator.color = IsActivated ? activeColor : inactiveColor;
    }
}
