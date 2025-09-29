using System.Collections.Generic;
using UnityEngine;

public class PressurePlateSwitch : MonoBehaviour
{
    [SerializeField] private float requiredMass = 1f;
    [SerializeField] private SpriteRenderer indicator;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private bool autoReset = true;

    public bool IsActivated { get; private set; }

    private HashSet<Rigidbody2D> bodies = new HashSet<Rigidbody2D>();

    void Awake()
    {
        RefreshColor();
    }

    void Update()
    {
        float total = 0f;
        foreach (var rb in bodies)
        {
            if (rb != null) total += rb.mass;
        }

        bool shouldActivate = total >= requiredMass;

        if (autoReset)
        {
            if (shouldActivate != IsActivated)
            {
                IsActivated = shouldActivate;
                RefreshColor();
            }
        }
        else
        {
            if (!IsActivated && shouldActivate)
            {
                IsActivated = true;
                RefreshColor();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.attachedRigidbody != null)
            bodies.Add(other.attachedRigidbody);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.attachedRigidbody != null)
            bodies.Remove(other.attachedRigidbody);
    }

    private void RefreshColor()
    {
        if (indicator != null)
            indicator.color = IsActivated ? activeColor : inactiveColor;
    }
}
