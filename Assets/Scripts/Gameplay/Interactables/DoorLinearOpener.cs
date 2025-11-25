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

    [Header("Options")]
    [SerializeField] private bool loopMoveSounds = false;
    [SerializeField] private bool lockWhenFullyOpen = false;

    public bool IsOpen { get; private set; }

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool permanentlyOpen;

    public float OpenDistance => openDistance;
    public Vector3 DirectionVector => GetDirectionVector();
    public float PreviewDuration => previewDuration;
    public float HoldDuration => holdDuration;

    void Awake()
    {
        startPos = transform.position;
        targetPos = startPos + GetDirectionVector() * openDistance;
        permanentlyOpen = false;

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

        bool atGoal = (transform.position - goal).sqrMagnitude <= 0.0001f;

        if (loopMoveSounds && audioSource != null && audioSource.loop && audioSource.isPlaying)
        {
            if (atGoal)
            {
                audioSource.loop = false;
                audioSource.Stop();
            }
        }

        if (lockWhenFullyOpen && !permanentlyOpen)
        {
            if (IsOpen && atGoal && (transform.position - targetPos).sqrMagnitude <= 0.0001f)
            {
                permanentlyOpen = true;
            }
        }
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
        if (lockWhenFullyOpen && permanentlyOpen) return;
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
            if (!IsOpen) return;
            if (lockWhenFullyOpen && permanentlyOpen) return;
            IsOpen = false;
            PlayCloseSound();
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

        if (loopMoveSounds)
        {
            audioSource.loop = true;
            audioSource.clip = openClip;
            audioSource.volume = openVolume;
            audioSource.Stop();
            audioSource.Play();
        }
        else
        {
            audioSource.loop = false;
            audioSource.Stop();
            audioSource.PlayOneShot(openClip, openVolume);
        }
    }

    private void PlayCloseSound()
    {
        if (audioSource == null) return;
        if (closeClip == null) return;

        if (loopMoveSounds)
        {
            audioSource.loop = true;
            audioSource.clip = closeClip;
            audioSource.volume = closeVolume;
            audioSource.Stop();
            audioSource.Play();
        }
        else
        {
            audioSource.loop = false;
            audioSource.Stop();
            audioSource.PlayOneShot(closeClip, closeVolume);
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
