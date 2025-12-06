using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class AttackHitbox : MonoBehaviour
{
    [Tooltip("Must include 'Player' layer (or leave 0 to accept any).")]
    public LayerMask targetMask;
    public int damage = 1;

    Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        
        bool maskAllows = (targetMask.value == 0) ||
                          ((targetMask.value & (1 << other.gameObject.layer)) != 0);
        if (!maskAllows) return;

        
        var playerOneShot = other.GetComponent<KomeaOneShotDeath>() ??
                            other.GetComponentInParent<KomeaOneShotDeath>();

        if (playerOneShot != null)
        {
            // Debug.Log($"[AttackHitbox] {transform.root.name} hit {other.name}");
            playerOneShot.KillPlayer();
        }
    }
}
