using UnityEngine;

public class KomeaMovement2 : MonoBehaviour
{
    private Rigidbody2D body;
    private SpriteRenderer sr;
    public Animator animator;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;

    [Header("Facing")]
    [SerializeField] private bool artworkFacesRight = true;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;     // assign child
    [SerializeField] private float groundRadius = 0.12f; // 0.10–0.14
    [SerializeField] private LayerMask groundLayer;     // set to “Ground” in Inspector

    private float moveInput;
    private bool isGrounded;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        sr   = GetComponentInChildren<SpriteRenderer>();

        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.freezeRotation = true; // Freeze Z in Inspector is fine too
    }

    private void Update()
    {
        // 1) Horizontal input
        moveInput = Input.GetAxisRaw("Horizontal");

        // 2) Flip sprite by input
        if (moveInput > 0.01f){
            sr.flipX = !artworkFacesRight;
            animator.SetBool("isRunning", true);
        }        
        else if (moveInput < -0.01f){
            sr.flipX =  artworkFacesRight;
            animator.SetBool("isRunning", true);
        }
        else{
            animator.SetBool("isRunning", false);
        }   

        // 3) Jump only when grounded
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
        }
    }

    private void FixedUpdate()
{
    // Horizontal move
    body.linearVelocity = new Vector2(moveInput * moveSpeed, body.linearVelocity.y);

    // Robust ground detection: overlap + short ray + physics contact
    bool isCircleHit = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

    RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.08f, groundLayer);
    Debug.DrawRay(groundCheck.position, Vector2.down * 0.08f, Color.yellow);

    bool contact = false;
    var col = GetComponent<Collider2D>();
    if (col)
    {
        var cf = new ContactFilter2D();
        cf.SetLayerMask(groundLayer);
        cf.useTriggers = false;
        contact = col.IsTouching(cf);
    }

    isGrounded = isCircleHit || hit.collider != null || contact;
}


    private void OnDrawGizmosSelected()
    {
        if (!groundCheck) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}
