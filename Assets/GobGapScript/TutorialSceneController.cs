using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialSceneController : MonoBehaviour
{
    [Header("Gameplay Scene Patterns")]
    [Tooltip("เช่น 'GameplayLv{0}' => GameplayLv1..GameplayLv5")]
    [SerializeField] private string gameplayProMedPattern = "GameplayLv{0}";

    [Tooltip("เช่น 'GameplayEasyLv{0}' => GameplayEasyLv1..GameplayEasyLv5")]
    [SerializeField] private string gameplayEasyPattern = "GameplayEasyLv{0}";

    // ผูกกับปุ่ม Continue
    public void ContinueToGameplay()
    {
        int level = Mathf.Clamp(GameContext.SelectedLevel, 1, 5);

        string sceneName;
        if (GameContext.SelectedMode == GameContext.Mode.Easy)
            sceneName = string.Format(gameplayEasyPattern, level);
        else
            sceneName = string.Format(gameplayProMedPattern, level);

        SceneManager.LoadScene(sceneName);
    }
}