using UnityEngine;
using UnityEngine.SceneManagement;

public class gate : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider2D gateCollider;   // blocking collider
    [SerializeField] private SpriteRenderer sr;

    [Header("Enter Trigger (for scene load)")]
    [SerializeField] private Collider2D enterTrigger;   // separate collider with IsTrigger = true
    [SerializeField] private string nextSceneName = "Level_02";
    [SerializeField] private float loadDelay = 0.15f;

    [Header("Unlock (visual feedback, still blocking)")]
    [SerializeField] private Color lockedTint   = Color.white;
    [SerializeField] private Color unlockedTint = new Color(0.8f, 1f, 0.8f);
    [SerializeField] private float flashTime    = 0.25f;
    [SerializeField] private float pulseScale   = 1.05f;
    [SerializeField] private float holdUnlocked = 0.4f;

    [Header("Fade Away (gate disappears)")]
    [SerializeField] private float fadeDelay = 0.15f;
    [SerializeField] private float fadeTime  = 0.6f;

    // --- auto-check for remaining enemies (kept from your version) ---
    [SerializeField] private float checkEvery = 0.25f;

    private bool unlocked;
    private bool faded;

    void Awake()
    {
        if (!gateCollider) gateCollider = GetComponent<Collider2D>();
        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = lockedTint;

        // make sure enter trigger starts disabled
        if (enterTrigger)
        {
            enterTrigger.isTrigger = true;
            enterTrigger.enabled = false;
        }
    }

    private void Start()
    {
        StartCoroutine(CheckEnemiesLoop());
    }

    private System.Collections.IEnumerator CheckEnemiesLoop()
    {
        var wait = new WaitForSeconds(checkEvery);

        while (!unlocked)
        {
#if UNITY_2022_2_OR_NEWER
            int alive = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
#else
            int alive = FindObjectsOfType<EnemyHealth>(includeInactive: false).Length;
#endif
            if (alive == 0)
            {
                Unlock(); // flash + fade away + enable trigger
                yield break;
            }
            yield return wait;
        }
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

        // Allow passage and enable enter trigger
        if (gateCollider) gateCollider.enabled = false;     // no more blocking
        if (enterTrigger) enterTrigger.enabled = true;      // now we can detect the player walking through

        // Fade out sprite
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

    // Player walks through the (now enabled) enter trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!unlocked) return;                      // only after unlock
        if (!other.CompareTag("Player")) return;    // Komea must be Tag = Player

        if (!string.IsNullOrEmpty(nextSceneName))
            StartCoroutine(LoadNextAfterDelay());
    }

    private System.Collections.IEnumerator LoadNextAfterDelay()
    {
        if (loadDelay > 0f) yield return new WaitForSeconds(loadDelay);
        SceneManager.LoadScene(nextSceneName);
    }
}
