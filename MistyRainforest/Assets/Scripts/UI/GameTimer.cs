using UnityEngine;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

    public float ElapsedTime { get; private set; }
    public bool IsRunning { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (IsRunning)
        {
            ElapsedTime += Time.deltaTime;
        }
    }

    public void StartTimer()
    {
        ElapsedTime = 0f;
        IsRunning = true;
    }

    public void StopTimer()
    {
        IsRunning = false;
    }

    public string GetFormattedTime()
    {
        int totalSeconds = Mathf.FloorToInt(ElapsedTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        float ms = (ElapsedTime - totalSeconds) * 1000f;

        return $"{minutes:00}:{seconds:00}.{ms:000}";
    }
}
