using UnityEngine;
using UnityEngine.UI;

public class TutorialPageController : MonoBehaviour
{
    [Header("Pages (เรียงตามลำดับ)")]
    [SerializeField] private GameObject[] pages;

    [Header("Optional UI")]
    [SerializeField] private Button globalPrevButton;
    [SerializeField] private Button globalNextButton;

    [Header("Page Features")]
    [SerializeField] private TutorialVideoController videoController;
    [SerializeField] private TutorialMotionStepController motionStepController;

    [Header("Start Page")]
    [SerializeField] private int startPageIndex = 0;

    private int currentPageIndex = 0;

    private void Start()
    {
        ShowPage(startPageIndex);
    }

    public void NextPage()
    {
        if (currentPageIndex >= pages.Length - 1)
            return;

        HandleBeforeLeavePage(currentPageIndex);
        ShowPage(currentPageIndex + 1);
    }

    public void PrevPage()
    {
        if (currentPageIndex <= 0)
            return;

        HandleBeforeLeavePage(currentPageIndex);
        ShowPage(currentPageIndex - 1);
    }

    public void ShowPage(int targetIndex)
    {
        if (pages == null || pages.Length == 0)
        {
            Debug.LogWarning("TutorialSceneController: pages is empty.");
            return;
        }

        if (targetIndex < 0 || targetIndex >= pages.Length)
        {
            Debug.LogWarning($"TutorialSceneController: invalid page index {targetIndex}");
            return;
        }

        currentPageIndex = targetIndex;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
                pages[i].SetActive(i == currentPageIndex);
        }

        HandleAfterEnterPage(currentPageIndex);
        RefreshNavigationButtons();
    }

    private void HandleBeforeLeavePage(int pageIndex)
    {
        // สมมติ page 1 = page วิดีโอ
        if (pageIndex == 1 && videoController != null)
        {
            videoController.StopAndReset();
        }
    }

    private void HandleAfterEnterPage(int pageIndex)
    {
        // สมมติ page 1 = page วิดีโอ
        if (pageIndex == 1 && videoController != null)
        {
            videoController.PrepareVideoPage();
        }

        // สมมติ page 3 = page motion check
        if (pageIndex == 3 && motionStepController != null)
        {
            motionStepController.ResetStep();
        }
    }

    private void RefreshNavigationButtons()
    {
        if (globalPrevButton != null)
            globalPrevButton.interactable = currentPageIndex > 0;

        if (globalNextButton != null)
            globalNextButton.interactable = currentPageIndex < pages.Length - 1;
    }
}