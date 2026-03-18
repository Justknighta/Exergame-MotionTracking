using UnityEngine;
using UnityEngine.SceneManagement;

public class UIButton : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string homeSceneName = "Home";
    [SerializeField] private string levelSceneName = "Level";
    [SerializeField] private string shopSceneName = "Shop";

    // ถ้า tutorial แยกตามโหมด ค่อยเพิ่มเป็น Tutorial_Easy/Medium/Pro ทีหลังได้
    [SerializeField] private string tutorialSceneName = "Tutorial";

    [Header("Mode Scene Pattern")]
    [Tooltip("เช่น 'ModeLv{0}' => ModeLv1..ModeLv5")]
    [SerializeField] private string modeScenePattern = "ModeLv{0}";

    [Header("Other Scenes")]
    [SerializeField] private string gameplaySceneName = "Gameplay";
    [SerializeField] private string scoreSceneName = "Score";
    [SerializeField] private string gameOverSceneName = "GameOver";

    // -------------------------
    // Basic navigation
    // -------------------------

    public void GoHome() => LoadSceneSafe(homeSceneName);
    public void GoLevel() => LoadSceneSafe(levelSceneName);
    public void GoShop() => LoadSceneSafe(shopSceneName);
    public void GoTutorial() => LoadSceneSafe(tutorialSceneName);

    // -------------------------
    // Mode navigation
    // -------------------------

    // เผื่อยังมีปุ่มเดิม "GoModeBrainty" อยู่ ก็ให้ยังใช้ได้
    public void GoModeBrainty() => GoModeByLevelIndex(1);

    // ใช้กับปุ่มเล่นของแต่ละด่าน (ส่งเลขด่าน 1..5)
    public void GoModeByLevelIndex(int levelIndex)
    {
        string sceneName = string.Format(modeScenePattern, levelIndex);
        LoadSceneSafe(sceneName);
    }

    // -------------------------
    // Gameplay / Score
    // -------------------------

    public void GoGameplay() => LoadSceneSafe(gameplaySceneName);
    public void GoScore() => LoadSceneSafe(scoreSceneName);
    public void GoGameOver() => LoadSceneSafe(gameOverSceneName);

    // -------------------------
    // Back rules (NEW)
    // -------------------------

    /// <summary>
    /// กติกาใหม่ของโปรเจกต์นี้:
    /// - ปุ่ม Back ในหน้า Mode => กลับ Level เสมอ
    /// </summary>
    public void BackFromMode() => LoadSceneSafe(levelSceneName);

    /// <summary>
    /// ถ้าอยากมีปุ่ม Back แบบทั่วไป (ไม่ใช้ history แล้ว)
    /// ให้กำหนดฉากปลายทางเองจาก Inspector ต่อปุ่ม
    /// </summary>
    public void BackToScene(string sceneName) => LoadSceneSafe(sceneName);

    // -------------------------
    // Quit
    // -------------------------

    public void Quit()
    {
        Application.Quit();
    }

    // -------------------------
    // Helper
    // -------------------------

    private static void LoadSceneSafe(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[UIButton] sceneName is empty.");
            return;
        }

        string current = SceneManager.GetActiveScene().name;
        if (current == sceneName) return;

        SceneManager.LoadScene(sceneName);
    }
}