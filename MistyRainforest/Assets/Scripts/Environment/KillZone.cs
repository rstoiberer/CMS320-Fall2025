using UnityEngine;

[DisallowMultipleComponent]
public class KillZone : MonoBehaviour
{
    [SerializeField] private LayerMask targetMask; // set to Player | Enemy in Inspector

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only react to specified layers
        if ((targetMask.value & (1 << other.gameObject.layer)) == 0) return;

        // 1) Enemy? -> call Die() so the gate gets the AnyEnemyDied event
        EnemyScout enemy = other.GetComponent<EnemyScout>();
        if (enemy == null) enemy = other.GetComponentInParent<EnemyScout>();
        if (enemy != null)
        {
            enemy.Die("DeathZone");
            return;
        }

        // 2) Player? -> keep your existing one-shot death / scene reload
        var death = other.GetComponent<KomeaOneShotDeath>() 
                 ?? other.GetComponentInParent<KomeaOneShotDeath>();
        if (death != null)
        {
            death.KillPlayer();
            return;
        }

        // Anything else that enters can be cleaned up if you want:
        Destroy(other.gameObject);
    }
}
