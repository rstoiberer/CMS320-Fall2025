using UnityEngine;

public class PearlPickup : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only react when the Player touches it
        if (!other.CompareTag("Player")) return;

        // Stop the timer and log the final time
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.StopTimer();
            Debug.Log("Final time: " + GameTimer.Instance.GetFormattedTime());
        }
        else
        {
            Debug.LogWarning("GameTimer.Instance is null – did you forget to add it to the Backstory scene?");
        }

        // For now, just hide the pearl so it looks collected
        gameObject.SetActive(false);
    }
}
