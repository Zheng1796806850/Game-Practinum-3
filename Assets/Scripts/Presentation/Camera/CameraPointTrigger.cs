using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CameraPointTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cam == null) return;
        cam.OnTriggerEntered(transform);
    }
}
