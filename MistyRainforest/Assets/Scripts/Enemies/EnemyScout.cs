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
    [SerializeField] private float verticalTolerance = 0.75f;    // max |dy| to allow detection
    [SerializeField] private bool useLineOfSight = true;         // raycast through blockers
    [SerializeField] private LayerMask losBlockers;              // set to Platforms/Ground, NOT Player
    [SerializeField] private float loseSightLinger = 0.4f;       // hysteresis after LoS lost
    [SerializeField] private bool debugLog = false;

    [Header("Contact Handling (no shove)")]
    [SerializeField] private float touchEpsilon = 0.03f;
    [SerializeField] private float microStepSpeed = 0.8f;

    public static System.Action<EnemyScout> AnyEnemyDied;
    public bool IsDead { get; private set; }
    [SerializeField] private float destroyDelay = 0.05f;

    // Track attack coroutine
    private Coroutine attackRoutine;

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

    // Audio (merfolk death)
    private Level1AudioManager audioManager;

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

        GameObject musicObject = GameObject.Find("Level1AudioManager");
        if (musicObject != null)
        {
            audioManager = musicObject.GetComponent<Level1AudioManager>();
            Debug.Log("Audio manager found!");
        }
        else
    {
        Debug.LogWarning("Level1AudioManager GameObject not found!");
    }
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

    void DoChase()
    {
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

        if (animator != null)
        {
            animator.SetTrigger("attack");
        }

        attackRoutine = StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        attacking = true;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        yield return new WaitForSeconds(attackWindup);

        if (attackHitbox) attackHitbox.enabled = true;
        yield return new WaitForSeconds(attackActiveTime);
        if (attackHitbox) attackHitbox.enabled = false;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        attacking = false;
        cooldownEndTime = Time.time + attackCooldown;
        SetState(State.Cooldown);

        attackRoutine = null;
    }

    void DoCooldown()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (Time.time >= cooldownEndTime)
            SetState(CanDetectPlayer(out _) ? State.Chase : State.Patrol);
    }

    bool CanDetectPlayer(out float dxAbs)
    {
        dxAbs = 999f;
        if (!player) return false;

        Vector2 me = transform.position;
        Vector2 pl = player.position;

        float dyAbs = Mathf.Abs(pl.y - me.y);
        dxAbs = Mathf.Abs(pl.x - me.x);

        if (dxAbs > aggroDistance) return false;
        if (dyAbs > verticalTolerance) return false;

        if (requireSamePlatform)
        {
            if (playerPlatform == null || selfPlatform == null) return false;
            if (!selfPlatform.IsOnSamePlatformAs(playerPlatform)) return false;
        }

        if (useLineOfSight)
        {
            Vector2 dir = (pl - me).normalized;
            float dist = Vector2.Distance(pl, me);

            RaycastHit2D hit = Physics2D.Raycast(me, dir, dist, losBlockers);
            if (hit.collider != null)
            {
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
            return d.distance;
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
        Gizmos.DrawLine(transform.position + Vector3.up * verticalTolerance, transform.position + Vector3.right * aggroDistance + Vector3.up * verticalTolerance);
        Gizmos.DrawLine(transform.position - Vector3.right * aggroDistance + Vector3.up * verticalTolerance, transform.position + Vector3.right * aggroDistance + Vector3.up * verticalTolerance);
        Gizmos.DrawLine(transform.position - Vector3.right * aggroDistance - Vector3.up * verticalTolerance, transform.position + Vector3.right * aggroDistance - Vector3.up * verticalTolerance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }


    public void Die(string cause = "DeathZone")
    {
        if (IsDead) return;
        IsDead = true;

        // Animation
        if (animator) animator.SetTrigger("merfolk_dead");


Debug.Log("[EnemyScout] Die() called - attempting to play damage sound");
if (audioManager != null)
{
    Debug.Log("[EnemyScout] audioManager found");
    if (audioManager.damageSound != null)
    {
        Debug.Log("[EnemyScout] damageSound assigned, playing now!");
        audioManager.PlaySFX(audioManager.damageSound);
    }
    else
    {
        Debug.LogError("[EnemyScout] damageSound is NULL!");
    }
}
else
{
    Debug.LogError("[EnemyScout] audioManager is NULL!");
}
        
        if (attackHitbox) attackHitbox.enabled = false;
        if (animator) animator.SetBool("isChasing", false);
        if (sr) sr.enabled = false;
        if (selfCol) selfCol.enabled = false;
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        AnyEnemyDied?.Invoke(this);

        Destroy(gameObject, destroyDelay);
    }
}
