using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    private Rigidbody2D rb;
    private bool facingRight = true;

    [Header("Flip Settings")]
    public Transform graphics;
    public Transform[] flipChildren;

    [Header("Ground Check Settings")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    [SerializeField] private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        isGrounded = IsGrounded();

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        UpdateFacingByMouse();
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void UpdateFacingByMouse()
    {
        if (Camera.main == null) return;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        bool shouldFaceRight = mouseWorld.x >= transform.position.x;
        if (shouldFaceRight != facingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;

        if (graphics != null)
        {
            Vector3 scale = graphics.localScale;
            scale.x *= -1;
            graphics.localScale = scale;
        }

        if (flipChildren != null && flipChildren.Length > 0)
        {
            foreach (Transform child in flipChildren)
            {
                Vector3 scale = child.localScale;
                scale.x *= -1;
                child.localScale = scale;
            }
        }
    }

    public bool IsFacingRight()
    {
        return facingRight;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
