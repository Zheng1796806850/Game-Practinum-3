using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    private Rigidbody2D rb;
    private bool isGrounded;
    private bool facingRight = true;

    [Header("Flip Settings")]
    public Transform graphics;
    public Transform[] flipChildren;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (moveInput > 0 && !facingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && facingRight)
        {
            Flip();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        isGrounded = false;
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
}
