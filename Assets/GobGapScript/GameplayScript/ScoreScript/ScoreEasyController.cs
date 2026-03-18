using UnityEngine;
using TMPro;

public class ScoreEasyController : MonoBehaviour
{
    [Header("Coins UI (optional)")]
    [SerializeField] private TMP_Text coinsEarnedText;
    [SerializeField] private TMP_Text totalCoinsText;

    [Header("Easy Reward")]
    [SerializeField] private int easyCoinsReward = 500;

    private void Start()
    {
        int earned = Mathf.Max(0, easyCoinsReward);

        // บวกเหรียญทันที
        ProgressService.AddCoins(earned);

        Debug.Log($"[ScoreEasy] Coins Added = {earned}. Total Now = {ProgressService.GetCoins()}");

        // อัปเดต UI (ถ้ามี)
        if (coinsEarnedText != null)
            coinsEarnedText.text = earned.ToString();

        if (totalCoinsText != null)
            totalCoinsText.text = ProgressService.GetCoins().ToString();
    }
}