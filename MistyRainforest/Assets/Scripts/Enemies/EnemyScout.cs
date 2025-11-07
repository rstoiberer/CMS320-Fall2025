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
    [SerializeField] private float patrolDistance = 2f;
    [SerializeField] private bool startFacingRight = true;
    [SerializeField] private float pauseAtEnds = 0.15f;

    [Header("Player (auto-wired if left empty)")]
    [SerializeField] private Transform player;
    [SerializeField] private PlatformDetector playerPlatform;

    [Header("Chase / Attack")]
    [SerializeField] private float aggroDistance = 7f;       // horizontal reach
    [SerializeField] private float chaseSpeed = 2.0f;
    [SerializeField] private float accel = 15f;
    [SerializeField] private float preferredStopDistance = 0.10f;
    [SerializeField] private float attackRange = 0.50f;      // gizmo only
    [SerializeField] private float reactionDelayOnAggro = 0.25f;
    [SerializeField] private float attackWindup = 0.18f;
    [SerializeField] private float attackActiveTime = 0.15f;
    [SerializeField] private float attackCooldown = 0.50f;
    [SerializeField] private AttackHitbox attackHitbox;      // assign the child
    [SerializeField] private Animator animator;

    [Header("Detection Options")]
    [SerializeField] private bool requireSamePlatform = true;
    [SerializeField] private float verticalTolerance = 0.75f;    // NEW: max |dy| to allow detection
    [SerializeField] private bool useLineOfSight = true;          // NEW: raycast through blockers
    [SerializeField] private LayerMask losBlockers;               // NEW: set to Platforms/Ground, NOT Player
    [SerializeField] private float loseSightLinger = 0.4f;        // NEW: hysteresis after LoS lost
    [SerializeField] private bool debugLog = false;

    [Header("Contact Handling (no shove)")]
    [SerializeField] private float touchEpsilon = 0.03f;
    [SerializeField] private float microStepSpeed = 0.8f;


    // ---------- DEATH HANDLING ----------
    public static System.Action<EnemyScout> AnyEnemyDied;  // lets gate detect when an enemy dies
    public bool IsDead { get; private set; }
    [SerializeField] private float destroyDelay = 0.05f;   // short delay before removing




    // components / helpers
    private Rigidbody2D rb;
    private PlatformDetector selfPlatform;
    private Collider2D selfCol;
    private Collider2D playerCol;
    private SpriteRenderer sr;

    // patrol state
    private float startX;
    private int dir;    // +1 right, -1 left
    private bool pausing;

    // fsm
    private State state = State.Patrol;
    private bool attacking;
    private float cooldownEndTime;
    private float aggroReadyTime;

    // detection hysteresis
    private float lastSeenTime = -999f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        selfPlatform = GetComponent<PlatformDetector>();
        selfCol = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        if (selfCol) selfCol.isTrigger = false;

        startX = transform.position.x;
        dir = startFacingRight ? 1 : -1;
        ApplyFacing(dir);

        // Auto-wire player & helpers
        if (!player)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj) player = pObj.transform;
        }
        if (player)
        {
            playerCol = player.GetComponent<Collider2D>() ??
                        player.GetComponentInChildren<Collider2D>(true);
            if (!playerPlatform)
            {
                playerPlatform = player.GetComponent<PlatformDetector>() ??
                                  player.GetComponentsInChildren<PlatformDetector>(true).FirstOrDefault();
            }
        }

        if (attackHitbox) attackHitbox.enabled = false;
    }

    void Start()
    {
        if (!player)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj) player = pObj.transform;
        }
        if (player && !playerCol)
            playerCol = player.GetComponent<Collider2D>() ?? player.GetComponentInChildren<Collider2D>(true);
        if (player && !playerPlatform)
            playerPlatform = player.GetComponent<PlatformDetector>() ??
                             player.GetComponentsInChildren<PlatformDetector>(true).FirstOrDefault();
    }

    void FixedUpdate()
    {
        switch (state)
        {
            case State.Patrol: DoPatrol(); break;
            case State.Chase: DoChase(); break;
            case State.Attack: break; // coroutine handles it
            case State.Cooldown: DoCooldown(); break;
        }
    }

    // ---------- PATROL ----------
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

        ApplyFacing(dir);
        SetAnim(speed: Mathf.Abs(rb.linearVelocity.x), chasing: false);

        if (CanDetectPlayer(out float dxAbs))
        {
            lastSeenTime = Time.time;
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

    // ---------- CHASE ----------
    void DoChase()
    {
        // Update last-seen & decide if we should drop aggro
        if (CanDetectPlayer(out _))
        {
            lastSeenTime = Time.time;
        }
        else if (Time.time > lastSeenTime + loseSightLinger)
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
        int sign = dx >= 0 ? 1 : -1;
        ApplyFacing(sign);

        float sep = HorizontalSeparationToPlayer();

        if (sep <= touchEpsilon)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (!attacking) StartAttack();
            SetAnim(speed: 0f, chasing: true);
            return;
        }

        if (sep <= preferredStopDistance)
        {
            float vxMicro = Mathf.MoveTowards(rb.linearVelocity.x, sign * microStepSpeed, accel * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(vxMicro, rb.linearVelocity.y);
            SetAnim(speed: Mathf.Abs(rb.linearVelocity.x), chasing: true);
            return;
        }

        float targetVx = sign * chaseSpeed;
        float vx = Mathf.MoveTowards(rb.linearVelocity.x, targetVx, accel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        SetAnim(speed: Mathf.Abs(rb.linearVelocity.x), chasing: true);
    }

    // ---------- ATTACK ----------
    void StartAttack()
    {
        if (attacking) return;

        if (attackHitbox)
        {
            var t = attackHitbox.transform;
            var p = t.localPosition;
            p.x = Mathf.Abs(p.x) * FacingSign();
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

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        yield return new WaitForSeconds(attackWindup);

        if (attackHitbox) attackHitbox.enabled = true;
        yield return new WaitForSeconds(attackActiveTime);
        if (attackHitbox) attackHitbox.enabled = false;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        attacking = false;
        cooldownEndTime = Time.time + attackCooldown;
        SetState(State.Cooldown);
    }

    // ---------- COOLDOWN ----------
    void DoCooldown()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (Time.time >= cooldownEndTime)
            SetState(CanDetectPlayer(out _) ? State.Chase : State.Patrol);
    }

    // ---------- HELPERS ----------
    // NEW: unified detection with vertical tolerance, platform gate, and optional LOS
    bool CanDetectPlayer(out float dxAbs)
    {
        dxAbs = 999f;
        if (!player) return false;

        Vector2 me = transform.position;
        Vector2 pl = player.position;

        float dyAbs = Mathf.Abs(pl.y - me.y);
        dxAbs = Mathf.Abs(pl.x - me.x);

        // must be within horizontal reach
        if (dxAbs > aggroDistance) return false;

        // must be within vertical band (prevents “directly below” aggro)
        if (dyAbs > verticalTolerance) return false;

        // platform constraint if enabled
        if (requireSamePlatform)
        {
            if (playerPlatform == null || selfPlatform == null) return false;
            if (!selfPlatform.IsOnSamePlatformAs(playerPlatform)) return false;
        }

        // optional line-of-sight: platforms should be on losBlockers
        if (useLineOfSight)
        {
            Vector2 dir = (pl - me).normalized;
            float dist = Vector2.Distance(pl, me);

            // IMPORTANT: losBlockers must NOT include the Player layer
            RaycastHit2D hit = Physics2D.Raycast(me, dir, dist, losBlockers);
            if (hit.collider != null)
            {
                // something blocked the view (platform/wall)
                if (debugLog) Debug.Log($"[EnemyScout] LoS blocked by {hit.collider.name}");
                return false;
            }
        }

        return true;
    }

    float HorizontalSeparationToPlayer()
    {
        if (selfCol && playerCol)
        {
            var d = Physics2D.Distance(selfCol, playerCol);
            return d.distance; // <=0 means penetrating
        }
        return Mathf.Abs(player.position.x - transform.position.x);
    }

    int FacingSign()
    {
        if (sr != null) return sr.flipX ? -1 : 1;
        return transform.localScale.x >= 0 ? 1 : -1;
    }

    void ApplyFacing(float sign)
    {
        int s = sign >= 0 ? 1 : -1;

        if (sr != null)
        {
            sr.flipX = (s < 0);
            var ls = transform.localScale;
            ls.x = Mathf.Abs(ls.x);
            transform.localScale = ls;
        }
        else
        {
            var ls = transform.localScale;
            ls.x = Mathf.Abs(ls.x) * (s > 0 ? 1 : -1);
            transform.localScale = ls;
        }
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
        Gizmos.color = Color.yellow;
        // visualize vertical tolerance band
        Gizmos.DrawLine(transform.position + Vector3.up * verticalTolerance, transform.position + Vector3.right * aggroDistance + Vector3.up * verticalTolerance);
        Gizmos.DrawLine(transform.position - Vector3.right * aggroDistance + Vector3.up * verticalTolerance, transform.position + Vector3.right * aggroDistance + Vector3.up * verticalTolerance);
        Gizmos.DrawLine(transform.position - Vector3.right * aggroDistance - Vector3.up * verticalTolerance, transform.position + Vector3.right * aggroDistance - Vector3.up * verticalTolerance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    
    // ---------- DEATH ----------
    public void Die(string cause = "DeathZone")
    {
        if (IsDead) return;
        IsDead = true;

        // Stop all movement and collisions
        if (attackHitbox) attackHitbox.enabled = false;
        if (animator) animator.SetBool("isChasing", false);
        if (sr) sr.enabled = false;             // hide sprite (optional)
        if (selfCol) selfCol.enabled = false;   // no collisions
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        // Notify gate or others
        AnyEnemyDied?.Invoke(this);

        // Destroy after short delay
        Destroy(gameObject, destroyDelay);
    }

}
