using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAnimatorBinder : MonoBehaviour
{
    public Animator animator;
    public string moveBoolParam = "IsMoving";
    public string stunnedBoolParam = "IsStunned";
    public float speedThreshold = 0.05f;
    public bool useHorizontalSpeedOnly = true;
    public Health health;

    private Rigidbody2D rb;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>() ?? GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        if (health == null) health = GetComponent<Health>();
    }

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>() ?? GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        if (health == null) health = GetComponent<Health>();
    }

    void Update()
    {
        if (animator == null || rb == null) return;

        bool stunned = false;
        if (health != null)
        {
            stunned = health.IsFrozen;
        }

        if (!string.IsNullOrEmpty(stunnedBoolParam))
        {
            animator.SetBool(stunnedBoolParam, stunned);
        }

        if (stunned)
        {
            if (!string.IsNullOrEmpty(moveBoolParam))
            {
                animator.SetBool(moveBoolParam, false);
            }
            return;
        }

        float speed = useHorizontalSpeedOnly ? Mathf.Abs(rb.linearVelocity.x) : rb.linearVelocity.magnitude;
        bool moving = speed >= speedThreshold;

        if (!string.IsNullOrEmpty(moveBoolParam))
        {
            animator.SetBool(moveBoolParam, moving);
        }
    }
}
