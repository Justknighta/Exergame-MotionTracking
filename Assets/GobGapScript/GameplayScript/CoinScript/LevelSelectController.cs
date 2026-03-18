using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectController : MonoBehaviour
{
    [Header("Level Buttons (1..5)")]
    [SerializeField] private LevelButtonView[] levelButtons;

    [Header("Popups")]
    [SerializeField] private NotEnoughCoinsPopup notEnoughPopup;
    [SerializeField] private UnlockConfirmPopup unlockConfirmPopup;

    [Header("Coin HUD")]
    [SerializeField] private CoinHUDController coinHUD;

    [Header("Mode Scene Pattern")]
    [Tooltip("เช่น 'ModeLv{0}' => ModeLv1..ModeLv5")]
    [SerializeField] private string modeScenePattern = "ModeLv{0}";

    [SerializeField] private Transform contentRoot;

    private void Start()
    {
        RefreshAll();
    }

    public void RefreshAll()
    {
        coinHUD?.Refresh();

        int coins = ProgressService.GetCoins();

        LevelButtonView[] buttons = contentRoot.GetComponentsInChildren<LevelButtonView>(true);

        foreach (var b in buttons)
        {
            if (b == null) continue;

            bool unlocked = ProgressService.IsLevelUnlocked(b.LevelIndex);

            b.SetLocked(!unlocked);
            b.SetAffordable(coins >= b.UnlockCost);
        }
    }

    /// <summary>
    /// ผูกกับปุ่ม "ด่าน" ทุกปุ่ม
    /// </summary>
    public void OnClickLevel(int levelIndex, int cost)
    {
        // ด่าน 1 ฟรี
        if (levelIndex == 1)
        {
            OpenMode(levelIndex);
            return;
        }

        // ปลดล็อกแล้ว -> เข้า Mode
        if (ProgressService.IsLevelUnlocked(levelIndex))
        {
            OpenMode(levelIndex);
            return;
        }

        // ยังล็อก -> เช็คเหรียญ
        int coins = ProgressService.GetCoins();
        if (coins < cost)
        {
            int missing = cost - coins;
            notEnoughPopup?.Open(missing);
            return;
        }

        // เหรียญพอ -> เปิด popup ยืนยันซื้อ
        unlockConfirmPopup?.Open(levelIndex, cost, onConfirm: () =>
        {
            int missing;
            bool ok = ProgressService.TryUnlockLevel(levelIndex, out missing);

            if (!ok)
            {
                notEnoughPopup?.Open(missing);
                return;
            }

            // ซื้อสำเร็จ
            RefreshAll();

            // จะเข้า Mode เลยไหม?
            // ถ้าอยากเข้าเลย ให้เปิดบรรทัดนี้
            // OpenMode(levelIndex);
        });
    }

    private void OpenMode(int levelIndex)
    {
        string sceneName = string.Format(modeScenePattern, levelIndex);
        SceneManager.LoadScene(sceneName);
    }
}