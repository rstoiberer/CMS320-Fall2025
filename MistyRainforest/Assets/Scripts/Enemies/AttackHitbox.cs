using UnityEngine;

[DisallowMultipleComponent]
public class AttackHitbox : MonoBehaviour
{
    public LayerMask targetMask; // e.g., Player layer
    public int damage = 1;

    void OnTriggerEnter2D(Collider2D other)
    {
        if ((targetMask.value & (1 << other.gameObject.layer)) == 0) return;

        // Try collider, then parent, for a damage receiver.
        var playerOneShot = other.GetComponent<KomeaOneShotDeath>() ?? other.GetComponentInParent<KomeaOneShotDeath>();
        if (playerOneShot != null)
        {
            playerOneShot.KillPlayer();
            return;
        }
    }
}

