using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GlitchText : MonoBehaviour
{
    [Header("Target")]
    public TMP_Text tmpText;
    public Text uiText;

    [Header("Timing")]
    public bool useUnscaledTime = true;
    public float minGlitchInterval = 0.5f;
    public float maxGlitchInterval = 3f;
    public float glitchDuration = 0.2f;

    [Header("Flicker")]
    public float flickerFrequency = 40f;
    [Range(0f, 1f)] public float flickerMinAlpha = 0.2f;

    [Header("Glitch Text")]
    public bool randomizeCharacters = true;
    public string glitchCharacters = "0123456789!@#$%^&*?<>";

    [Header("Jitter")]
    public bool jitterPosition = true;
    public float jitterAmount = 1f;

    private string originalText;
    private Color originalColor;
    private Vector3 originalLocalPos;
    private float nextGlitchTime;
    private float glitchEndTime;
    private bool inGlitch;

    private bool hasTmp;
    private bool hasUi;

    private float CurrentTime
    {
        get { return useUnscaledTime ? Time.unscaledTime : Time.time; }
    }

    private float DeltaTime
    {
        get { return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; }
    }

    void Awake()
    {
        if (tmpText == null) tmpText = GetComponent<TMP_Text>();
        if (uiText == null) uiText = GetComponent<Text>();

        hasTmp = tmpText != null;
        hasUi = uiText != null;

        if (!hasTmp && !hasUi) return;

        if (hasTmp)
        {
            originalText = tmpText.text;
            originalColor = tmpText.color;
        }
        else
        {
            originalText = uiText.text;
            originalColor = uiText.color;
        }

        originalLocalPos = transform.localPosition;
        ScheduleNextGlitch();
    }

    void OnEnable()
    {
        if (hasTmp)
        {
            originalText = tmpText.text;
            originalColor = tmpText.color;
        }
        else if (hasUi)
        {
            originalText = uiText.text;
            originalColor = uiText.color;
        }

        originalLocalPos = transform.localPosition;
        inGlitch = false;
        ScheduleNextGlitch();
    }

    void Update()
    {
        if (!hasTmp && !hasUi) return;

        float t = CurrentTime;

        if (!inGlitch)
        {
            if (t >= nextGlitchTime)
            {
                StartGlitch(t);
            }
        }
        else
        {
            if (t >= glitchEndTime)
            {
                EndGlitch();
                ScheduleNextGlitch();
            }
            else
            {
                UpdateGlitch(t);
            }
        }
    }

    private void ScheduleNextGlitch()
    {
        float interval = Random.Range(minGlitchInterval, maxGlitchInterval);
        nextGlitchTime = CurrentTime + Mathf.Max(0.01f, interval);
    }

    private void StartGlitch(float startTime)
    {
        inGlitch = true;
        glitchEndTime = startTime + Mathf.Max(0.01f, glitchDuration);

        if (randomizeCharacters && !string.IsNullOrEmpty(glitchCharacters))
        {
            string glitched = GenerateGlitchedText(originalText.Length);
            SetText(glitched);
        }
    }

    private void EndGlitch()
    {
        inGlitch = false;
        SetText(originalText);
        SetColorAlpha(originalColor.a);
        transform.localPosition = originalLocalPos;
    }

    private void UpdateGlitch(float t)
    {
        float alphaFactor = 0.5f + 0.5f * Mathf.Sin(t * flickerFrequency * Mathf.PI * 2f);
        float a = Mathf.Lerp(flickerMinAlpha, 1f, alphaFactor);
        SetColorAlpha(a);

        if (jitterPosition)
        {
            Vector2 offset = Random.insideUnitCircle * jitterAmount;
            transform.localPosition = originalLocalPos + (Vector3)offset;
        }

        if (randomizeCharacters && !string.IsNullOrEmpty(glitchCharacters))
        {
            string glitched = GenerateGlitchedText(originalText.Length);
            SetText(glitched);
        }
    }

    private string GenerateGlitchedText(int length)
    {
        if (length <= 0) return "";
        int poolLen = glitchCharacters.Length;
        if (poolLen == 0) return originalText;

        char[] buffer = new char[length];
        for (int i = 0; i < length; i++)
        {
            buffer[i] = glitchCharacters[Random.Range(0, poolLen)];
        }
        return new string(buffer);
    }

    private void SetText(string value)
    {
        if (hasTmp) tmpText.text = value;
        if (hasUi) uiText.text = value;
    }

    private void SetColorAlpha(float a)
    {
        if (hasTmp)
        {
            Color c = tmpText.color;
            c.a = a;
            tmpText.color = c;
        }

        if (hasUi)
        {
            Color c = uiText.color;
            c.a = a;
            uiText.color = c;
        }
    }
}
