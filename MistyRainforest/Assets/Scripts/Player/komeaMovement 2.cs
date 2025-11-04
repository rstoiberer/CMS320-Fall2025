using UnityEngine;

public class KomeaMovement : MonoBehaviour
{
    private Rigidbody2D body;
    private SpriteRenderer sr;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;

    // Set this to true if your sprite's artwork faces RIGHT by default.
    // Set to false if the artwork faces LEFT by default.
    [SerializeField] private bool artworkFacesRight = true;

    private float moveInput; // store input read in Update()

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>(); // fallback
    }

    private void Update()
    {
        // Read input here (more responsive)
        moveInput = Input.GetAxisRaw("Horizontal");

        // Flip without changing scale/size:
        if (moveInput > 0.01f)
            sr.flipX = !artworkFacesRight;   // moving right
        else if (moveInput < -0.01f)
            sr.flipX = artworkFacesRight;    // moving left

        if (Input.GetKeyDown(KeyCode.Space))
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
    }

    private void FixedUpdate()
    {
        // Apply movement in FixedUpdate for physics
        body.linearVelocity = new Vector2(moveInput * moveSpeed, body.linearVelocity.y);
    }
}
