using UnityEngine;

public class WaterPlatform : MonoBehaviour, IFreezable
{
    public enum UnfreezeMode
    {
        TimedByBullet,
        RequireFire
    }

    public enum FountainType
    {
        Normal,
        Lava
    }

    [Header("Visuals")]
    [SerializeField] private Transform waterVisualRoot;
    [SerializeField] private SpriteRenderer waterRenderer;
    [SerializeField] private Transform iceVisualRoot;
    [SerializeField] private SpriteRenderer iceRenderer;

    [Header("General")]
    [SerializeField] private FountainType fountainType = FountainType.Normal;
    [SerializeField] private float maxHeight = 4f;
    [SerializeField] private float minHeight = 0.2f;
    [SerializeField] private float riseDuration = 1.5f;
    [SerializeField] private float fallDuration = 1.5f;
    [SerializeField] private float pauseAtTop = 0.5f;
    [SerializeField] private float pauseAtBottom = 0.5f;
    [SerializeField] private float startWidth = 0.5f;

    [Header("Unfreeze Rule")]
    [SerializeField] private UnfreezeMode unfreezeMode = UnfreezeMode.TimedByBullet;
    [SerializeField] private float defaultFreezeDuration = 3f;
    [SerializeField] private bool meltWhenHitByFireInTimedMode = true;

    [Header("Lava Settings")]
    [SerializeField] private float lavaDamage = 1f;
    [SerializeField] private string playerTag = "Player";

    [Header("Colliders")]
    [SerializeField] private BoxCollider2D hitTrigger;
    [SerializeField] private BoxCollider2D iceCollider;

#if UNITY_EDITOR
    [Header("Gizmos")]
    [SerializeField] private bool gizmosShowRealtimePoint = true;
    [SerializeField] private float gizmosPreviewScaleX = 0.0f;
#endif

    private bool isFrozen;
    private float freezeTimer;
    private float currentVisualWidth;
    private float currentVisualHeight;

    private float cycleTimer;
    private enum CycleState { Rising, PauseTop, Falling, PauseBottom }
    private CycleState cycleState = CycleState.Rising;

    private Collider2D playerCol;

    void Awake()
    {
        if (waterVisualRoot != null) waterVisualRoot.localScale = Vector3.one;
        if (iceVisualRoot != null) iceVisualRoot.localScale = Vector3.one;
        if (hitTrigger != null) hitTrigger.transform.localScale = Vector3.one;
        if (iceCollider != null) iceCollider.transform.localScale = Vector3.one;

        if (waterRenderer != null) waterRenderer.drawMode = SpriteDrawMode.Tiled;
        if (iceRenderer != null) iceRenderer.drawMode = SpriteDrawMode.Tiled;

        if (waterRenderer != null)
        {
            var sz = waterRenderer.size;
            sz.x = Mathf.Max(0.01f, startWidth);
            sz.y = Mathf.Max(0.01f, minHeight);
            waterRenderer.size = sz;
        }

        SetWaterActive(true);
        SetIceActive(false);
        if (iceCollider != null) iceCollider.enabled = false;

        ReadVisualWH();
        SyncTriggerTo(currentVisualWidth, currentVisualHeight);

        cycleTimer = 0f;
        cycleState = CycleState.Rising;
    }

    void Start()
    {
        if (!string.IsNullOrEmpty(playerTag))
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) playerCol = go.GetComponent<Collider2D>();
        }
    }

    void Update()
    {
        if (isFrozen)
        {
            if (unfreezeMode == UnfreezeMode.TimedByBullet && freezeTimer > 0f)
            {
                freezeTimer -= Time.deltaTime;
                if (freezeTimer <= 0f) Unfreeze();
            }
            return;
        }

        UpdateFountainMotion();

        ReadVisualWH();
        SyncTriggerTo(currentVisualWidth, currentVisualHeight);
    }

    private void UpdateFountainMotion()
    {
        float targetH = GetSimulatedHeightAtTime(cycleTimer);

        cycleTimer += Time.deltaTime;
        float cycleLen = riseDuration + pauseAtTop + fallDuration + pauseAtBottom;
        if (cycleTimer >= cycleLen) cycleTimer -= cycleLen;

        ApplyWaterHeight(targetH);
    }

    private float GetSimulatedHeightAtTime(float t)
    {
        float cycleLen = riseDuration + pauseAtTop + fallDuration + pauseAtBottom;
        float cycleT = t % cycleLen;

        if (cycleT < riseDuration)
        {
            float tNorm = cycleT / riseDuration;
            float k = 1f - (1f - tNorm) * (1f - tNorm);
            return Mathf.Lerp(minHeight, maxHeight, k);
        }
        cycleT -= riseDuration;

        if (cycleT < pauseAtTop)
        {
            return maxHeight;
        }
        cycleT -= pauseAtTop;

        if (cycleT < fallDuration)
        {
            float tNorm = cycleT / fallDuration;
            float k = tNorm * tNorm;
            return Mathf.Lerp(maxHeight, minHeight, k);
        }

        return minHeight;
    }

    private void ApplyWaterHeight(float h)
    {
        float hh = Mathf.Max(0.01f, h);
        if (waterRenderer != null)
        {
            Vector2 sz = waterRenderer.size;
            sz.y = hh;
            waterRenderer.size = sz;
        }
        if (waterVisualRoot != null)
        {
            waterVisualRoot.localPosition = new Vector3(0f, hh * 0.5f, 0f);
        }
    }

    private void ReadVisualWH()
    {
        float w = startWidth;
        float h = minHeight;
        if (waterRenderer != null) w = Mathf.Max(0.01f, waterRenderer.size.x);
        if (waterRenderer != null) h = Mathf.Max(0.01f, waterRenderer.size.y);
        currentVisualWidth = w;
        currentVisualHeight = h;
    }

    private void SyncTriggerTo(float w, float h)
    {
        if (hitTrigger != null)
        {
            hitTrigger.size = new Vector2(w, h);
            hitTrigger.offset = new Vector2(0f, h * 0.5f);
        }
        if (isFrozen && iceCollider != null)
        {
            iceCollider.size = new Vector2(w, h);
            iceCollider.offset = new Vector2(0f, h * 0.5f);
        }
    }

    private void Freeze(float duration, float heightAtFreeze)
    {
        isFrozen = true;

        float h = Mathf.Max(minHeight, heightAtFreeze);

        ReadVisualWH();
        float w = currentVisualWidth;
        SetWaterActive(false);
        SetIceActive(true);

        if (iceRenderer != null)
        {
            Vector2 sz = iceRenderer.size;
            sz.x = Mathf.Max(0.01f, w);
            sz.y = Mathf.Max(0.01f, h);
            iceRenderer.size = sz;
        }
        if (iceVisualRoot != null)
        {
            iceVisualRoot.localPosition = new Vector3(0f, h * 0.5f, 0f);
        }
        if (iceCollider != null)
        {
            iceCollider.enabled = true;
            iceCollider.size = new Vector2(w, h);
            iceCollider.offset = new Vector2(0f, h * 0.5f);
        }

        freezeTimer = (unfreezeMode == UnfreezeMode.TimedByBullet)
            ? Mathf.Max(duration, defaultFreezeDuration)
            : Mathf.Infinity;
    }

    private void Unfreeze()
    {
        isFrozen = false;
        SetIceActive(false);
        if (iceCollider != null) iceCollider.enabled = false;
        SetWaterActive(true);
        ReadVisualWH();
        SyncTriggerTo(currentVisualWidth, currentVisualHeight);
    }

    private void SetWaterActive(bool active)
    {
        if (waterVisualRoot != null) waterVisualRoot.gameObject.SetActive(active);
        if (waterRenderer != null) waterRenderer.enabled = active;
    }

    private void SetIceActive(bool active)
    {
        if (iceVisualRoot != null) iceVisualRoot.gameObject.SetActive(active);
        if (iceRenderer != null) iceRenderer.enabled = active;
    }

    public void ApplyFreeze(float duration)
    {
        if (!isFrozen)
        {
            float curH = (waterRenderer != null) ? waterRenderer.size.y : minHeight;
            Freeze(defaultFreezeDuration, curH);
        }
        else
        {
            if (unfreezeMode == UnfreezeMode.TimedByBullet)
                freezeTimer = Mathf.Max(freezeTimer, defaultFreezeDuration);
        }
    }

    public void TryMeltFromFire()
    {
        if (!isFrozen) return;
        if (unfreezeMode == UnfreezeMode.RequireFire) Unfreeze();
        else if (meltWhenHitByFireInTimedMode) Unfreeze();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (fountainType == FountainType.Lava && !isFrozen)
        {
            if (other.CompareTag(playerTag))
            {
                if (other.TryGetComponent<IDamageable>(out var dmg))
                {
                    dmg.ApplyDamage(lavaDamage * Time.deltaTime, gameObject);
                }
            }
        }
    }

#if UNITY_EDITOR
    private float GetWaterWorldScaleY()
    {
        if (waterVisualRoot != null) return waterVisualRoot.lossyScale.y;
        return transform.lossyScale.y;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = (fountainType == FountainType.Lava) ? Color.red : Color.cyan;

        int steps = 100;
        float previewDuration = riseDuration + pauseAtTop + fallDuration + pauseAtBottom;
        float stepTime = previewDuration / steps;

        Vector3 basePos = transform.position;
        float sY = Mathf.Abs(GetWaterWorldScaleY());
        Vector3 prevPoint = basePos + Vector3.up * (GetSimulatedHeightAtTime(0f) * sY);
        float dx = gizmosPreviewScaleX;

        for (int i = 1; i <= steps; i++)
        {
            float t = i * stepTime;
            float h = GetSimulatedHeightAtTime(t);
            Vector3 point = basePos + new Vector3(dx * i / steps, h * sY, 0f);
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }

        Gizmos.color = Color.yellow;
        Vector3 a = basePos + Vector3.up * (minHeight * sY) + Vector3.left * 0.5f;
        Vector3 b = basePos + Vector3.up * (minHeight * sY) + Vector3.right * 0.5f;
        Gizmos.DrawLine(a, b);
        a = basePos + Vector3.up * (maxHeight * sY) + Vector3.left * 0.5f;
        b = basePos + Vector3.up * (maxHeight * sY) + Vector3.right * 0.5f;
        Gizmos.DrawLine(a, b);

        if (gizmosShowRealtimePoint)
        {
            float liveH = 0f;
            if (waterRenderer != null)
            {
                liveH = waterRenderer.size.y * sY;
            }
            Vector3 p = basePos + Vector3.up * liveH;
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.DrawSolidDisc(p, Vector3.forward, 0.05f);
#endif
        }
    }
#endif
}
