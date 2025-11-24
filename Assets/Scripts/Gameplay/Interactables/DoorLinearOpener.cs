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

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [Range(0f, 1f)][SerializeField] private float openVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float closeVolume = 1f;

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

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void Update()
    {
        Vector3 goal = IsOpen ? targetPos : startPos;
        float spd = IsOpen ? openSpeed : closeSpeed;
        transform.position = Vector3.MoveTowards(transform.position, goal, spd * Time.deltaTime);
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        PlayOpenSound();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        PlayCloseSound();
    }

    public void SetOpen(bool open)
    {
        if (open)
        {
            if (!IsOpen)
            {
                IsOpen = true;
                PlayOpenSound();
            }
        }
        else
        {
            if (IsOpen)
            {
                IsOpen = false;
                PlayCloseSound();
            }
        }
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

    private void PlayOpenSound()
    {
        if (audioSource == null) return;
        if (openClip == null) return;
        audioSource.Stop();
        audioSource.PlayOneShot(openClip, openVolume);
    }

    private void PlayCloseSound()
    {
        if (audioSource == null) return;
        if (closeClip == null) return;
        audioSource.Stop();
        audioSource.PlayOneShot(closeClip, closeVolume);
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
