using UnityEngine;

public class playerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    void Start()
    {
        // If a player already exists (e.g., you choose to persist it later), move it.
        var existing = Object.FindFirstObjectByType<KomeaMovement2>();
        if (existing != null)
        {
            existing.transform.position = transform.position;
            return;
        }

        // Otherwise, spawn a fresh one
        if (playerPrefab != null)
            Instantiate(playerPrefab, transform.position, Quaternion.identity);
        else
            Debug.LogError("PlayerSpawner: playerPrefab not assigned.");
    }
}
