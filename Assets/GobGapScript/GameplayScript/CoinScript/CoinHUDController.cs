using UnityEngine;
using TMPro;

public class CoinHUDController : MonoBehaviour
{
    [SerializeField] private TMP_Text coinText;

    private void Start()
    {
        Refresh();
    }

    /// <summary>
    /// โหลดเหรียญจาก ProgressService แล้วอัปเดต UI
    /// </summary>
    public void Refresh()
    {
        if (coinText == null) return;

        int coins = ProgressService.GetCoins();
        coinText.text = coins.ToString();
    }
}