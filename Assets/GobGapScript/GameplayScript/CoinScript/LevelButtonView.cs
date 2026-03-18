using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelButtonView : MonoBehaviour
{
    public enum LevelButtonContext
    {
        LevelSelect,
        Shop
    }

    [Header("Config")]
    [SerializeField] private int levelIndex = 1;
    [SerializeField] private int unlockCost = 0;

    [Header("Context")]
    [SerializeField] private LevelButtonContext context;

    [Header("UI Refs")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;

    [SerializeField] private TMP_Text mainLabel;   // ใช้สำหรับ "เล่นเลย" (shop)
    [SerializeField] private TMP_Text costText;    // ราคา

    [SerializeField] private Button button;

    [Header("Controller")]
    [SerializeField] private LevelSelectController controller;

    public int LevelIndex => levelIndex;
    public int UnlockCost => unlockCost;

    private void Reset()
    {
        button = GetComponent<Button>();
        backgroundImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();

        button.onClick.RemoveListener(HandleClick);
        button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        controller?.OnClickLevel(levelIndex, unlockCost);
    }

    /// <summary>
    /// ถูกเรียกจาก Controller
    /// </summary>
    public void SetLocked(bool locked)
    {
        // เปลี่ยนพื้นหลัง
        if (backgroundImage != null)
            backgroundImage.sprite = locked ? lockedSprite : unlockedSprite;

        // แสดงราคา
        if (costText != null)
            costText.gameObject.SetActive(locked);

        if (costText != null)
            costText.text = locked && unlockCost > 0 ? unlockCost.ToString() : "";

        // แสดง mainLabel เฉพาะบางกรณี
        if (mainLabel != null)
        {
            if (context == LevelButtonContext.Shop)
            {
                // Shop: ถ้าปลดล็อกแล้วให้แสดง "เล่นเลย"
                mainLabel.text = locked ? "" : "เล่นเลย";
            }
            else
            {
                // LevelSelect: ไม่ต้องแสดงข้อความ
                mainLabel.text = "";
            }
        }

        // ❗ ไม่ปิด interactable เพราะ locked ยังต้องกดได้
        if (button != null)
            button.interactable = true;
    }

    public void SetAffordable(bool affordable)
    {
        // ถ้าภายหลังอยากให้ราคาเปลี่ยนสี สามารถเพิ่มได้
    }
}