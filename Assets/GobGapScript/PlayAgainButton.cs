using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayAgainButton : MonoBehaviour
{
    [SerializeField] private string modeScenePattern = "ModeLv{0}";

    public void PlayAgain()
    {
        int level = GameContext.SelectedLevel;

        if (level <= 0)
        {
            Debug.LogWarning("[PlayAgain] SelectedLevel is invalid.");
            return;
        }

        string sceneName = string.Format(modeScenePattern, level);

        Debug.Log($"[PlayAgain] Loading {sceneName}");

        SceneManager.LoadScene(sceneName);
    }
}