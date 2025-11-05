using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public static System.Action<EnemyHealth, int> Damaged;
    public static System.Action OnAnyEnemyDied;

    [SerializeField] private int maxHealth = 3; // number of hits to kill
    [SerializeField] private Color hurtColor = new Color(1f, 0.6f, 0.6f);
    [SerializeField] private float hurtFlashTime = 0.1f;
    [SerializeField] private float deathDelay = 0.25f;

    private int current;
    private SpriteRenderer sr;
    private Color baseColor;

    void Awake()
    {
        current = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        if (sr) baseColor = sr.color;
    }

    public void TakeDamage(int amount)
    {
        current -= amount;
        
        Damaged?.Invoke(this, current);

        if (sr) StartCoroutine(Flash());

        if (current <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator Flash()
    {
        if (!sr) yield break;
        sr.color = hurtColor;
        yield return new WaitForSeconds(hurtFlashTime);
        sr.color = baseColor;
    }

    private void Die()
    {
        // disable colliders & logic, then destroy
        foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.simulated = false;
        OnAnyEnemyDied?.Invoke();
        Destroy(gameObject, deathDelay);
    }
}
