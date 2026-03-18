using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Hearts")]
    [SerializeField] private Image[] heartsImages;
    [SerializeField] private Sprite heartFullSprite;
    [SerializeField] private Sprite heartEmptySprite;

    [Header("Hearts Root (optional)")]
    [SerializeField] private GameObject heartsRoot;

    [Header("Score")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Result Popups")]
    [SerializeField] private GameObject perfectPopup;
    [SerializeField] private GameObject goodPopup;
    [SerializeField] private GameObject damage1000Popup;
    [SerializeField] private GameObject damage500Popup;

    [Header("Boss Power")]
    [SerializeField] private GameObject bossHpRoot;
    [SerializeField] private Image bossPowerImage;
    [Tooltip("ใส่ Sprite 9 รูป: index 0 = 0/8, index 8 = 8/8")]
    [SerializeField] private Sprite[] bossPowerSprites;

    private Coroutine _feedbackRoutine;

    private void Awake()
    {
        HideAllResultPopups();

        if (bossHpRoot != null)
            bossHpRoot.SetActive(false);
    }

    // =========================================================
    // HEARTS
    // =========================================================

    public void SetHeartsVisible(bool visible)
    {
        if (heartsRoot != null)
        {
            heartsRoot.SetActive(visible);
            return;
        }

        if (heartsImages == null) return;
        for (int i = 0; i < heartsImages.Length; i++)
        {
            if (heartsImages[i] == null) continue;
            heartsImages[i].gameObject.SetActive(visible);
        }
    }

    public void SetHearts(int hearts)
    {
        if (heartsImages == null) return;

        for (int i = 0; i < heartsImages.Length; i++)
        {
            if (heartsImages[i] == null) continue;

            bool filled = i < hearts;

            if (filled && heartFullSprite != null)
                heartsImages[i].sprite = heartFullSprite;

            if (!filled && heartEmptySprite != null)
                heartsImages[i].sprite = heartEmptySprite;

            heartsImages[i].enabled = true;
        }
    }

    // =========================================================
    // SCORE
    // =========================================================

    public void SetScore(int score)
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }

    // =========================================================
    // RESULT FEEDBACK
    // =========================================================

    public void ShowResultFeedback(object gradeObj, int damage, float duration)
    {
        HideResultFeedbackImmediate();

        if (gradeObj != null && gradeObj.ToString() == "Perfect")
        {
            if (perfectPopup != null) perfectPopup.SetActive(true);
        }
        else
        {
            if (goodPopup != null) goodPopup.SetActive(true);
        }

        if (damage >= 1000)
        {
            if (damage1000Popup != null) damage1000Popup.SetActive(true);
        }
        else
        {
            if (damage500Popup != null) damage500Popup.SetActive(true);
        }

        _feedbackRoutine = StartCoroutine(HideFeedbackAfterDelay(duration));
    }

    private IEnumerator HideFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideResultFeedbackImmediate();
    }

    private void HideResultFeedbackImmediate()
    {
        if (_feedbackRoutine != null)
        {
            StopCoroutine(_feedbackRoutine);
            _feedbackRoutine = null;
        }

        if (perfectPopup != null) perfectPopup.SetActive(false);
        if (goodPopup != null) goodPopup.SetActive(false);
        if (damage1000Popup != null) damage1000Popup.SetActive(false);
        if (damage500Popup != null) damage500Popup.SetActive(false);
    }

    public void HideAllResultPopups()
    {
        if (perfectPopup != null) perfectPopup.SetActive(false);
        if (goodPopup != null) goodPopup.SetActive(false);
        if (damage1000Popup != null) damage1000Popup.SetActive(false);
        if (damage500Popup != null) damage500Popup.SetActive(false);
    }

    public void ShowPerfectOnly(float duration)
    {
        HideResultFeedbackImmediate();

        if (perfectPopup != null)
            perfectPopup.SetActive(true);

        _feedbackRoutine = StartCoroutine(HideFeedbackAfterDelay(duration));
    }

    // =========================================================
    // BOSS POWER SYSTEM
    // =========================================================

    public void ShowBossPower(int currentPower)
    {
        if (bossHpRoot != null)
            bossHpRoot.SetActive(true);

        SetBossPower(currentPower);
    }

    public void SetBossPower(int currentPower)
    {
        if (bossPowerImage == null || bossPowerSprites == null || bossPowerSprites.Length == 0)
            return;

        int clamped = Mathf.Clamp(currentPower, 0, bossPowerSprites.Length - 1);
        bossPowerImage.sprite = bossPowerSprites[clamped];
    }

    public void HideBossPower()
    {
        if (bossHpRoot != null)
            bossHpRoot.SetActive(false);
    }

    // ---------------------------------------------------------
    // Compatibility wrappers (เผื่อมี object เก่าเรียกชื่อเดิม)
    // ---------------------------------------------------------

    // public void ShowBossHP(int maxHP)
    // {
    //     ShowBossPower(0);
    // }

    // public void SetBossHP(int currentHP)
    // {
    //     SetBossPower(currentHP);
    // }

    // public void HideBossHP()
    // {
    //     HideBossPower();
    // }
}