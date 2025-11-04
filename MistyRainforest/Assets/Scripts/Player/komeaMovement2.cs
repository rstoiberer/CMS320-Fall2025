using UnityEngine;

public class KomeaMovement2 : MonoBehaviour
{
    private Rigidbody2D body;
    private SpriteRenderer sr;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;

    [Header("Facing")]
    [SerializeField] private bool artworkFacesRight = true;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;     // empty child under feet
    [SerializeField] private float groundRadius = 0.1f; // small circle
    [SerializeField] private LayerMask groundLayer;     // set to your Ground layer

    private float moveInput;
    private bool isGrounded;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.freezeRotation = true;
    }

    private void Update()
    {
        // 1) Read input
        moveInput = Input.GetAxisRaw("Horizontal");

        // 2) Face the move direction (flipX only; no scale flipping)
        //    If your artwork faces RIGHT by default → artworkFacesRight = true
        //    If it faces LEFT  by default → artworkFacesRight = false
        if (moveInput > 0.01f)          // moving right
            sr.flipX = !artworkFacesRight;
        else if (moveInput < -0.01f)    // moving left
            sr.flipX = artworkFacesRight;
        // (if ~0, keep current facing)

        // 3) Jump only when grounded
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
    }


    private void FixedUpdate()
    {
        // Move
        body.linearVelocity = new Vector2(moveInput * moveSpeed, body.linearVelocity.y);

        // Ground check (physics step)
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
    }

    // Optional: visualize ground check in Scene view
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}
