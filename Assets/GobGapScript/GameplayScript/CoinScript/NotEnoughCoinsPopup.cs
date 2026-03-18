using UnityEngine;
using TMPro;

public class NotEnoughCoinsPopup : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text messageText;

    public void Open(int missingAmount)
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        if (messageText != null)
            messageText.text = $"เหรียญไม่พอ\nยังขาดอีก {missingAmount}";
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}