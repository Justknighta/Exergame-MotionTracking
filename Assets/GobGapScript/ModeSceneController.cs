using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeSceneController : MonoBehaviour
{
    [Header("Level Config")]
    [SerializeField] private int levelIndex = 1; // ใส่ให้ตรงกับ ModeLvX (1..5)

    [Header("Tutorial Scenes")]
    [SerializeField] private string tutorialEasySceneName = "Tutorial_Easy";
    [SerializeField] private string tutorialMediumSceneName = "Tutorial_Medium";
    [SerializeField] private string tutorialProSceneName = "Tutorial_Pro";

    // ผูกกับปุ่ม Easy
    public void SelectEasy()
    {
        GameContext.SetSelection(levelIndex, GameContext.Mode.Easy);
        SceneManager.LoadScene(tutorialEasySceneName);
    }

    // ผูกกับปุ่ม Medium
    public void SelectMedium()
    {
        GameContext.SetSelection(levelIndex, GameContext.Mode.Medium);
        SceneManager.LoadScene(tutorialMediumSceneName);
    }

    // ผูกกับปุ่ม Pro
    public void SelectPro()
    {
        GameContext.SetSelection(levelIndex, GameContext.Mode.Pro);
        SceneManager.LoadScene(tutorialProSceneName);
    }
}