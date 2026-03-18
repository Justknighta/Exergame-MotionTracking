using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingPopup : MonoBehaviour
{
    [SerializeField] private GameObject settingPanel;

    public void OpenPopup()
    {
        settingPanel.SetActive(true);
    }

    public void ClosePopup()
    {
        settingPanel.SetActive(false);
    }

    public void OpenFeedbackForm()
    {
        Application.OpenURL("https://forms.gle/uiPuW4xE2xVKtj2j6");
    }
}
