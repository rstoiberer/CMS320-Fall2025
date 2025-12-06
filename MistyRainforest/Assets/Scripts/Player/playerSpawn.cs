using UnityEngine;

public class playerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    void Start()
    {
        var existing = Object.FindFirstObjectByType<KomeaMovement2>();
        if (existing != null)
        {
            existing.transform.position = transform.position;
            return;
        }

        if (playerPrefab != null)
            Instantiate(playerPrefab, transform.position, Quaternion.identity);
        else
            Debug.LogError("PlayerSpawner: playerPrefab not assigned.");
    }
}
