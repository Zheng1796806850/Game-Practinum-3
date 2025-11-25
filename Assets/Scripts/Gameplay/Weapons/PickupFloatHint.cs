using UnityEngine;

public class PickupFloatHint : MonoBehaviour
{
    public enum FloatAxis
    {
        Vertical,
        Horizontal
    }

    public float amplitude = 0.25f;
    public float frequency = 1f;
    public FloatAxis axis = FloatAxis.Vertical;

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

        if (axis == FloatAxis.Vertical)
        {
            pos.y += offset;
        }
        else
        {
            pos.x += offset;
        }

        transform.localPosition = pos;
    }
}
