using UnityEngine;

[DisallowMultipleComponent]
public class PlatformDetector : MonoBehaviour
{
    [Header("Ground Probe")]
    public Transform groundCheck;            
    public float groundCheckRadius = 0.10f;  
    public LayerMask groundMask = 0;
    public bool showGizmos = false;

    Collider2D _currentPlatform;

    public void SamplePlatform()
    {
        _currentPlatform = Physics2D.OverlapCircle(
            groundCheck.position, groundCheckRadius, groundMask);
    }

    public Collider2D CurrentPlatform()
    {
        return _currentPlatform;
    }

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
