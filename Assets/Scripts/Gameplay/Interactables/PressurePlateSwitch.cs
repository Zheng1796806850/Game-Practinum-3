using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PressurePlateSwitch : MonoBehaviour, ISwitch
{
    [SerializeField] private float requiredMass = 1f;
    [SerializeField] private SpriteRenderer indicator;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private bool autoReset = true;

    [Header("Sequence")]
    [SerializeField] private bool useSequentialMode = false;
    [SerializeField] private MonoBehaviour[] prerequisites;

    [Header("Visual Hide")]
    [SerializeField] private bool hideWhenPlayerOrEnemyOnTop = true;
    [SerializeField] private HideMode hideMode = HideMode.ClearSpecificCells;
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private bool autoUseCellFromTransform = true;
    [SerializeField] private Vector3Int[] cellsToToggle;
    [SerializeField] private GameObject visualGameObjectOverride;

    public enum HideMode { None, DisableTilemapRenderer, ClearSpecificCells, DisableGameObject }

    public bool IsActivated { get; private set; }
    public event Action<bool> OnActivatedChanged;

    private HashSet<Rigidbody2D> bodies = new HashSet<Rigidbody2D>();
    private int qualifyingOccupants;
    private TilemapRenderer cachedTilemapRenderer;
    private TileBase[] cachedTiles;
    private bool visualHidden;

    void Awake()
    {
        PrepareVisualCache();
        RefreshColor();
    }

    void Update()
    {
        float total = 0f;
        foreach (var rb in bodies)
        {
            if (rb != null) total += rb.mass;
        }

        bool baseShouldActivate = total >= requiredMass;
        bool prereqsMet = !useSequentialMode || ArePrerequisitesMet();
        bool shouldActivate = baseShouldActivate && prereqsMet;

        if (autoReset)
        {
            SetActivated(shouldActivate);
        }
        else
        {
            if (!IsActivated && shouldActivate) SetActivated(true);
            if (IsActivated && (!baseShouldActivate || !prereqsMet)) SetActivated(false);
        }

        UpdatePlateVisual(hideWhenPlayerOrEnemyOnTop && qualifyingOccupants > 0);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.attachedRigidbody != null)
            bodies.Add(other.attachedRigidbody);
        if (IsPlayerOrEnemy(other))
            qualifyingOccupants++;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.attachedRigidbody != null)
            bodies.Remove(other.attachedRigidbody);
        if (IsPlayerOrEnemy(other))
            qualifyingOccupants = Mathf.Max(0, qualifyingOccupants - 1);
    }

    private void SetActivated(bool v)
    {
        if (IsActivated == v) return;
        IsActivated = v;
        RefreshColor();
        OnActivatedChanged?.Invoke(IsActivated);
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

    private void RefreshColor()
    {
        if (indicator != null)
            indicator.color = IsActivated ? activeColor : inactiveColor;
    }

    private bool IsPlayerOrEnemy(Collider2D c)
    {
        if (c == null) return false;
        if (c.CompareTag("Player") || c.CompareTag("Enemy")) return true;
        if (c.GetComponentInParent<PlayerController>() != null) return true;
        if (c.GetComponentInParent<Enemy>() != null) return true;
        return false;
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
}
