using UnityEngine;

public class PlatformDetector : MonoBehaviour
{
    [Header("Ground Check")]
    public Transform groundCheck;            // empty child under feet
    public float groundCheckRadius = 0.08f;
    public LayerMask groundMask;             // set to Ground layer

    public Collider2D CurrentPlatform { get; private set; }

    void FixedUpdate()
    {
        CurrentPlatform = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!groundCheck) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
#endif

    public bool IsOnSamePlatformAs(PlatformDetector other)
    {
        return CurrentPlatform && other && CurrentPlatform == other.CurrentPlatform;
    }
}

