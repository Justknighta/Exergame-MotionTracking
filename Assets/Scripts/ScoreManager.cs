using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score")]
    [SerializeField] private int totalScore = 0;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText; // ลาก TMP_Text มาใส่

    public int TotalScore => totalScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // ถ้าอยากให้ข้ามฉากแล้วยังอยู่: uncomment
        // DontDestroyOnLoad(gameObject);

        RefreshUI();
    }

    public void SetScore(int value)
    {
        totalScore = Mathf.Max(0, value);
        RefreshUI();
    }

    public void AddScore(int amount)
    {
        totalScore = Mathf.Max(0, totalScore + amount);
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {totalScore}";
    }
}