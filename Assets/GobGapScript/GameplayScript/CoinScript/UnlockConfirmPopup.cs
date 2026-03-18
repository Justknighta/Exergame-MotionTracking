using System;
using UnityEngine;
using TMPro;

public class UnlockConfirmPopup : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text messageText;

    private Action _onConfirm;

    // 🔹 ชื่อด่านเรียงตาม index (1-based)
    private static readonly string[] LevelNames =
    {
        "Brainty",
        "Granddy",
        "Bobxing",
        "Hula Hula",
        "Dragruden"
    };

    public void Open(int levelIndex, int cost, Action onConfirm)
    {
        _onConfirm = onConfirm;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (messageText != null)
        {
            string levelName = GetLevelName(levelIndex);
            messageText.text = $"ปลดล็อก {levelName} ในราคา {cost}";
        }
    }

    public void Confirm()
    {
        _onConfirm?.Invoke();
        Close();
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        _onConfirm = null;
    }

    private string GetLevelName(int levelIndex)
    {
        int idx = levelIndex - 1;

        if (idx >= 0 && idx < LevelNames.Length)
            return LevelNames[idx];

        return $"Level {levelIndex}";
    }
}