using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreSceneController : MonoBehaviour
{
    [Header("Scoreboard")]
    [SerializeField] private TMP_Text finalScoreText;

    [Header("Perfect UI")]
    [SerializeField] private Slider perfectSlider;
    [SerializeField] private TMP_Text perfectValueText;

    [Header("Good UI")]
    [SerializeField] private Slider goodSlider;
    [SerializeField] private TMP_Text goodValueText;

    [Header("Coins (Top/Right)")]
    [SerializeField] private TMP_Text coinsEarnedText;
    [SerializeField] private TMP_Text totalCoinsText;

    // ====================== ⭐ STARS ======================

    [Header("Stars (Single Sprite Mode)")]
    [SerializeField] private PoseSequenceConfig config;

    [SerializeField] private Image starImage;
    [SerializeField] private Sprite star1Sprite;
    [SerializeField] private Sprite star2Sprite;
    [SerializeField] private Sprite star3Sprite;
    [SerializeField] private Sprite star4Sprite;
    [SerializeField] private Sprite star5Sprite;

    [SerializeField] private TMP_Text starMessageText;

    // ====================== RECORD ======================

    [Header("Record UI")]
    [SerializeField] private TMP_InputField inputName;
    [SerializeField] private Image inputNameOutlineImage;
    [SerializeField] private Color inputErrorColor = Color.red;
    [SerializeField] private Color inputNormalColor = Color.white;
    [SerializeField] private int maxNameLength = 12;

    [Header("Leaderboard UI")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private LeaderboardRowView rowTemplate;
    [SerializeField] private int maxEntriesToKeep = 100;

    [Header("Thai Vowel/Tone Fix")]
    [SerializeField] private bool useThaiVOffsetFix = true;
    [SerializeField] private float thaiVOffsetEm = 0.30f;

    private LeaderboardData _data;

    // ป้องกันบวกเหรียญซ้ำ
    private static int _lastAppliedScore = int.MinValue;
    private static int _lastAppliedPerfect = int.MinValue;
    private static int _lastAppliedGood = int.MinValue;
    private static bool _hasAppliedAtLeastOnce = false;

    private void Start()
    {
        int finalScore = GameSessionResult.FinalScore;
        int perfect = GameSessionResult.PerfectCount;
        int good = GameSessionResult.GoodCount;

        if (finalScoreText) finalScoreText.text = finalScore.ToString();

        ApplyPoseResultSliders(perfect, good);
        ApplyStars(finalScore);

        string modeName = GameContext.SelectedMode.ToString();
        Debug.Log($"[Score] Level={GameContext.SelectedLevel} | Mode={modeName} | Score={finalScore}");
        Debug.Log($"[Score] LeaderboardPath = {LeaderboardService.GetSavePathFromContext()}");

        int earned = CalculateCoinsFromContext(finalScore);

        Debug.Log($"[Score] Coins Earned = {earned}");

        bool shouldApplyCoins =
            !_hasAppliedAtLeastOnce ||
            finalScore != _lastAppliedScore ||
            perfect != _lastAppliedPerfect ||
            good != _lastAppliedGood;

        if (shouldApplyCoins && earned > 0)
        {
            ProgressService.AddCoins(earned);
            Debug.Log($"[Score] Coins Added. Total Now = {ProgressService.GetCoins()}");

            _hasAppliedAtLeastOnce = true;
            _lastAppliedScore = finalScore;
            _lastAppliedPerfect = perfect;
            _lastAppliedGood = good;
        }

        if (coinsEarnedText) coinsEarnedText.text = "+" + earned.ToString();
        if (totalCoinsText) totalCoinsText.text = ProgressService.GetCoins().ToString();

        _data = LeaderboardService.LoadFromContext();
        RenderLeaderboard(-1, -1);
        ClearNameInputError();

        AudioManager.SFX(SfxId.LeaderboardWow);
    }

    // ====================== SCOREBOARD / SLIDERS ======================

    private void ApplyPoseResultSliders(int perfect, int good)
    {
        int totalPoses = GetTotalPoses();

        if (perfectSlider != null)
        {
            perfectSlider.minValue = 0;
            perfectSlider.maxValue = totalPoses;
            perfectSlider.wholeNumbers = true;
            perfectSlider.value = Mathf.Clamp(perfect, 0, totalPoses);
        }

        if (goodSlider != null)
        {
            goodSlider.minValue = 0;
            goodSlider.maxValue = totalPoses;
            goodSlider.wholeNumbers = true;
            goodSlider.value = Mathf.Clamp(good, 0, totalPoses);
        }

        if (perfectValueText != null)
            perfectValueText.text = perfect.ToString();

        if (goodValueText != null)
            goodValueText.text = good.ToString();
    }

    private int GetTotalPoses()
    {
        if (config == null) return 1;
        return Mathf.Max(1, config.routineCount + config.bossCount);
    }

    // ====================== ⭐ STAR LOGIC ======================

    private void ApplyStars(int finalScore)
    {
        int stars = CalculateStarsFromScore(finalScore);
        SetStarSprite(stars);
        SetStarsMessage(stars);
    }

    private int CalculateStarsFromScore(int finalScore)
    {
        if (config == null) return 1;

        int totalPoses = Mathf.Max(1, config.routineCount + config.bossCount);
        int scoreMax = totalPoses * 1000;
        int scoreMin = totalPoses * 500;

        int range = scoreMax - scoreMin;
        if (range <= 0) return 5;

        int step = range / 5;

        if (finalScore <= scoreMin) return 1;
        if (finalScore >= scoreMax) return 5;

        int delta = finalScore - scoreMin;
        int stars = 1 + (delta / step);

        return Mathf.Clamp(stars, 1, 5);
    }

    private void SetStarSprite(int stars)
    {
        if (starImage == null) return;

        switch (stars)
        {
            case 5: starImage.sprite = star5Sprite; break;
            case 4: starImage.sprite = star4Sprite; break;
            case 3: starImage.sprite = star3Sprite; break;
            case 2: starImage.sprite = star2Sprite; break;
            default: starImage.sprite = star1Sprite; break;
        }
    }

    private void SetStarsMessage(int stars)
    {
        if (starMessageText == null) return;

        switch (stars)
        {
            case 5:
                starMessageText.text = "ตำนานเรียกพี<voffset=0.3em>่</voffset> !";
                break;
            case 4:
                starMessageText.text = "โอ้โห สุดจ๊าบ !";
                break;
            case 3:
                starMessageText.text = "พอตัวไม่มั<voffset=0.3em>่</voffset>วนิ<voffset=0.3em>่</voffset>ม";
                break;
            case 2:
                starMessageText.text = "อาจจะยังนะ";
                break;
            default:
                starMessageText.text = "สู้เขา เอาใหม่นะ !";
                break;
        }
    }

    // ====================== COINS ======================

    private static int CalculateCoinsFromContext(int finalScore)
    {
        if (GameContext.SelectedMode == null)
            return 0;

        string mode = GameContext.SelectedMode.ToString();

        switch (mode)
        {
            case "Easy":
                return 500;

            case "Medium":
                return Mathf.Max(0, finalScore / 10);

            case "Pro":
                return Mathf.Max(0, (finalScore / 10) * 2);

            default:
                Debug.LogWarning($"[Score] Unknown Mode: {mode}");
                return 0;
        }
    }

    // ====================== LEADERBOARD ======================

    public bool TryRecord()
    {
        string rawName = inputName ? inputName.text.Trim() : "";

        if (string.IsNullOrEmpty(rawName))
        {
            TriggerNameInputError();
            return false;
        }

        string clampedName = NameInputLimiterTMP.ClampByBaseLength(rawName, maxNameLength);

        if (inputName && inputName.text != clampedName)
            inputName.text = clampedName;

        if (string.IsNullOrEmpty(clampedName) || NameInputLimiterTMP.CountBaseChars(clampedName) <= 0)
        {
            TriggerNameInputError();
            return false;
        }

        string displayName = useThaiVOffsetFix
            ? FixThaiMarksWithVOffset(clampedName, thaiVOffsetEm)
            : clampedName;

        long insertedId;
        int insertedIndex = LeaderboardService.InsertAndSortFromContext(
            _data,
            displayName,
            GameSessionResult.FinalScore,
            maxEntriesToKeep,
            out insertedId
        );

        _data = LeaderboardService.LoadFromContext();
        RenderLeaderboard(insertedId, insertedIndex);

        ClearNameInputError();
        return true;
    }

    private void RenderLeaderboard(long highlightInsertId, int autoScrollToIndex)
    {
        if (contentRoot == null || rowTemplate == null) return;

        rowTemplate.gameObject.SetActive(false);

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            var child = contentRoot.GetChild(i);
            if (child == rowTemplate.transform) continue;
            Destroy(child.gameObject);
        }

        if (_data == null) _data = new LeaderboardData();
        if (_data.entries == null) _data.entries = new System.Collections.Generic.List<LeaderboardEntry>();

        for (int i = 0; i < _data.entries.Count; i++)
        {
            var e = _data.entries[i];
            if (e == null) continue;

            var row = Instantiate(rowTemplate, contentRoot);
            row.gameObject.SetActive(true);

            bool highlight = highlightInsertId >= 0 && e.insertId == highlightInsertId;
            row.Bind(i + 1, e.name, e.score, highlight);
        }

        if (autoScrollToIndex >= 0 && scrollRect != null)
            StartCoroutine(ScrollToIndexNextFrame(autoScrollToIndex, _data.entries.Count));
    }

    private IEnumerator ScrollToIndexNextFrame(int index0Based, int totalCount)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return null;

        if (scrollRect == null) yield break;

        if (totalCount <= 1)
        {
            scrollRect.verticalNormalizedPosition = 1f;
            yield break;
        }

        float t = Mathf.Clamp01((float)index0Based / (totalCount - 1f));
        scrollRect.verticalNormalizedPosition = 1f - t;
    }

    private void TriggerNameInputError()
    {
        if (inputNameOutlineImage)
            inputNameOutlineImage.color = inputErrorColor;

        if (inputName)
            StartCoroutine(ShakeRect(inputName.transform as RectTransform, 0.2f, 8f, 8));
    }

    private void ClearNameInputError()
    {
        if (inputNameOutlineImage)
            inputNameOutlineImage.color = inputNormalColor;
    }

    private IEnumerator ShakeRect(RectTransform rt, float duration, float strength, int vibrato)
    {
        if (rt == null) yield break;

        Vector3 start = rt.localPosition;
        float time = 0f;

        while (time < duration)
        {
            float damper = 1f - (time / duration);
            float x = (Random.value * 2f - 1f) * strength * damper;
            rt.localPosition = start + new Vector3(x, 0f, 0f);

            float step = duration / Mathf.Max(1, vibrato);
            time += step;
            yield return new WaitForSecondsRealtime(step);
        }

        rt.localPosition = start;
    }

    private string FixThaiMarksWithVOffset(string input, float em)
    {
        if (string.IsNullOrEmpty(input)) return input;

        string pattern = "([ัิีึืำ])([่้๊๋์็])";
        string replace = $"$1<voffset={em:0.00}em>$2</voffset>";
        return Regex.Replace(input, pattern, replace);
    }
}