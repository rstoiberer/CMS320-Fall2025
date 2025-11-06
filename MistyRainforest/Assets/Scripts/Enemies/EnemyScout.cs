using UnityEngine;
using System.Linq;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PlatformDetector))]
[DisallowMultipleComponent]
public class EnemyScout : MonoBehaviour
{
    public enum State { Patrol, Chase, Attack, Cooldown }

    [Header("Patrol")]
    [SerializeField] private float speed = 0.5f;
    [SerializeField] private float patrolDistance = 2f;   // half-length from startX
    [SerializeField] private bool startFacingRight = true;
    [SerializeField] private float pauseAtEnds = 0.15f;

    [Header("Player (auto-wired if left empty)")]
    [SerializeField] private Transform player;                 // Komea Transform
    [SerializeField] private PlatformDetector playerPlatform;  // Komea PlatformDetector

    [Header("Chase / Attack")]
    [SerializeField] private float aggroDistance = 7f;
    [SerializeField] private float chaseSpeed = 2.0f;
    [SerializeField] private float accel = 15f;
    [SerializeField] private float preferredStopDistance = 0.10f; // tuned to ensure overlap
    [SerializeField] private float attackRange = 0.50f;          // gizmo only
    [SerializeField] private float reactionDelayOnAggro = 0.25f;
    [SerializeField] private float attackWindup = 0.18f;         // tuned
    [SerializeField] private float attackActiveTime = 0.15f;     // tuned
    [SerializeField] private float attackCooldown = 0.50f;       // tuned
    [SerializeField] private AttackHitbox attackHitbox;          // child trigger; assign per enemy
    [SerializeField] private Animator animator;

    [Header("Detection Options")]
    [SerializeField] private bool requireSamePlatform = true;
    [SerializeField] private bool debugLog = false;

    [Header("Contact Handling (no shove)")]
    [SerializeField] private float touchEpsilon = 0.03f;   // <= touching/overlapping
    [SerializeField] private float microStepSpeed = 0.8f;  // creep when very close

    // components / helpers
    private Rigidbody2D rb;
    private PlatformDetector selfPlatform;
    private Collider2D selfCol;
    private Collider2D playerCol;

    // patrol state
    private float startX;
    private int dir;             // +1 right, -1 left
    private bool pausing;

    // fsm
    private State state = State.Patrol;
    private bool attacking;
    private float cooldownEndTime;
    private float aggroReadyTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        selfPlatform = GetComponent<PlatformDetector>();
        selfCol = GetComponent<Collider2D>();

        // Make absolutely sure the BODY stays solid.
        if (selfCol) selfCol.isTrigger = false;

        startX = transform.position.x;
        dir = startFacingRight ? 1 : -1;

        // Auto-wire player and its PlatformDetector / Collider
        if (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj) player = pObj.transform;
        }

        if (player != null)
        {
            playerCol = player.GetComponent<Collider2D>() ??
                        player.GetComponentInChildren<Collider2D>(true);
            if (playerPlatform == null)
            {
                playerPlatform = player.GetComponent<PlatformDetector>() ??
                                  player.GetComponentsInChildren<PlatformDetector>(true).FirstOrDefault();
            }
        }

        if (attackHitbox != null) attackHitbox.enabled = false; // only during active frames
    }

    void Start()
    {
        // Late auto-wire in case player spawned later
        if (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj) player = pObj.transform;
        }
        if (player != null && playerCol == null)
            playerCol = player.GetComponent<Collider2D>() ?? player.GetComponentInChildren<Collider2D>(true);
        if (player != null && playerPlatform == null)
            playerPlatform = player.GetComponent<PlatformDetector>() ??
                             player.GetComponentsInChildren<PlatformDetector>(true).FirstOrDefault();
    }

    void FixedUpdate()
    {
        switch (state)
        {
            case State.Patrol:   DoPatrol();   break;
            case State.Chase:    DoChase();    break;
            case State.Attack:   /* coroutine drives it */ break;
            case State.Cooldown: DoCooldown(); break;
        }
    }

    // ---------------- PATROL ----------------
    void DoPatrol()
    {
        if (pausing || rb == null || !rb.simulated) return;

        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);

        float left = startX - patrolDistance;
        float right = startX + patrolDistance;
        if ((dir > 0 && transform.position.x >= right) ||
            (dir < 0 && transform.position.x <= left))
        {
            StartCoroutine(Turn());
        }

        Face(dir);
        SetAnim(speed: Mathf.Abs(rb.linearVelocity.x), chasing: false);

        if (CanSeePlayerOnSamePlatform(out float dxAbs))
        {
            aggroReadyTime = Time.time + reactionDelayOnAggro;
            SetState((dxAbs <= attackRange) ? State.Attack : State.Chase);
        }
    }

    IEnumerator Turn()
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
        if (!CanSeePlayerOnSamePlatform(out _))
        {
            SetState(State.Patrol);
            return;
        }

        if (Time.time < aggroReadyTime)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            SetAnim(speed: 0f, chasing: true);
            return;
        }

        float dx = player.position.x - transform.position.x;
        float sign = Mathf.Sign(dx);
        Face(sign);

        float sep = HorizontalSeparationToPlayer();

        // 1) Touching/overlapping -> stop & ATTACK (no Y-velocity check anymore)
        if (sep <= touchEpsilon)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (!attacking) StartAttack();
            SetAnim(speed: 0f, chasing: true);
            return;
        }

        // 2) Close but not touching -> micro creep (prevents body-check shove)
        if (sep <= preferredStopDistance)
        {
            float vxMicro = Mathf.MoveTowards(rb.linearVelocity.x, sign * microStepSpeed, accel * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(vxMicro, rb.linearVelocity.y);
            SetAnim(speed: Mathf.Abs(rb.linearVelocity.x), chasing: true);
            return;
        }

        // 3) Normal chase
        float targetVx = sign * chaseSpeed;
        float vx = Mathf.MoveTowards(rb.linearVelocity.x, targetVx, accel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        SetAnim(speed: Mathf.Abs(rb.linearVelocity.x), chasing: true);
    }

    // ---------------- ATTACK ----------------
    void StartAttack()
    {
        if (attacking) return;

        // Keep attack point in FRONT of facing, even after flips/prefab overrides
        if (attackHitbox)
        {
            var t = attackHitbox.transform;
            var p = t.localPosition;
            p.x = Mathf.Abs(p.x) * Mathf.Sign(transform.localScale.x);
            t.localPosition = p;
        }

        if (debugLog) Debug.Log($"[EnemyScout] {name} StartAttack");

        SetState(State.Attack);
        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        attacking = true;
        SetAnim(attack: true);

        // Freeze horizontal during wind-up/active to avoid slide-shove
        var savedVx = rb.linearVelocity.x;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        yield return new WaitForSeconds(attackWindup);

        if (attackHitbox) attackHitbox.enabled = true;
        yield return new WaitForSeconds(attackActiveTime);
        if (attackHitbox) attackHitbox.enabled = false;

        // brief stop after hitbox ends
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        attacking = false;
        cooldownEndTime = Time.time + attackCooldown;
        SetState(State.Cooldown);
    }

    // ---------------- COOLDOWN ----------------
    void DoCooldown()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (Time.time >= cooldownEndTime)
            SetState(CanSeePlayerOnSamePlatform(out _) ? State.Chase : State.Patrol);
    }

    // ---------------- HELPERS ----------------
    bool CanSeePlayerOnSamePlatform(out float dxAbs)
    {
        dxAbs = 999f;
        if (player == null) return false;

        dxAbs = Mathf.Abs(player.position.x - transform.position.x);

        if (!requireSamePlatform)
            return dxAbs <= aggroDistance;

        if (playerPlatform == null || selfPlatform == null) return false;
        bool same = selfPlatform.IsOnSamePlatformAs(playerPlatform);
        return same && dxAbs <= aggroDistance;
    }

    float HorizontalSeparationToPlayer()
    {
        // True collider separation (<= 0 means penetrating)
        if (selfCol != null && playerCol != null)
        {
            var d = Physics2D.Distance(selfCol, playerCol);
            return d.distance;
        }

        // Fallback: center distance (less accurate)
        return Mathf.Abs(player.position.x - transform.position.x);
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

    void SetState(State s)
    {
        if (debugLog && s != state) Debug.Log($"[EnemyScout] {name} -> {s}");
        state = s;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, aggroDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
