using UnityEngine;

[DisallowMultipleComponent]
public class PlatformDetector : MonoBehaviour
{
    [Header("Ground Probe")]
    public Transform groundCheck;            // a child a few cm below feet
    public float groundCheckRadius = 0.10f;  // try 0.10–0.14
    public LayerMask groundMask = 0;         // must include your "Ground" layer
    public bool showGizmos = false;

    // last collider we detected under the probe
    Collider2D _currentPlatform;

    /// Call each physics step from whoever uses it (or leave here in FixedUpdate)
    public void SamplePlatform()
    {
        _currentPlatform = Physics2D.OverlapCircle(
            groundCheck.position, groundCheckRadius, groundMask);
    }

    /// Returns the collider under feet this frame (may be null if airborne / at an edge)
    public Collider2D CurrentPlatform()
    {
        return _currentPlatform;
    }

    /// True only if both are standing on the SAME collider
    public bool IsOnSamePlatformAs(PlatformDetector other)
    {
        if (other == null) return false;
        return _currentPlatform != null && other._currentPlatform != null &&
               ReferenceEquals(_currentPlatform, other._currentPlatform);
    }

    void FixedUpdate()
    {
        SamplePlatform();
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos || groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
