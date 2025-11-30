using UnityEngine;
using TMPro;

public class TimerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;

    private void Awake()
    {
        if (timerText == null)
        {
            timerText = GetComponent<TMP_Text>();
        }
    }

    private void Update()
    {
        if (GameTimer.Instance == null)
        {
            timerText.text = "--:--.---";
            return;
        }

        timerText.text = GameTimer.Instance.GetFormattedTime();
    }
}
