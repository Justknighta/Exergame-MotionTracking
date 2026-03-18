using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PausePopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;

    [Header("Refs (ลาก GameplayController ใส่)")]
    [SerializeField] private GameplayController gameplay;

    private void Awake()
    {
        if (pausePanel != null) pausePanel.SetActive(false);

        if (gameplay == null)
            gameplay = FindObjectOfType<GameplayController>();
    }

    public void OpenPopup()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        gameplay?.PauseGame();
    }

    public void ClosePopup()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        gameplay?.ResumeGame();
    }



}
