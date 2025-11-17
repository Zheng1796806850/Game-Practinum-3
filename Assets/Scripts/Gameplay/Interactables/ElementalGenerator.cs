using System;
using UnityEngine;
using UnityEngine.UI;

public class ElementalGenerator : MonoBehaviour, ISwitch
{
    public enum AcceptType { FireOnly, IceOnly, Any }

    [Header("Accept")]
    [SerializeField] private AcceptType accept = AcceptType.IceOnly;

    [Header("Mode")]
    [SerializeField] private bool useReverseMode = false;

    [Header("Charge (%)")]
    [Range(0f, 100f)][SerializeField] private float activationPercent = 100f;
    [Range(0f, 100f)][SerializeField] private float deactivationPercent = 80f;
    [Range(0f, 100f)][SerializeField] private float decayPerSecondPercent = 5f;
    [Range(0f, 100f)][SerializeField] private float perHitChargePercent = 25f;

    [Header("Hit Cooldown")]
    [SerializeField] private float retriggerCooldown = 0.05f;

    [Header("UI")]
    [SerializeField] private Slider generatorBar;
    [SerializeField] private Slider[] lineSliders;

    [Header("Sequence")]
    [SerializeField] private bool useSequentialMode = false;
    [SerializeField] private MonoBehaviour[] prerequisites;
    [SerializeField] private bool drainOnPrerequisiteLost = true;

    public bool IsActivated { get; private set; }
    public float ChargePercent { get; private set; }
    public event Action<bool> OnActivatedChanged;

    private int lastHitFrame = -1;
    private float lastHitTime = -999f;

    void Awake()
    {
        if (generatorBar != null)
        {
            generatorBar.maxValue = 1f;
            generatorBar.value = 0f;
        }
        if (lineSliders != null)
        {
            for (int i = 0; i < lineSliders.Length; i++)
            {
                if (lineSliders[i] != null)
                {
                    lineSliders[i].maxValue = 1f;
                    lineSliders[i].value = 0f;
                }
            }
        }

        ClampThresholds();

        if (useReverseMode)
        {
            ChargePercent = 100f;
        }
        else
        {
            ChargePercent = 0f;
        }

        IsActivated = false;
        EvaluateActivation();
        RefreshUI();
    }

    void Update()
    {
        if (useSequentialMode && !ArePrerequisitesMet())
        {
            ForceDeactivateDueToPrereqLost();
            return;
        }

        if (decayPerSecondPercent > 0f)
        {
            if (!useReverseMode)
            {
                if (ChargePercent > 0f)
                {
                    ChargePercent = Mathf.Max(0f, ChargePercent - decayPerSecondPercent * Time.deltaTime);
                    EvaluateActivation();
                    RefreshUI();
                }
            }
            else
            {
                if (ChargePercent < 100f)
                {
                    ChargePercent = Mathf.Min(100f, ChargePercent + decayPerSecondPercent * Time.deltaTime);
                    EvaluateActivation();
                    RefreshUI();
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<Projectile>(out var proj)) return;
        if (Time.frameCount == lastHitFrame) return;
        if (Time.time - lastHitTime < retriggerCooldown) return;
        if (!Accepts(proj)) return;
        if (useSequentialMode && !ArePrerequisitesMet()) return;

        float add = perHitChargePercent;
        var payload = other.GetComponent<GeneratorChargePayload>();
        if (payload != null && payload.chargePercent >= 0f) add = payload.chargePercent;

        float delta = add;
        if (useReverseMode) delta = -delta;

        ChargePercent = Mathf.Clamp(ChargePercent + delta, 0f, 100f);
        EvaluateActivation();
        RefreshUI();

        lastHitFrame = Time.frameCount;
        lastHitTime = Time.time;
    }

    private bool Accepts(Projectile proj)
    {
        if (accept == AcceptType.Any) return true;
        if (accept == AcceptType.FireOnly) return proj.projectileType == Projectile.ProjectileType.Fire;
        return proj.projectileType == Projectile.ProjectileType.Ice;
    }

    private void EvaluateActivation()
    {
        bool newActive = IsActivated;

        if (!useReverseMode)
        {
            if (IsActivated)
            {
                if (ChargePercent < deactivationPercent) newActive = false;
            }
            else
            {
                if (ChargePercent >= activationPercent) newActive = true;
            }
        }
        else
        {
            if (IsActivated)
            {
                if (ChargePercent > activationPercent) newActive = false;
            }
            else
            {
                if (ChargePercent <= deactivationPercent) newActive = true;
            }
        }

        if (newActive && useSequentialMode && !ArePrerequisitesMet()) newActive = false;
        if (newActive != IsActivated) SetActivated(newActive);
    }

    private void SetActivated(bool v)
    {
        if (IsActivated == v) return;
        IsActivated = v;
        OnActivatedChanged?.Invoke(IsActivated);
    }

    private void ForceDeactivateDueToPrereqLost()
    {
        bool changed = false;
        if (IsActivated)
        {
            IsActivated = false;
            OnActivatedChanged?.Invoke(false);
            changed = true;
        }
        if (drainOnPrerequisiteLost && (ChargePercent > 0f || changed))
        {
            ChargePercent = 0f;
        }
        RefreshUI();
    }

    private void RefreshUI()
    {
        float t = Mathf.Clamp01(ChargePercent / 100f);

        if (generatorBar != null)
            generatorBar.value = t;

        if (lineSliders != null && lineSliders.Length > 0)
        {
            int n = lineSliders.Length;
            float scaled = t * n;
            for (int i = 0; i < n; i++)
            {
                var s = lineSliders[i];
                if (s == null) continue;
                float local = Mathf.Clamp01(scaled - i);
                s.value = local;
            }
        }
    }

    private void OnValidate()
    {
        ClampThresholds();
        RefreshUI();
    }

    private void ClampThresholds()
    {
        activationPercent = Mathf.Clamp(activationPercent, 0f, 100f);
        deactivationPercent = Mathf.Clamp(deactivationPercent, 0f, activationPercent);
        perHitChargePercent = Mathf.Clamp(perHitChargePercent, 0f, 100f);
        decayPerSecondPercent = Mathf.Max(0f, decayPerSecondPercent);
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
}
