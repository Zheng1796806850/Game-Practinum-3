using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class EndSequenceTrigger : MonoBehaviour
{
    [System.Serializable]
    public class Segment
    {
        public string text;
        public float fadeIn = 1f;
        public float hold = 1.5f;
        public float fadeOut = 1f;
    }

    [System.Serializable]
    public class DisableEntry
    {
        public UnityEngine.Object target;
        public float delay;
    }

    public string playerTag = "Player";
    public bool triggerOnce = true;
    public bool useUnscaledTime = true;

    public float overlayFadeIn = 1.2f;
    public Color overlayColor = new Color(0f, 0f, 0f, 1f);

    public List<Segment> segments = new List<Segment>()
    {
        new Segment(){ text="1", fadeIn=0.8f, hold=1.2f, fadeOut=0.6f },
        new Segment(){ text="2", fadeIn=0.8f, hold=1.8f, fadeOut=0.6f }
    };

    public float gapBetweenSegments = 0.2f;

    public Font textFont;
    public int fontSize = 42;
    public Color textColor = Color.white;
    public Vector2 textMargins = new Vector2(160f, 120f);
    public float textLineSpacing = 1f;
    public FontStyle textStyle = FontStyle.Normal;
    public TextAnchor textAlignment = TextAnchor.MiddleCenter;

    public UnityEvent onActivateEndPanel;
    public float delayBeforeOverlayFadeOutAfterPanel = 1f;
    public float overlayFadeOutDuration = 0.8f;

    public List<DisableEntry> disableSchedule = new List<DisableEntry>();

    private bool hasTriggered = false;
    private Canvas overlayCanvas;
    private CanvasScaler overlayScaler;
    private GameObject overlayRoot;
    private Image overlayImage;
    private CanvasGroup overlayGroup;
    private GameObject textRoot;
    private Text uiText;
    private CanvasGroup textGroup;
    private Coroutine running;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (triggerOnce && hasTriggered) return;
        hasTriggered = true;
        StartSequence();
    }

    public void StartSequence()
    {
        if (running != null) return;
        StartDisableSchedule();
        BuildUIIfNeeded();
        running = StartCoroutine(SequenceRoutine());
    }

    private void StartDisableSchedule()
    {
        for (int i = 0; i < disableSchedule.Count; i++)
        {
            var entry = disableSchedule[i];
            if (entry == null) continue;
            StartCoroutine(DisableAfterDelay(entry));
        }
    }

    private IEnumerator DisableAfterDelay(DisableEntry entry)
    {
        float d = Mathf.Max(0f, entry.delay);
        if (d > 0f) yield return WaitFor(d);
        DisableTarget(entry.target);
    }

    private void DisableTarget(UnityEngine.Object o)
    {
        if (o == null) return;

        if (o is GameObject go)
        {
            go.SetActive(false);
            return;
        }

        if (o is Behaviour behaviour)
        {
            behaviour.enabled = false;
            return;
        }

        if (o is Renderer renderer)
        {
            renderer.enabled = false;
            return;
        }

        if (o is Collider collider3D)
        {
            collider3D.enabled = false;
            return;
        }

        if (o is Collider2D collider2D)
        {
            collider2D.enabled = false;
            return;
        }

        var comp = o as Component;
        if (comp != null)
        {
            var p = comp.GetType().GetProperty("enabled", BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(bool) && p.CanWrite)
            {
                p.SetValue(comp, false, null);
            }
        }
    }

    private void BuildUIIfNeeded()
    {
        if (overlayCanvas != null) return;

        overlayRoot = new GameObject("EndSequenceOverlay");
        overlayCanvas = overlayRoot.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 5000;
        overlayScaler = overlayRoot.AddComponent<CanvasScaler>();
        overlayScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        overlayScaler.referenceResolution = new Vector2(1920, 1080);
        overlayRoot.AddComponent<GraphicRaycaster>();

        var overlayImageGO = new GameObject("Overlay");
        overlayImageGO.transform.SetParent(overlayRoot.transform, false);
        overlayImage = overlayImageGO.AddComponent<Image>();
        overlayImage.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 1f);
        var overlayRect = overlayImage.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayGroup = overlayImageGO.AddComponent<CanvasGroup>();
        overlayGroup.alpha = 0f;
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;

        textRoot = new GameObject("Text");
        textRoot.transform.SetParent(overlayRoot.transform, false);
        uiText = textRoot.AddComponent<Text>();
        if (textFont == null) textFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        uiText.font = textFont;
        uiText.fontSize = Mathf.Max(10, fontSize);
        uiText.color = textColor;
        uiText.alignment = textAlignment;
        uiText.supportRichText = true;
        uiText.lineSpacing = Mathf.Max(0.1f, textLineSpacing);
        var textRect = uiText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(textMargins.x, textMargins.y);
        textRect.offsetMax = new Vector2(-textMargins.x, -textMargins.y);
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Truncate;
        uiText.text = "";
        uiText.fontStyle = textStyle;

        textGroup = textRoot.AddComponent<CanvasGroup>();
        textGroup.alpha = 0f;
        textGroup.interactable = false;
        textGroup.blocksRaycasts = false;
    }

    private IEnumerator SequenceRoutine()
    {
        yield return FadeCanvasGroup(overlayGroup, 0f, 1f, overlayFadeIn);

        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            uiText.text = seg.text;
            textGroup.alpha = 0f;
            yield return FadeCanvasGroup(textGroup, 0f, 1f, Mathf.Max(0f, seg.fadeIn));
            yield return WaitFor(seg.hold);
            yield return FadeCanvasGroup(textGroup, 1f, 0f, Mathf.Max(0f, seg.fadeOut));
            if (gapBetweenSegments > 0f) yield return WaitFor(gapBetweenSegments);
        }

        uiText.text = "";
        textGroup.alpha = 0f;

        onActivateEndPanel?.Invoke();

        if (delayBeforeOverlayFadeOutAfterPanel > 0f)
            yield return WaitFor(delayBeforeOverlayFadeOutAfterPanel);

        yield return FadeCanvasGroup(overlayGroup, 1f, 0f, Mathf.Max(0f, overlayFadeOutDuration));

        Destroy(overlayRoot);
        running = null;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            cg.alpha = to;
            yield break;
        }
        float t = 0f;
        cg.alpha = from;
        while (t < duration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            cg.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }
        cg.alpha = to;
    }

    private IEnumerator WaitFor(float duration)
    {
        if (duration <= 0f) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }
}
