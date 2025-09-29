using UnityEngine;

public class DoorLinearOpener : MonoBehaviour
{
    [SerializeField] private float openDistance = 4f;
    [SerializeField] private float openSpeed = 3f;
    [SerializeField] private float closeSpeed = 3f;

    public bool IsOpen { get; private set; }

    private Vector3 startPos;
    private Vector3 targetPos;

    void Awake()
    {
        startPos = transform.position;
        targetPos = startPos + Vector3.up * openDistance;
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
}
