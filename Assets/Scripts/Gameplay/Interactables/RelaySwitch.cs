using System;
using UnityEngine;
using UnityEngine.UI;

public class RelaySwitch : MonoBehaviour, ISwitch
{
    [Serializable]
    public class InputEntry
    {
        public MonoBehaviour source;
        public GameObject activeVisual;
    }

    public enum RelayMode
    {
        AllOn,
        AnyOn
    }

    [Header("Logic")]
    [SerializeField] private RelayMode mode = RelayMode.AllOn;
    [SerializeField] private InputEntry[] inputs;
    [SerializeField] private bool requireAtLeastOneValidInput = true;

    [Header("Progress UI")]
    [SerializeField] private Slider relayBar;
    [SerializeField] private Slider[] lineSliders;
    [SerializeField] private float fillDurationSeconds = 0.5f;

    public bool IsActivated { get; private set; }
    public event Action<bool> OnActivatedChanged;

    private float fill01;
    private bool targetActive;
    private bool lastTargetActive;

    void Awake()
    {
        fill01 = 0f;
        targetActive = false;
        lastTargetActive = false;
        RefreshUI();
    }

    void Update()
    {
        targetActive = EvaluateInputs();

        if (targetActive != lastTargetActive)
        {
            if (!targetActive)
            {
                if (IsActivated)
                {
                    IsActivated = false;
                    OnActivatedChanged?.Invoke(false);
                }
            }

            lastTargetActive = targetActive;
        }

        UpdateFillAndActivation();
        UpdateInputVisuals();
    }

    private bool EvaluateInputs()
    {
        if (inputs == null || inputs.Length == 0)
        {
            if (requireAtLeastOneValidInput) return false;
            return mode == RelayMode.AllOn;
        }

        bool hasValid = false;

        if (mode == RelayMode.AllOn)
        {
            for (int i = 0; i < inputs.Length; i++)
            {
                var entry = inputs[i];

                if (entry == null || entry.source == null)
                {
                    if (requireAtLeastOneValidInput)
                        return false;
                    continue;
                }

                if (entry.source is ISwitch sw)
                {
                    hasValid = true;
                    if (!sw.IsActivated)
                        return false;
                }
                else
                {
                    if (requireAtLeastOneValidInput)
                        return false;
                }
            }

            if (!hasValid && requireAtLeastOneValidInput) return false;
            return true;
        }
        else // AnyOn
        {
            bool anyOn = false;

            for (int i = 0; i < inputs.Length; i++)
            {
                var entry = inputs[i];
                if (entry == null || entry.source == null) continue;

                if (entry.source is ISwitch sw)
                {
                    hasValid = true;
                    if (sw.IsActivated)
                    {
                        anyOn = true;
                        break;
                    }
                }
            }

            if (!hasValid && requireAtLeastOneValidInput) return false;
            return anyOn;
        }
    }

    private void UpdateFillAndActivation()
    {
        float target = targetActive ? 1f : 0f;

        if (fillDurationSeconds <= 0f)
        {
            fill01 = target;
        }
        else
        {
            float speed = 1f / Mathf.Max(0.0001f, fillDurationSeconds);
            fill01 = Mathf.MoveTowards(fill01, target, speed * Time.deltaTime);
        }

        RefreshUI();

        if (!IsActivated && fill01 >= 1f && targetActive)
        {
            IsActivated = true;
            OnActivatedChanged?.Invoke(true);
        }

        if (IsActivated && !targetActive)
        {
            IsActivated = false;
            OnActivatedChanged?.Invoke(false);
        }
    }

    private void RefreshUI()
    {
        float t = Mathf.Clamp01(fill01);

        if (relayBar != null)
            relayBar.value = t;

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

    private void UpdateInputVisuals()
    {
        if (inputs == null) return;

        for (int i = 0; i < inputs.Length; i++)
        {
            var entry = inputs[i];
            if (entry == null) continue;
            if (entry.activeVisual == null) continue;

            bool active = false;

            if (entry.source is ISwitch sw)
                active = sw.IsActivated;

            if (entry.activeVisual.activeSelf != active)
                entry.activeVisual.SetActive(active);
        }
    }

    private void OnValidate()
    {
        if (fillDurationSeconds < 0f) fillDurationSeconds = 0f;
        RefreshUI();
    }
}
