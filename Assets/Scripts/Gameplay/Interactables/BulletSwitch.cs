using UnityEngine;

public class BulletSwitch : MonoBehaviour
{
    [SerializeField] private string requiredTag = "Fire";
    [SerializeField] private SpriteRenderer indicator;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.green;
    public bool IsActivated { get; private set; }

    void Awake()
    {
        if (indicator != null) indicator.color = inactiveColor;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsActivated) return;
        if (!other.TryGetComponent<Projectile>(out _)) return;
        if (!other.CompareTag(requiredTag)) return;

        IsActivated = true;
        if (indicator != null) indicator.color = activeColor;
    }
}
