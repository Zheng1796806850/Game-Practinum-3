using UnityEngine;

public class CrosshairOffsetRuntimePreview : MonoBehaviour
{
    public Texture2D crosshairTexture;
    public Vector2 offset = Vector2.zero;
    public CursorMode cursorMode = CursorMode.Auto;

    public bool previewInGameView = true;
    public bool showTrueCenterInGameView = true;
    public float trueCenterMarkSize = 10f;

    public bool applyOnValidateInEditor = true;

    private Vector2 appliedHotspot;
    private Texture2D px;

    void Awake()
    {
        if (px == null)
        {
            px = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            px.SetPixel(0, 0, Color.white);
            px.Apply();
        }
    }

    void Start()
    {
        ApplyCursorOffset();
    }

    void OnValidate()
    {
        if (!Application.isPlaying && applyOnValidateInEditor) ApplyCursorOffset();
    }

    void Update()
    {
        if (previewInGameView)
        {
            Cursor.visible = false;
        }
        else
        {
            Cursor.visible = true;
            EnsureCursorApplied();
        }
    }

    void EnsureCursorApplied()
    {
        if (crosshairTexture == null) return;
        Vector2 hs = new Vector2(crosshairTexture.width * 0.5f - offset.x, crosshairTexture.height * 0.5f - offset.y);
        if (hs != appliedHotspot)
        {
            appliedHotspot = hs;
            Cursor.SetCursor(crosshairTexture, appliedHotspot, cursorMode);
        }
    }

    void ApplyCursorOffset()
    {
        if (crosshairTexture == null) return;
        appliedHotspot = new Vector2(crosshairTexture.width * 0.5f - offset.x, crosshairTexture.height * 0.5f - offset.y);
        Cursor.SetCursor(crosshairTexture, appliedHotspot, cursorMode);
    }

    void OnGUI()
    {
        if (crosshairTexture == null) return;

        Vector2 mouse = Input.mousePosition;

        if (previewInGameView)
        {
            Rect r = new Rect(
                mouse.x - appliedHotspot.x,
                Screen.height - mouse.y - crosshairTexture.height + appliedHotspot.y,
                crosshairTexture.width,
                crosshairTexture.height
            );
            GUI.DrawTexture(r, crosshairTexture);
        }

        if (showTrueCenterInGameView)
        {
            float s = Mathf.Max(1f, trueCenterMarkSize);
            DrawLine(new Vector2(mouse.x - s, mouse.y), new Vector2(mouse.x + s, mouse.y), 1f);
            DrawLine(new Vector2(mouse.x, mouse.y - s), new Vector2(mouse.x, mouse.y + s), 1f);
        }
    }

    void DrawLine(Vector2 a, Vector2 b, float thickness)
    {
        if (px == null) return;
        Vector2 dir = b - a;
        float len = dir.magnitude;
        if (len <= 0.0001f) return;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Rect r = new Rect(a.x, Screen.height - a.y, len, thickness);
        Matrix4x4 m = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, new Vector2(a.x, Screen.height - a.y));
        GUI.DrawTexture(r, px);
        GUI.matrix = m;
    }
}
