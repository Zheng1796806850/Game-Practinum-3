using UnityEngine;

public class PlayerAnimatorBinder : MonoBehaviour
{
    public Animator animator;
    public string speedParam = "Speed";
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>() ?? GetComponent<Animator>();
    }

    void Update()
    {
        if (animator == null) return;
        bool moving = Input.GetKey(leftKey) || Input.GetKey(rightKey);
        animator.SetFloat(speedParam, moving ? 1f : 0f);
    }
}
