using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// จำกัดความยาวชื่อแบบ "base characters" ไม่รวม combining marks (เช่น วรรณยุกต์/สระบนล่าง)
/// - นับทุกอย่างที่ไม่ใช่ combining mark เป็น base (รวมอังกฤษ/เลข/สัญลักษณ์/emoji)
/// - แสดงตัวนับ x/12 ผ่าน TMP_Text
/// - ทำงานตอนพิมพ์ (onValueChanged) + กัน recursion + พยายามรักษา caret
/// </summary>
public class NameInputLimiterTMP : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_InputField inputName;
    [SerializeField] private TMP_Text counterText; // แสดง x/12 (base count)

    [Header("Rules")]
    [SerializeField] private int maxBaseLength = 12;

    private bool _internalChange;

    private void Awake()
    {
        if (inputName == null) inputName = GetComponent<TMP_InputField>();
    }

    private void OnEnable()
    {
        if (inputName != null)
            inputName.onValueChanged.AddListener(OnNameChanged);

        // initial refresh
        RefreshCounter(inputName != null ? inputName.text : "");
    }

    private void OnDisable()
    {
        if (inputName != null)
            inputName.onValueChanged.RemoveListener(OnNameChanged);
    }

    private void OnNameChanged(string newValue)
    {
        if (_internalChange) return;
        if (inputName == null) return;

        // เก็บ caret ก่อน
        int oldCaret = inputName.caretPosition;

        // clamp
        string clamped = ClampByBaseLength(newValue, maxBaseLength);

        if (!string.Equals(newValue, clamped))
        {
            _internalChange = true;

            inputName.text = clamped;

            // พยายามตั้ง caret ให้ใกล้เดิมที่สุด
            // (ถ้าตัดข้อความออก caret ควรไม่เกินความยาวใหม่)
            int newCaret = Mathf.Clamp(oldCaret, 0, clamped.Length);
            inputName.caretPosition = newCaret;
            inputName.selectionAnchorPosition = newCaret;
            inputName.selectionFocusPosition = newCaret;

            _internalChange = false;
        }

        RefreshCounter(inputName.text);
    }

    private void RefreshCounter(string text)
    {
        int baseCount = CountBaseChars(text);
        if (counterText != null)
            counterText.text = $"{baseCount}/{maxBaseLength}";
    }

    /// <summary>
    /// นับ base chars: ทุก char ที่ "ไม่ใช่ combining mark" จะนับเป็น 1
    /// </summary>
    public static int CountBaseChars(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;

        int count = 0;
        for (int i = 0; i < s.Length; i++)
        {
            if (!IsCombiningMark(s[i]))
                count++;
        }
        return count;
    }

    /// <summary>
    /// ตัด string ให้ base chars ไม่เกิน maxBase
    /// โดยอนุญาตให้ combining marks หลัง base ตัวสุดท้าย "ติดมาได้"
    /// เพื่อไม่ให้สระ/วรรณยุกต์หลุดกลางคำ
    /// </summary>
    public static string ClampByBaseLength(string s, int maxBase)
    {
        if (string.IsNullOrEmpty(s)) return s;
        if (maxBase <= 0) return "";

        int baseCount = 0;
        var sb = new StringBuilder(s.Length);

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            bool isMark = IsCombiningMark(c);

            if (!isMark)
            {
                if (baseCount >= maxBase)
                {
                    // เกินแล้ว หยุดทันที (ไม่เอา base ตัวใหม่)
                    break;
                }
                baseCount++;
                sb.Append(c);
            }
            else
            {
                // เป็น mark: อนุญาตเฉพาะกรณี "เคยมี base แล้ว" และ "ยังไม่หลุดเกิน maxBase"
                // (ถ้า mark มาเป็นตัวแรกๆ โดยไม่มี base ให้ไม่ append จะปลอดภัยกว่า)
                if (baseCount > 0 && baseCount <= maxBase)
                    sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// ตรวจ combining mark โดยใช้ UnicodeCategory
    /// ครอบคลุมไทย/และภาษาที่ใช้ mark แบบ combining ทั้งหมด
    /// </summary>
    private static bool IsCombiningMark(char c)
    {
        var cat = CharUnicodeInfo.GetUnicodeCategory(c);
        return cat == UnicodeCategory.NonSpacingMark
            || cat == UnicodeCategory.SpacingCombiningMark
            || cat == UnicodeCategory.EnclosingMark;
    }
}
