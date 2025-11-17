using UnityEngine;

public class PressurePlateDetectionArea : MonoBehaviour
{
    public enum AreaKind
    {
        Player,
        Enemy
    }

    [SerializeField] private AreaKind kind = AreaKind.Player;
    [SerializeField] private PressurePlateSwitch owner;

    public AreaKind Kind => kind;
    public PressurePlateSwitch Owner => owner;

    void Reset()
    {
        if (owner == null) owner = GetComponentInParent<PressurePlateSwitch>();
    }

    void Awake()
    {
        if (owner == null) owner = GetComponentInParent<PressurePlateSwitch>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null) owner.HandleAreaTriggerEnter(this, other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (owner != null) owner.HandleAreaTriggerExit(this, other);
    }
}