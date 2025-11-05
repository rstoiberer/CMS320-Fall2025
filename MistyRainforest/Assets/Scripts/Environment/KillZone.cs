using UnityEngine;

[DisallowMultipleComponent]
public class KillZone : MonoBehaviour
{
    [SerializeField] private LayerMask targetMask; // set to Player in Inspector

    private void OnTriggerEnter2D(Collider2D other)
    {
        // only react to specified layers
        if ((targetMask.value & (1 << other.gameObject.layer)) == 0) return;

        // find the player's death handler on this object or its parents
        var death = other.GetComponent<KomeaOneShotDeath>()
                   ?? other.GetComponentInParent<KomeaOneShotDeath>();

        if (death != null)
            death.KillPlayer();  // your existing one-shot death / scene reload
    }
}
