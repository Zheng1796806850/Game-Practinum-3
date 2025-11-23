using UnityEngine;

public class DoorLinearOpener : MonoBehaviour
{
    public enum OpenDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    [Header("Movement Settings")]
    [SerializeField] private OpenDirection openDirection = OpenDirection.Up;
    [SerializeField] private float openDistance = 4f;
    [SerializeField] private float openSpeed = 3f;
    [SerializeField] private float closeSpeed = 3f;

    [Header("Editor Preview")]
    [SerializeField] private float previewDuration = 1f;
    [SerializeField] private float holdDuration = 1f;

    public bool IsOpen { get; private set; }

    private Vector3 startPos;
    private Vector3 targetPos;

    public float OpenDistance => openDistance;
    public Vector3 DirectionVector => GetDirectionVector();
    public float PreviewDuration => previewDuration;

    public float HoldDuration => holdDuration;

    void Awake()
    {
        startPos = transform.position;
        targetPos = startPos + GetDirectionVector() * openDistance;
    }

    void Update()
    {
        Vector3 goal = IsOpen ? targetPos : startPos;
        float spd = IsOpen ? openSpeed : closeSpeed;
        transform.position = Vector3.MoveTowards(transform.position, goal, spd * Time.deltaTime);
    }

    public void Open()
    {
        IsOpen = true;
    }

    public void Close()
    {
        IsOpen = false;
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
    }

    private Vector3 GetDirectionVector()
    {
        switch (openDirection)
        {
            case OpenDirection.Up: return Vector3.up;
            case OpenDirection.Down: return Vector3.down;
            case OpenDirection.Left: return Vector3.left;
            case OpenDirection.Right: return Vector3.right;
            default: return Vector3.up;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 s = Application.isPlaying ? startPos : transform.position;
        Vector3 t = s + GetDirectionVector() * openDistance;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(s, t);
        Gizmos.DrawWireSphere(s, 0.1f);
        Gizmos.DrawWireSphere(t, 0.1f);
    }
}
