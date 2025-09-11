using UnityEngine;

public class DoorLinearOpener : MonoBehaviour
{
    [SerializeField] private float openDistance = 4f;
    [SerializeField] private float openSpeed = 3f;
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
        if (!IsOpen) return;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, openSpeed * Time.deltaTime);
    }

    public void Open()
    {
        IsOpen = true;
    }
}
