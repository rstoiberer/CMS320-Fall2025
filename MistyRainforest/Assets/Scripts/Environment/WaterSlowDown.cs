using UnityEngine;

public class WaterSlowDown : MonoBehaviour
{
    [Header("Water Settings")]
    [SerializeField] private float speedMultiplier = 0.5f;
    [SerializeField] private float jumpMultiplier = 0.6f;
    [SerializeField] private float gravityScaleInWater = 1f;

    private KomeaMovement2 player; // <-- fixed reference to correct class
    private float originalSpeed;
    private float originalJump;
    private float originalGravity;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.GetComponent<KomeaMovement2>();
            if (player != null)
            {
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

                // Store originals
                originalSpeed = player.GetType()
                    .GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(player) is float s ? s : 5f;
                originalJump = player.GetType()
                    .GetField("jumpForce", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(player) is float j ? j : 7f;
                originalGravity = rb.gravityScale;

                // Apply modifiers
                player.GetType()
                    .GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .SetValue(player, originalSpeed * speedMultiplier);
                player.GetType()
                    .GetField("jumpForce", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .SetValue(player, originalJump * jumpMultiplier);
                rb.gravityScale = gravityScaleInWater;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

            // Restore originals
            player.GetType()
                .GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(player, originalSpeed);
            player.GetType()
                .GetField("jumpForce", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(player, originalJump);
            rb.gravityScale = originalGravity;
        }
    }
}
