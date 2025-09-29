using UnityEngine;

public class WaterPlatform : MonoBehaviour, IFreezable
{
    public enum UnfreezeMode
    {
        TimedByBullet,
        RequireFire
    }

    public enum ClampMode
    {
        XRangeOnly,
        ClampZone,
        VirtualTrigger
    }

    public enum ClampBehavior
    {
        HoldAtMax,        // 限高时：直接贴住玩家脚底
        OscillateToMax    // 限高时：继续起伏，但最高不超过玩家脚底
    }

    [Header("Visuals")]
    [SerializeField] private Transform waterVisualRoot;
    [SerializeField] private SpriteRenderer waterRenderer;
    [SerializeField] private Transform iceVisualRoot;
    [SerializeField] private SpriteRenderer iceRenderer;

    [Header("Clamp")]
    [SerializeField] private ClampMode clampMode = ClampMode.XRangeOnly;
    [SerializeField] private ClampBehavior clampBehavior = ClampBehavior.HoldAtMax;
    [SerializeField] private float xRangeHalfWidth = 0.45f;
    [SerializeField] private float zoneHalfWidth = 0.35f;
    [SerializeField] private float zoneHeight = 0.3f;
    [SerializeField] private float zoneEnterPad = 0.04f;
    [SerializeField] private float zoneExitPad = 0.08f;
    [SerializeField] private float clampRiseSpeed = 12f;   // 进入限高时向上追近速度
    [SerializeField] private float clampFallSpeed = 20f;   // 离开限高时放开回落速度
    [SerializeField] private float virtualTriggerHeight = 12f; // VirtualTrigger 的检测高度（独立于 maxHeight）

    [Header("General")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float maxHeight = 4f;          // 自由起伏时的最高高度
    [SerializeField] private float minHeight = 0.2f;
    [SerializeField] private float oscillationSpeed = 1.5f;
    [SerializeField] private float jitterStrength = 0.15f;
    [SerializeField] private float oscillationAmplitudeFactor = 1.0f;
    [SerializeField] private float startWidth = 0.5f;

    [Header("Unfreeze Rule")]
    [SerializeField] private UnfreezeMode unfreezeMode = UnfreezeMode.TimedByBullet;
    [SerializeField] private float defaultFreezeDuration = 3f;
    [SerializeField] private bool meltWhenHitByFireInTimedMode = true;

    [Header("Colliders")]
    [SerializeField] private BoxCollider2D hitTrigger;
    [SerializeField] private BoxCollider2D iceCollider;

    private Transform player;
    private Collider2D playerCol;
    private bool isFrozen;
    private float freezeTimer;
    private float seed;
    private float currentVisualWidth;
    private float currentVisualHeight;
    private bool clampActive;

    void Awake()
    {
        seed = Random.value * 1000f;
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
    }

    void Start()
    {
        if (player == null && !string.IsNullOrEmpty(playerTag))
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null)
            {
                player = go.transform;
                playerCol = go.GetComponent<Collider2D>();
            }
        }
    }

    void Update()
    {
        if (isFrozen)
        {
            if (unfreezeMode == UnfreezeMode.TimedByBullet)
            {
                if (freezeTimer > 0f)
                {
                    freezeTimer -= Time.deltaTime;
                    if (freezeTimer <= 0f) Unfreeze();
                }
            }
            return;
        }

        UpdateClampState();

        // 当前实际高度（用于平滑）
        float curH = (waterRenderer != null) ? waterRenderer.size.y : minHeight;

        float targetH;

        if (clampActive && playerCol != null)
        {
            // 允许的最高高度 = 玩家脚底到喷口的垂直距离（不再受 maxHeight 限制）
            float footDelta = playerCol.bounds.min.y - transform.position.y;
            float allowedMax = Mathf.Max(minHeight, footDelta);

            if (clampBehavior == ClampBehavior.HoldAtMax)
            {
                // 直接把高度推到玩家脚底（带平滑）
                targetH = Mathf.MoveTowards(curH, allowedMax, clampRiseSpeed * Time.deltaTime);
            }
            else // OscillateToMax
            {
                float t = Time.time * oscillationSpeed;
                float osc01 = 0.5f + 0.5f * Mathf.Sin(t);
                float noise = (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f * jitterStrength;
                float k = Mathf.Clamp01(osc01 * Mathf.Max(0f, oscillationAmplitudeFactor) + noise * 0.25f);
                float oscH = Mathf.Lerp(minHeight, allowedMax, k);
                // 为了更容易“摸到”脚底，向上追一点
                float lifted = Mathf.MoveTowards(curH, allowedMax, clampRiseSpeed * Time.deltaTime);
                targetH = Mathf.Max(oscH, lifted);
            }
        }
        else
        {
            // 非限高：常规起伏（在 minHeight..maxHeight 之间）
            float t = Time.time * oscillationSpeed;
            float osc01 = 0.5f + 0.5f * Mathf.Sin(t);
            float noise = (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f * jitterStrength;
            float k = Mathf.Clamp01(osc01 * Mathf.Max(0f, oscillationAmplitudeFactor) + noise * 0.25f);
            float freeH = Mathf.Lerp(minHeight, maxHeight, k);
            // 离开限高后不要瞬间弹回，做个下落平滑
            targetH = Mathf.MoveTowards(curH, freeH, clampFallSpeed * Time.deltaTime);
        }

        ApplyWaterHeight(targetH);
        ReadVisualWH();
        SyncTriggerTo(currentVisualWidth, currentVisualHeight);
    }

    private void UpdateClampState()
    {
        if (playerCol == null)
        {
            clampActive = false;
            return;
        }

        // 统一用碰撞体的世界中心与脚点
        float px = playerCol.bounds.center.x;
        float footY = playerCol.bounds.min.y;
        float nozzleY = transform.position.y;
        float selfX = transform.position.x;

        // 给横向范围加上玩家半宽，避免 pivot 偏移带来的误判
        float playerHalfW = playerCol.bounds.extents.x;

        if (clampMode == ClampMode.XRangeOnly)
        {
            float range = xRangeHalfWidth + playerHalfW;
            clampActive = Mathf.Abs(px - selfX) <= range;
            return;
        }

        if (clampMode == ClampMode.ClampZone)
        {
            float dx = Mathf.Abs(px - selfX);
            bool insideCore = dx <= (zoneHalfWidth + playerHalfW) && footY <= nozzleY + zoneHeight;
            bool insideEnter = dx <= (zoneHalfWidth + playerHalfW + zoneEnterPad) && footY <= nozzleY + zoneHeight + zoneEnterPad;
            bool insideExit = dx <= (zoneHalfWidth + playerHalfW + zoneExitPad) && footY <= nozzleY + zoneHeight + zoneExitPad;

            if (!clampActive) { if (insideCore || insideEnter) clampActive = true; }
            else { if (!insideExit) clampActive = false; }
            return;
        }

        if (clampMode == ClampMode.VirtualTrigger)
        {
            float halfW = Mathf.Max(xRangeHalfWidth, currentVisualWidth * 0.5f) + playerHalfW;
            float h = Mathf.Max(0.01f, virtualTriggerHeight); // 独立高度，不用 maxHeight
            Bounds box = new Bounds(
                new Vector3(selfX, nozzleY + h * 0.5f, 0f),
                new Vector3(halfW * 2f, h, 0.1f)
            );
            clampActive = box.Intersects(playerCol.bounds);
        }
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

        // 不再把冻结高度上限夹到 maxHeight，避免被强行压低
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

    // IFreezable
    public void ApplyFreeze(float duration)
    {
        if (!isFrozen)
        {
            // 冻结时取“当前应有的高度”，保证冰柱与此刻水柱无缝衔接
            float curH = (waterRenderer != null) ? waterRenderer.size.y : minHeight;
            Freeze(duration, curH);
        }
        else
        {
            if (unfreezeMode == UnfreezeMode.TimedByBullet)
                freezeTimer = Mathf.Max(freezeTimer, duration);
        }
    }

    public void TryMeltFromFire()
    {
        if (!isFrozen) return;
        if (unfreezeMode == UnfreezeMode.RequireFire) Unfreeze();
        else if (meltWhenHitByFireInTimedMode) Unfreeze();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (playerCol == null) return;

        Gizmos.color = Color.yellow;
        float selfX = transform.position.x;
        float playerHalfW = playerCol.bounds.extents.x;

        if (clampMode == ClampMode.XRangeOnly)
        {
            float range = xRangeHalfWidth + playerHalfW;
            Gizmos.DrawLine(new Vector3(selfX - range, transform.position.y, 0),
                            new Vector3(selfX - range, transform.position.y + 0.2f, 0));
            Gizmos.DrawLine(new Vector3(selfX + range, transform.position.y, 0),
                            new Vector3(selfX + range, transform.position.y + 0.2f, 0));
        }
        else if (clampMode == ClampMode.VirtualTrigger)
        {
            float halfW = Mathf.Max(xRangeHalfWidth, currentVisualWidth * 0.5f) + playerHalfW;
            float h = Mathf.Max(0.01f, virtualTriggerHeight);
            Gizmos.DrawWireCube(new Vector3(selfX, transform.position.y + h * 0.5f, 0),
                                new Vector3(halfW * 2f, h, 0.1f));
        }
    }
#endif

}
