using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Popup : MonoBehaviour
{
    [SerializeField] private GameObject Panel;

    [Header("Swap Buttons")]
    [SerializeField] private GameObject noRecordButton;
    [SerializeField] private GameObject recordButton;
    [SerializeField] private GameObject endGameButton;
    [SerializeField] private GameObject playAgainButton;

    public void OpenPopup()
    {
        if (Panel != null)
            Panel.SetActive(true);
    }

    public void ClosePopup()
    {
        if (Panel != null)
            Panel.SetActive(false);
    }

    // เรียกจาก Confirm ของ RecordPopup หรือ NoRecordPopup ก็ได้
    public void SwapButton()
    {
        if (noRecordButton != null)
            noRecordButton.SetActive(false);

        if (recordButton != null)
            recordButton.SetActive(false);

        if (endGameButton != null)
            endGameButton.SetActive(true);

        if (playAgainButton != null)
            playAgainButton.SetActive(true);
    }

    [SerializeField] private ScoreSceneController scoreController;

    public void ConfirmRecordFlow()
    {
        if (scoreController == null) return;

        bool success = scoreController.TryRecord();

        if (!success)
            return; // ❗ หยุดเลย ไม่ swap ไม่ close

        SwapButton();
        ClosePopup();
    }
}