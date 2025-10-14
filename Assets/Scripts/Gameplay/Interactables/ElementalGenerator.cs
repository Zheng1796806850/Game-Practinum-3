using UnityEngine;
using UnityEngine.UI;

public class ElementalGenerator : MonoBehaviour
{
    public enum AcceptType { FireOnly, IceOnly, Any }

    [Header("Accept")]
    [SerializeField] private AcceptType accept = AcceptType.IceOnly;

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

    public bool IsActivated { get; private set; }
    public float ChargePercent { get; private set; }

    private int lastHitFrame = -1;
    private float lastHitTime = -999f;

    void Awake()
    {
        if (generatorBar != null) { generatorBar.maxValue = 1f; generatorBar.value = 0f; }
        if (lineSliders != null)
        {
            for (int i = 0; i < lineSliders.Length; i++)
            {
                if (lineSliders[i] != null) { lineSliders[i].maxValue = 1f; lineSliders[i].value = 0f; }
            }
        }
        ChargePercent = 0f;
        IsActivated = false;
        ClampThresholds();
        RefreshUI();
    }

    void Update()
    {
        if (ChargePercent > 0f && decayPerSecondPercent > 0f)
        {
            ChargePercent = Mathf.Max(0f, ChargePercent - decayPerSecondPercent * Time.deltaTime);
            EvaluateActivation();
            RefreshUI();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<Projectile>(out var proj)) return;
        if (Time.frameCount == lastHitFrame) return;
        if (Time.time - lastHitTime < retriggerCooldown) return;
        if (!Accepts(proj)) return;

        ChargePercent = Mathf.Min(100f, ChargePercent + perHitChargePercent);
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
        if (IsActivated)
        {
            if (ChargePercent < deactivationPercent) IsActivated = false;
        }
        else
        {
            if (ChargePercent >= activationPercent) IsActivated = true;
        }
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
}
