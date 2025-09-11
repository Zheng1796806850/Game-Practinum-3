using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;

    [Header("Camera Boundaries")]
    public Vector2 minPosition;
    public Vector2 maxPosition;

    private Vector3 velocity = Vector3.zero;
    private void FixedUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);

        float clampedX = Mathf.Clamp(smoothedPosition.x, minPosition.x, maxPosition.x);
        float clampedY = Mathf.Clamp(smoothedPosition.y, minPosition.y, maxPosition.y);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }

}
