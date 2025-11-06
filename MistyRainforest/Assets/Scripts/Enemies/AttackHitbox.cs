using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class AttackHitbox : MonoBehaviour
{
    [Tooltip("Must include 'Player' layer (or leave 0 to accept any).")]
    public LayerMask targetMask; // set to Player in Inspector
    public int damage = 1;

    Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        // Ensure it is ALWAYS a trigger, even if prefab checkbox gets flipped.
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Allow if mask is 0 (failsafe) OR other.layer is in mask
        bool maskAllows = (targetMask.value == 0) ||
                          ((targetMask.value & (1 << other.gameObject.layer)) != 0);
        if (!maskAllows) return;

        // Look on the collider and up the hierarchy
        var playerOneShot = other.GetComponent<KomeaOneShotDeath>() ??
                            other.GetComponentInParent<KomeaOneShotDeath>();

        if (playerOneShot != null)
        {
            // Optional debug
            // Debug.Log($"[AttackHitbox] {transform.root.name} hit {other.name}");
            playerOneShot.KillPlayer();
        }
    }
}
