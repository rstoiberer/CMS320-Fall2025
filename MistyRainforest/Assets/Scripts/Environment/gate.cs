using UnityEngine;

public class gate : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider2D gateCollider;
    [SerializeField] private SpriteRenderer sr;

    [Header("Unlock (visual feedback, still blocking)")]
    [SerializeField] private Color lockedTint   = Color.white;
    [SerializeField] private Color unlockedTint = new Color(0.8f, 1f, 0.8f);
    [SerializeField] private float flashTime    = 0.25f;
    [SerializeField] private float pulseScale   = 1.05f;
    [SerializeField] private float holdUnlocked = 0.4f;

    [Header("Fade Away (gate disappears)")]
    [SerializeField] private float fadeDelay = 0.15f;
    [SerializeField] private float fadeTime  = 0.6f;

    private bool unlocked;
    private bool faded;

    void Awake()
    {
        if (!gateCollider) gateCollider = GetComponent<Collider2D>();
        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = lockedTint;
    }

    public void Unlock()
    {
        if (unlocked) return;
        unlocked = true;
        StartCoroutine(UnlockAndFade());
    }

    System.Collections.IEnumerator UnlockAndFade()
    {

        // Tint + pulse (still blocking)
        if (sr)
        {
            Color from = sr.color;
            Vector3 baseScale = transform.localScale;
            Vector3 targetScale = baseScale * pulseScale;

            float t = 0f;
            while (t < flashTime)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / flashTime);
                sr.color = Color.Lerp(from, unlockedTint, u);
                transform.localScale = Vector3.Lerp(baseScale, targetScale, Mathf.SmoothStep(0, 1, u));
                yield return null;
            }
            transform.localScale = baseScale;
        }

        // Wait briefly (still blocking)
        yield return new WaitForSeconds(holdUnlocked + fadeDelay);

        // Disable collider so the player can pass
        if (gateCollider) gateCollider.enabled = false;

        // Fade out
        if (sr)
        {
            Color c = sr.color;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / fadeTime);
                c.a = 1f - u;
                sr.color = c;
                yield return null;
            }
            sr.enabled = false; // fully invisible
        }

    }
    // --- Auto-check for remaining enemies ---
[SerializeField] private float checkEvery = 0.25f;
private bool checking;

private void Start()
{
    StartCoroutine(CheckEnemiesLoop());
}

private System.Collections.IEnumerator CheckEnemiesLoop()
{
    var wait = new WaitForSeconds(checkEvery);

    while (!unlocked)
    {
        int alive = FindObjectsOfType<EnemyHealth>(includeInactive: false).Length;
        if (alive == 0)
        {
            Unlock(); // flash + fade away
            yield break;
        }
        yield return wait;
    }
}
}
