using UnityEngine;

public class PickupFloatHint : MonoBehaviour
{
    public float amplitude = 0.25f;
    public float frequency = 1f;

    private Vector3 startPosition;
    private float timeOffset;

    void Awake()
    {
        startPosition = transform.localPosition;
        timeOffset = Random.Range(0f, 1000f);
    }

    void Update()
    {
        float t = Time.time + timeOffset;
        float offset = Mathf.Sin(t * frequency) * amplitude;
        Vector3 pos = startPosition;
        pos.y += offset;
        transform.localPosition = pos;
    }
}
