using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlatformDetector))]
public class EnemyScout : MonoBehaviour
{
    public enum State { Patrol, Chase, Attack, Cooldown }

    [Header("Patrol")]
    [SerializeField] private float speed = 2f;          // units/second
    [SerializeField] private float patrolDistance = 2f; // half-length from the start point
    [SerializeField] private bool startFacingRight = true;
    [SerializeField] private float pauseAtEnds = 0.15f; // optional pause when turning

    [Header("Player (auto-wired if left empty)")]
    [SerializeField] private Transform player;                 // Komea
    [SerializeField] private PlatformDetector playerPlatform;  // Komea's PlatformDetector

    [Header("Chase / Attack")]
    [SerializeField] private float aggroDistance = 7f;         // only if same platform
    [SerializeField] private float chaseSpeed = 2.0f;
    [SerializeField] private float accel = 15f;
    [SerializeField] private float preferredStopDistance = 0.9f; // stop slightly outside attack range
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private float reactionDelayOnAggro = 0.25f;
    [SerializeField] private float attackWindup = 0.22f;       // longer than player startup
    [SerializeField] private float attackActiveTime = 0.12f;
    [SerializeField] private float attackCooldown = 0.60f;
    [SerializeField] private AttackHitbox attackHitbox;        // child trigger; enabled only during active frames
    [SerializeField] private Animator animator;                // optional

    private Rigidbody2D rb;
    private PlatformDetector selfPlatform;

    private float startX;
    private int dir;            // +1 right, -1 left
    private bool pausing;

    private State state = State.Patrol;
    private bool attacking;
    private Coroutine attackCR;
    private float cooldownEndTime;
    private float aggroReadyTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        selfPlatform = GetComponent<PlatformDetector>();

        startX = transform.position.x;
        dir = startFacingRight ? 1 : -1;

        // Auto-wire player & playerPlatform if left unassigned (prefab-friendly)
        if (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player"); // Komea must have Tag = "Player"
            if (pObj != null) player = pObj.transform;
        }
        if (playerPlatform == null && player != null)
        {
            playerPlatform = player.GetComponent<PlatformDetector>();
            if (playerPlatform == null)
                playerPlatform = player.GetComponentsInChildren<PlatformDetector>(true).FirstOrDefault();
        }

        if (attackHitbox != null) attackHitbox.enabled = false; // only on during active frames
    }

    void FixedUpdate()
    {
        switch (state)
        {
            case State.Patrol:   DoPatrol();   break;
            case State.Chase:    DoChase();    break;
            case State.Attack:   /* handled by coroutine */ break;
            case State.Cooldown: DoCooldown(); break;
        }
    }

    // ---------------- PATROL ----------------
    void DoPatrol()
    {
        if (pausing || rb == null || !rb.simulated) return;

        // move along X (keep your original patrol)
        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);

        // bounds check
        float left = startX - patrolDistance;
        float right = startX + patrolDistance;

        if ((dir > 0 && transform.position.x >= right) ||
            (dir < 0 && transform.position.x <= left))
        {
            StartCoroutine(Turn());
        }

        Face(dir);
        SetAnim(speed: Mathf.Abs(rb.linearVelocity.x), chasing:false);

        // transition to chase if player is on SAME platform & in range
        if (CanSeePlayerOnSamePlatform(out float dxAbs))
        {
            aggroReadyTime = Time.time + reactionDelayOnAggro;
            state = (dxAbs <= attackRange) ? State.Attack : State.Chase;
        }
    }

    System.Collections.IEnumerator Turn()
    {
        pausing = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (pauseAtEnds > 0f) yield return new WaitForSeconds(pauseAtEnds);
        dir *= -1;
        pausing = false;
    }

    // ---------------- CHASE ----------------
    void DoChase()
    {
        if (!CanSeePlayerOnSamePlatform(out float dxAbs))
        {
            state = State.Patrol;
            return;
        }

        if (Time.time < aggroReadyTime)
        {
            // brief hesitation for fairness
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            SetAnim(speed: 0f, chasing:true);
            return;
        }

        float dx = player.position.x - transform.position.x;
        float sign = Mathf.Sign(dx);

        // Stop just outside attack range to give player a poke window
        if (dxAbs <= preferredStopDistance)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            Face(sign);

            if (dxAbs <= attackRange && Mathf.Abs(rb.linearVelocity.y) < 0.05f)
                StartAttack();

            SetAnim(speed: 0f, chasing:true);
            return;
        }

        float targetVx = sign * chaseSpeed;
        float vx = Mathf.MoveTowards(rb.linearVelocity.x, targetVx, accel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        Face(sign);

        SetAnim(speed: Mathf.Abs(rb.linearVelocity.x), chasing:true);
    }

    // ---------------- ATTACK ----------------
    void StartAttack()
    {
        if (attacking) return;
        state = State.Attack;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        attackCR = StartCoroutine(AttackRoutine());
    }

    System.Collections.IEnumerator AttackRoutine()
    {
        attacking = true;
        SetAnim(attack:true);

        // wind-up: player can interrupt by hitting first
        yield return new WaitForSeconds(attackWindup);

        if (attackHitbox) attackHitbox.enabled = true;
        yield return new WaitForSeconds(attackActiveTime);
        if (attackHitbox) attackHitbox.enabled = false;

        attacking = false;
        cooldownEndTime = Time.time + attackCooldown;
        state = State.Cooldown;
    }

    // ---------------- COOLDOWN ----------------
    void DoCooldown()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (Time.time >= cooldownEndTime)
            state = CanSeePlayerOnSamePlatform(out _) ? State.Chase : State.Patrol;
    }

    // ---------------- HELPERS ----------------
    bool CanSeePlayerOnSamePlatform(out float dxAbs)
    {
        dxAbs = 999f;
        if (player == null || playerPlatform == null || selfPlatform == null) return false;

        dxAbs = Mathf.Abs(player.position.x - transform.position.x);
        bool same = selfPlatform.IsOnSamePlatformAs(playerPlatform);
        return same && dxAbs <= aggroDistance;
    }

    void Face(float sign)
    {
        if (Mathf.Approximately(sign, 0f)) return;
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (sign > 0 ? 1 : -1);
        transform.localScale = s;
    }

    void SetAnim(float speed = 0f, bool chasing = false, bool attack = false)
    {
        if (!animator) return;
        animator.SetFloat("speed", speed);
        animator.SetBool("isChasing", chasing);
        if (attack) animator.SetTrigger("attack");
    }
}
