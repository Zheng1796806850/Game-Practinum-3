using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class UIFloatingText : MonoBehaviour
{
    [Header("Position")]
    public Vector2 amplitude = new Vector2(3f, 2f);
    public Vector2 speed = new Vector2(0.35f, 0.45f);
    public Vector2 frequency = new Vector2(1f, 1.2f);
    public bool useUnscaledTime = true;

    [Header("Rotation")]
    public bool affectRotation = false;
    public float rotationAmplitude = 3f;
    public float rotationSpeed = 0.4f;
    public float rotationFrequency = 1f;

    [Header("Scale")]
    public bool affectScale = false;
    public float scaleAmplitude = 0.02f;
    public float scaleSpeed = 0.5f;
    public float scaleFrequency = 1f;

    [Header("Random Phase")]
    public bool randomizePhaseOnEnable = true;
    [Range(0f, 9999f)] public float seed = 0f;

    private RectTransform rt;
    private Vector2 baseAnchoredPos;
    private Quaternion baseRotation;
    private Vector3 baseScale;
    private Vector3 tmpScale;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        baseAnchoredPos = rt.anchoredPosition;
        baseRotation = rt.localRotation;
        baseScale = rt.localScale;
        if (!Application.isPlaying && randomizePhaseOnEnable) seed = GetInstanceID() * 0.12345f;
    }

    void OnEnable()
    {
        if (rt == null) rt = GetComponent<RectTransform>();
        baseAnchoredPos = rt.anchoredPosition;
        baseRotation = rt.localRotation;
        baseScale = rt.localScale;
        if (randomizePhaseOnEnable) seed = Random.value * 1000f + GetInstanceID() * 0.37f;
    }

    void OnDisable()
    {
        if (rt == null) return;
        rt.anchoredPosition = baseAnchoredPos;
        rt.localRotation = baseRotation;
        rt.localScale = baseScale;
    }

    void Update()
    {
        if (rt == null) return;
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float tx = (t * speed.x + seed) * frequency.x;
        float ty = (t * speed.y + seed * 1.2345f) * frequency.y;
        float ox = Mathf.Sin(tx) * amplitude.x;
        float oy = Mathf.Sin(ty) * amplitude.y;
        rt.anchoredPosition = baseAnchoredPos + new Vector2(ox, oy);

        if (affectRotation)
        {
            float tr = (t * rotationSpeed + seed * 2.345f) * rotationFrequency;
            float ang = Mathf.Sin(tr) * rotationAmplitude;
            rt.localRotation = Quaternion.Euler(0f, 0f, ang) * baseRotation;
        }

        if (affectScale)
        {
            float ts = (t * scaleSpeed + seed * 3.456f) * scaleFrequency;
            float s = 1f + Mathf.Sin(ts) * scaleAmplitude;
            tmpScale.x = baseScale.x * s;
            tmpScale.y = baseScale.y * s;
            tmpScale.z = baseScale.z;
            rt.localScale = tmpScale;
        }
    }

    void OnValidate()
    {
        if (rt == null) rt = GetComponent<RectTransform>();
        amplitude.x = Mathf.Max(0f, amplitude.x);
        amplitude.y = Mathf.Max(0f, amplitude.y);
        rotationAmplitude = Mathf.Max(0f, rotationAmplitude);
        scaleAmplitude = Mathf.Max(0f, scaleAmplitude);
    }
}
