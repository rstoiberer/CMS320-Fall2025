using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private Transform attackPoint;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float attackRange = 0.6f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float attacksPerSecond = 3f;

    private float nextAttackTime;
    private Vector3 baseLocalPos;

    private void Awake()
    {
        baseLocalPos = attackPoint.localPosition; // remember initial offset
        // make sure X is positive so mirroring works cleanly
        baseLocalPos.x = Mathf.Abs(baseLocalPos.x);
        attackPoint.localPosition = baseLocalPos;
    }

    private void LateUpdate()
    {
        float dir = sr.flipX ? 1f : -1f; 
        attackPoint.localPosition = new Vector3
        (
            baseLocalPos.x * dir, baseLocalPos.y, baseLocalPos.z
        );
    }


    private void Update()
    {
        if (Time.time >= nextAttackTime && Input.GetKeyDown(KeyCode.J))
        {
            DoHit();
            nextAttackTime = Time.time + 1f / attacksPerSecond;
        }
    }

    private void DoHit()
    {
        var hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (var h in hits)
            if (h.TryGetComponent<EnemyHealth>(out var hp))
                hp.TakeDamage(damage);
    }

    private void OnDrawGizmosSelected()
    {
        if (!attackPoint) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
