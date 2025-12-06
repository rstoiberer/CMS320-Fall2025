using UnityEngine;

[DisallowMultipleComponent]
public class KillZone : MonoBehaviour
{
    [SerializeField] private LayerMask targetMask; // set to Player | Enemy in Inspector

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only react to specified layers
        if ((targetMask.value & (1 << other.gameObject.layer)) == 0) return;

        EnemyScout enemy = other.GetComponent<EnemyScout>();
        if (enemy == null) enemy = other.GetComponentInParent<EnemyScout>();
        if (enemy != null)
        {
            enemy.Die("DeathZone");
            return;
        }

        var death = other.GetComponent<KomeaOneShotDeath>() 
                 ?? other.GetComponentInParent<KomeaOneShotDeath>();
        if (death != null)
        {
            death.KillPlayer();
            return;
        }

        Destroy(other.gameObject);
    }
}
