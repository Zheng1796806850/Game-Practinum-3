using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class PressurePlateSwitch : MonoBehaviour, ISwitch
{
    public enum PlateType
    {
        General,
        EnemySpecific
    }

    public enum HideMode
    {
        None,
        DisableTilemapRenderer,
        ClearSpecificCells,
        DisableGameObject
    }

    [Header("Type")]
    [SerializeField] private PlateType plateType = PlateType.General;

    [Header("General Plate")]
    [SerializeField] private bool allowPlayer = true;
    [SerializeField] private bool allowEnemies = true;

    [Header("Enemy Plate")]
    [SerializeField] private string requiredEnemyTag = "Enemy";
    [SerializeField] private float captureFreezeDuration = 9999f;
    [SerializeField] private Transform captureAnchor;
    [SerializeField] private bool parentCapturedEnemyToAnchor = true;

    [Header("Charge (%)")]
    [Range(0f, 100f)][SerializeField] private float activationPercent = 100f;
    [Range(0f, 100f)][SerializeField] private float deactivationPercent = 80f;
    [Range(0f, 100f)][SerializeField] private float decayPerSecondPercent = 5f;
    [SerializeField] private float secondsFrom0To100 = 2f;

    [Header("UI")]
    [SerializeField] private Slider plateBar;
    [SerializeField] private Slider[] lineSliders;

    [Header("Sequence")]
    [SerializeField] private bool useSequentialMode = false;
    [SerializeField] private MonoBehaviour[] prerequisites;
    [SerializeField] private bool drainOnPrerequisiteLost = true;

    [Header("Indicator")]
    [SerializeField] private SpriteRenderer indicator;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.green;

    [Header("Visual Hide")]
    [SerializeField] private bool hideWhenPlayerOrEnemyOnTop = true;
    [SerializeField] private HideMode hideMode = HideMode.ClearSpecificCells;
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private bool autoUseCellFromTransform = true;
    [SerializeField] private Vector3Int[] cellsToToggle;
    [SerializeField] private GameObject visualGameObjectOverride;

    [Header("Detection Areas")]
    [SerializeField] private PressurePlateDetectionArea playerDetectionArea;
    [SerializeField] private PressurePlateDetectionArea enemyDetectionArea;

    public bool IsActivated { get; private set; }
    public float ChargePercent { get; private set; }
    public event Action<bool> OnActivatedChanged;

    private HashSet<Collider2D> overlappedEnemyCandidates = new HashSet<Collider2D>();
    private int qualifyingOccupants;
    private TilemapRenderer cachedTilemapRenderer;
    private TileBase[] cachedTiles;
    private bool visualHidden;

    private Collider2D capturedCollider;
    private Transform capturedRoot;
    private Rigidbody2D capturedRb;
    private Transform capturedOriginalParent;
    private RigidbodyConstraints2D capturedOriginalConstraints;
    private float capturedOriginalGravityScale;
    private Health capturedHealth;

    private static readonly List<PressurePlateSwitch> allPlates = new List<PressurePlateSwitch>();

    void Awake()
    {
        if (!allPlates.Contains(this)) allPlates.Add(this);
        PrepareVisualCache();
        InitUI();
        ClampThresholds();
        RefreshIndicator();
        if (captureAnchor == null) captureAnchor = transform;
        AutoFindDetectionAreas();
    }

    void OnEnable()
    {
        if (!allPlates.Contains(this)) allPlates.Add(this);
    }

    void OnDisable()
    {
        if (allPlates.Contains(this)) allPlates.Remove(this);
        ReleaseCapturedEnemyInternal(false);
    }

    void OnDestroy()
    {
        if (allPlates.Contains(this)) allPlates.Remove(this);
        ReleaseCapturedEnemyInternal(true);
    }

    void Update()
    {
        bool prereqsMet = !useSequentialMode || ArePrerequisitesMet();
        bool hasActiveOccupant = HasActiveOccupant();

        if (!prereqsMet)
        {
            if (drainOnPrerequisiteLost && (ChargePercent > 0f || IsActivated))
            {
                ChargePercent = 0f;
                if (IsActivated)
                {
                    IsActivated = false;
                    OnActivatedChanged?.Invoke(false);
                    RefreshIndicator();
                }
                RefreshUI();
            }

            UpdatePlateVisual(hideWhenPlayerOrEnemyOnTop && qualifyingOccupants > 0);
            return;
        }

        if (hasActiveOccupant && secondsFrom0To100 > 0f)
        {
            float perSecond = 100f / Mathf.Max(0.0001f, secondsFrom0To100);
            ChargePercent = Mathf.Clamp(ChargePercent + perSecond * Time.deltaTime, 0f, 100f);
        }
        else if (!hasActiveOccupant && decayPerSecondPercent > 0f)
        {
            ChargePercent = Mathf.Clamp(ChargePercent - decayPerSecondPercent * Time.deltaTime, 0f, 100f);
        }

        EvaluateActivation();
        RefreshUI();
        UpdatePlateVisual(hideWhenPlayerOrEnemyOnTop && qualifyingOccupants > 0);
    }

    private bool HasActiveOccupant()
    {
        if (plateType == PlateType.General)
        {
            return qualifyingOccupants > 0;
        }
        else
        {
            return capturedRoot != null;
        }
    }

    public void HandleAreaTriggerEnter(PressurePlateDetectionArea area, Collider2D other)
    {
        if (area == null || other == null) return;

        if (area == playerDetectionArea && IsPlayerOrEnemy(other))
        {
            qualifyingOccupants++;
        }

        if (area == enemyDetectionArea && plateType == PlateType.EnemySpecific && IsAllowedEnemy(other))
        {
            overlappedEnemyCandidates.Add(other);
        }
    }

    public void HandleAreaTriggerExit(PressurePlateDetectionArea area, Collider2D other)
    {
        if (area == null || other == null) return;

        if (area == playerDetectionArea && IsPlayerOrEnemy(other))
        {
            qualifyingOccupants = Mathf.Max(0, qualifyingOccupants - 1);
        }

        if (area == enemyDetectionArea && plateType == PlateType.EnemySpecific)
        {
            overlappedEnemyCandidates.Remove(other);
            if (capturedCollider == other)
            {
                ReleaseCapturedEnemyInternal(false);
            }
        }
    }

    private void EvaluateActivation()
    {
        bool newActive = IsActivated;

        if (IsActivated)
        {
            if (ChargePercent < deactivationPercent) newActive = false;
        }
        else
        {
            if (ChargePercent >= activationPercent) newActive = true;
        }

        if (newActive != IsActivated)
        {
            IsActivated = newActive;
            OnActivatedChanged?.Invoke(IsActivated);
            RefreshIndicator();
        }
    }

    private void InitUI()
    {
        ChargePercent = 0f;

        if (plateBar != null)
        {
            plateBar.maxValue = 1f;
            plateBar.value = 0f;
        }

        if (lineSliders != null)
        {
            for (int i = 0; i < lineSliders.Length; i++)
            {
                if (lineSliders[i] == null) continue;
                lineSliders[i].maxValue = 1f;
                lineSliders[i].value = 0f;
            }
        }
    }

    private void RefreshUI()
    {
        float t = Mathf.Clamp01(ChargePercent / 100f);

        if (plateBar != null)
            plateBar.value = t;

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

    private void RefreshIndicator()
    {
        if (indicator != null)
            indicator.color = IsActivated ? activeColor : inactiveColor;
    }

    private bool IsPlayerOrEnemy(Collider2D c)
    {
        if (c == null) return false;
        if (!allowPlayer && !allowEnemies) return false;

        bool isPlayer = false;
        bool isEnemy = false;

        if (c.CompareTag("Player") || c.GetComponentInParent<PlayerController>() != null) isPlayer = true;
        if (c.CompareTag("Enemy") || c.GetComponentInParent<Enemy>() != null) isEnemy = true;

        if (isPlayer && !allowPlayer) isPlayer = false;
        if (isEnemy && !allowEnemies) isEnemy = false;

        return isPlayer || isEnemy;
    }

    private bool IsAllowedEnemy(Collider2D c)
    {
        if (c == null) return false;
        if (plateType != PlateType.EnemySpecific) return false;

        if (!string.IsNullOrEmpty(requiredEnemyTag))
        {
            if (c.CompareTag(requiredEnemyTag)) return true;
            if (c.attachedRigidbody != null && c.attachedRigidbody.CompareTag(requiredEnemyTag)) return true;
            if (c.transform.root != null && c.transform.root.CompareTag(requiredEnemyTag)) return true;
            return false;
        }

        if (c.CompareTag("Enemy")) return true;
        if (c.GetComponentInParent<Enemy>() != null) return true;
        return false;
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

    private void PrepareVisualCache()
    {
        if (hideMode == HideMode.DisableTilemapRenderer)
        {
            if (targetTilemap != null)
                cachedTilemapRenderer = targetTilemap.GetComponent<TilemapRenderer>();
        }
        else if (hideMode == HideMode.ClearSpecificCells)
        {
            if (targetTilemap != null)
            {
                if ((cellsToToggle == null || cellsToToggle.Length == 0) && autoUseCellFromTransform)
                {
                    var cell = targetTilemap.WorldToCell(transform.position);
                    cellsToToggle = new[] { cell };
                }
                if (cellsToToggle != null && cellsToToggle.Length > 0)
                {
                    cachedTiles = new TileBase[cellsToToggle.Length];
                    for (int i = 0; i < cellsToToggle.Length; i++)
                    {
                        cachedTiles[i] = targetTilemap.GetTile(cellsToToggle[i]);
                    }
                }
            }
        }
    }

    private void UpdatePlateVisual(bool shouldHide)
    {
        if (hideMode == HideMode.None) return;
        if (shouldHide)
        {
            if (!visualHidden)
            {
                visualHidden = true;
                HideVisual();
            }
        }
        else
        {
            if (visualHidden)
            {
                visualHidden = false;
                ShowVisual();
            }
        }
    }

    private void HideVisual()
    {
        switch (hideMode)
        {
            case HideMode.DisableTilemapRenderer:
                if (cachedTilemapRenderer != null) cachedTilemapRenderer.enabled = false;
                break;
            case HideMode.ClearSpecificCells:
                if (targetTilemap != null && cellsToToggle != null)
                {
                    for (int i = 0; i < cellsToToggle.Length; i++)
                        targetTilemap.SetTile(cellsToToggle[i], null);
                }
                break;
            case HideMode.DisableGameObject:
                if (visualGameObjectOverride != null) visualGameObjectOverride.SetActive(false);
                break;
        }
    }

    private void ShowVisual()
    {
        switch (hideMode)
        {
            case HideMode.DisableTilemapRenderer:
                if (cachedTilemapRenderer != null) cachedTilemapRenderer.enabled = true;
                break;
            case HideMode.ClearSpecificCells:
                if (targetTilemap != null && cellsToToggle != null && cachedTiles != null)
                {
                    for (int i = 0; i < cellsToToggle.Length && i < cachedTiles.Length; i++)
                        targetTilemap.SetTile(cellsToToggle[i], cachedTiles[i]);
                }
                break;
            case HideMode.DisableGameObject:
                if (visualGameObjectOverride != null) visualGameObjectOverride.SetActive(true);
                break;
        }
    }

    private void ClampThresholds()
    {
        activationPercent = Mathf.Clamp(activationPercent, 0f, 100f);
        deactivationPercent = Mathf.Clamp(deactivationPercent, 0f, activationPercent);
        decayPerSecondPercent = Mathf.Max(0f, decayPerSecondPercent);
        secondsFrom0To100 = Mathf.Max(0.0001f, secondsFrom0To100);
    }

    private void OnValidate()
    {
        ClampThresholds();
        RefreshUI();
        RefreshIndicator();
        AutoFindDetectionAreas();
    }

    private void AutoFindDetectionAreas()
    {
        var areas = GetComponentsInChildren<PressurePlateDetectionArea>(true);
        for (int i = 0; i < areas.Length; i++)
        {
            var a = areas[i];
            if (a == null) continue;
            if (a.Owner != this) continue;

            if (a.Kind == PressurePlateDetectionArea.AreaKind.Player)
            {
                if (playerDetectionArea == null) playerDetectionArea = a;
            }
            else if (a.Kind == PressurePlateDetectionArea.AreaKind.Enemy)
            {
                if (enemyDetectionArea == null) enemyDetectionArea = a;
            }
        }
    }

    public static void NotifyEnemyBoopedPush(Collider2D enemyCol)
    {
        if (enemyCol == null) return;
        for (int i = 0; i < allPlates.Count; i++)
        {
            var p = allPlates[i];
            if (p == null) continue;
            p.OnEnemyBooped(enemyCol, true);
        }
    }

    public static void NotifyEnemyBoopedPull(Collider2D enemyCol)
    {
        if (enemyCol == null) return;
        for (int i = 0; i < allPlates.Count; i++)
        {
            var p = allPlates[i];
            if (p == null) continue;
            p.OnEnemyBooped(enemyCol, false);
        }
    }

    private void OnEnemyBooped(Collider2D enemyCol, bool isPush)
    {
        if (plateType != PlateType.EnemySpecific) return;
        if (!IsAllowedEnemy(enemyCol)) return;

        if (isPush)
        {
            if (!overlappedEnemyCandidates.Contains(enemyCol)) return;
            if (capturedRoot == null)
            {
                CaptureEnemy(enemyCol);
            }
        }
        else
        {
            if (capturedCollider == enemyCol)
            {
                ReleaseCapturedEnemyInternal(false);
            }
        }
    }

    private void CaptureEnemy(Collider2D enemyCol)
    {
        if (enemyCol == null) return;

        var root = enemyCol.attachedRigidbody ? enemyCol.attachedRigidbody.transform : enemyCol.transform;
        capturedCollider = enemyCol;
        capturedRoot = root;
        capturedRb = root.GetComponent<Rigidbody2D>();
        capturedHealth = root.GetComponent<Health>();

        if (capturedRb != null)
        {
            capturedOriginalConstraints = capturedRb.constraints;
            capturedOriginalGravityScale = capturedRb.gravityScale;
            capturedRb.linearVelocity = Vector2.zero;
            capturedRb.angularVelocity = 0f;
            capturedRb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        capturedOriginalParent = root.parent;

        if (captureAnchor != null && parentCapturedEnemyToAnchor)
        {
            root.position = captureAnchor.position;
            root.rotation = captureAnchor.rotation;
            root.parent = captureAnchor;
        }
        else if (captureAnchor != null)
        {
            root.position = captureAnchor.position;
        }
        else
        {
            root.position = transform.position;
        }

        if (capturedHealth != null && captureFreezeDuration > 0f)
        {
            capturedHealth.ApplyFreeze(captureFreezeDuration);
        }
    }

    private void ReleaseCapturedEnemyInternal(bool destroying)
    {
        if (capturedRoot == null) return;

        if (capturedRb != null)
        {
            capturedRb.constraints = capturedOriginalConstraints;
            capturedRb.gravityScale = capturedOriginalGravityScale;
        }

        if (parentCapturedEnemyToAnchor && capturedRoot != null)
        {
            capturedRoot.parent = capturedOriginalParent;
        }

        if (capturedHealth != null)
        {
            capturedHealth.ClearFreeze();
        }

        capturedCollider = null;
        capturedRoot = null;
        capturedRb = null;
        capturedOriginalParent = null;
        capturedHealth = null;
    }
}
