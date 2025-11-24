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
    [SerializeField] private bool allowPlayerToActivateEnemyPlate = false;

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

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip activateClip;
    [SerializeField] private AudioClip deactivateClip;
    [Range(0f, 1f)][SerializeField] private float activateVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float deactivateVolume = 1f;

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
        InitUI();
        CacheVisual();
        RefreshIndicator();
        AutoFindDetectionAreas();
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void OnEnable()
    {
        if (!allPlates.Contains(this)) allPlates.Add(this);
        RefreshUI();
        RefreshIndicator();
        AutoFindDetectionAreas();
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
                    SetActivated(false);
                }
            }
            RefreshUI();
            UpdatePlateVisual(false);
            return;
        }

        if (hasActiveOccupant)
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
            if (capturedRoot != null) return true;
            if (allowPlayerToActivateEnemyPlate && qualifyingOccupants > 0) return true;
            return false;
        }
    }

    public void HandleAreaTriggerEnter(PressurePlateDetectionArea area, Collider2D other)
    {
        if (area == null || other == null) return;

        if (area == playerDetectionArea && IsPlayerOrEnemy(other))
        {
            qualifyingOccupants++;
        }

        if (area == enemyDetectionArea && plateType == PlateType.EnemySpecific)
        {
            if (IsAllowedEnemy(other))
            {
                overlappedEnemyCandidates.Add(other);
            }
            else
            {
                TryReleaseCapturedEnemyByProjectile(other);
            }
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

    private void TryReleaseCapturedEnemyByProjectile(Collider2D other)
    {
        if (capturedRoot == null) return;
        if (other == null) return;

        var proj = other.GetComponentInParent<Projectile>();
        if (proj == null) return;

        var payload = other.GetComponent<GeneratorChargePayload>();
        if (payload == null)
        {
            payload = other.GetComponentInParent<GeneratorChargePayload>();
        }

        bool isReleaseShot = false;
        if (proj.projectileType == Projectile.ProjectileType.Ice)
        {
            if (payload != null && payload.chargeSign < 0)
            {
                isReleaseShot = true;
            }
        }

        if (!isReleaseShot) return;

        ReleaseCapturedEnemyInternal(false);
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
            SetActivated(newActive);
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
                var s = lineSliders[i];
                if (s != null)
                {
                    s.maxValue = 1f;
                    s.value = 0f;
                }
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

                float segment = Mathf.Clamp01(scaled - i);
                s.value = segment;
            }
        }
    }

    private void RefreshIndicator()
    {
        if (indicator == null) return;
        indicator.color = IsActivated ? activeColor : inactiveColor;
    }

    private void CacheVisual()
    {
        if (hideMode == HideMode.DisableTilemapRenderer && targetTilemap != null)
        {
            cachedTilemapRenderer = targetTilemap.GetComponent<TilemapRenderer>();
        }

        if (hideMode == HideMode.ClearSpecificCells && targetTilemap != null)
        {
            if ((cellsToToggle == null || cellsToToggle.Length == 0) && autoUseCellFromTransform)
            {
                Vector3Int cell = targetTilemap.WorldToCell(transform.position);
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

        if (hideMode == HideMode.DisableGameObject && visualGameObjectOverride == null)
        {
            visualGameObjectOverride = gameObject;
        }
    }

    private void UpdatePlateVisual(bool shouldHide)
    {
        if (shouldHide == visualHidden) return;

        visualHidden = shouldHide;

        if (visualHidden)
        {
            HideVisual();
        }
        else
        {
            ShowVisual();
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

    private bool IsPlayerOrEnemy(Collider2D c)
    {
        if (c == null) return false;

        bool isPlayer = false;
        bool isEnemy = false;

        if (c.CompareTag("Player") || c.GetComponentInParent<PlayerController>() != null)
        {
            isPlayer = true;
        }

        if (c.CompareTag("Enemy") || c.GetComponentInParent<Enemy>() != null)
        {
            isEnemy = true;
        }

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

        if (parentCapturedEnemyToAnchor && captureAnchor != null)
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

    private void SetActivated(bool value)
    {
        if (IsActivated == value) return;
        IsActivated = value;
        if (IsActivated)
        {
            PlayActivateSound();
        }
        else
        {
            PlayDeactivateSound();
        }
        OnActivatedChanged?.Invoke(IsActivated);
        RefreshIndicator();
    }

    private void PlayActivateSound()
    {
        if (audioSource == null) return;
        if (activateClip == null) return;
        audioSource.Stop();
        audioSource.PlayOneShot(activateClip, activateVolume);
    }

    private void PlayDeactivateSound()
    {
        if (audioSource == null) return;
        if (deactivateClip == null) return;
        audioSource.Stop();
        audioSource.PlayOneShot(deactivateClip, deactivateVolume);
    }
}
