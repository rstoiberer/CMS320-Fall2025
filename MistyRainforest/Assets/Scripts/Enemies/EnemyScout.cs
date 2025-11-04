using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyScout : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private float speed = 2f;          // units/second
    [SerializeField] private float patrolDistance = 2f; // half-length from the start point
    [SerializeField] private bool startFacingRight = true;
    [SerializeField] private float pauseAtEnds = 0.15f; // optional pause when turning

    private Rigidbody2D rb;
    private float startX;
    private int dir;            // +1 right, -1 left
    private bool pausing;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startX = transform.position.x;
        dir = startFacingRight ? 1 : -1;
    }

    void FixedUpdate()
    {
        if (pausing || rb == null || !rb.simulated) return;

        // move along X
        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);

        // bounds check
        float left = startX - patrolDistance;
        float right = startX + patrolDistance;

        if ((dir > 0 && transform.position.x >= right) ||
            (dir < 0 && transform.position.x <= left))
        {
            StartCoroutine(Turn());
        }
    }

    System.Collections.IEnumerator Turn()
    {
        pausing = true;
        // stop horizontal motion for a tick (feels nicer)
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (pauseAtEnds > 0f) yield return new WaitForSeconds(pauseAtEnds);
        dir *= -1;
        pausing = false;
    }
}
