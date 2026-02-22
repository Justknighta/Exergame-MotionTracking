using UnityEngine;
using TMPro;

public class TimedPoseEvaluator : MonoBehaviour
{
    [Header("Rule (ท่าที่จะตรวจ)")]
    [SerializeField] private PoseRuleBase rule;

    [Header("Session")]
    public KeyCode startKey = KeyCode.A;
    public float checkInterval = 5f;

    [Header("Pass Rule")]
    [Range(0f, 1f)] public float passThreshold = 0.50f;
    [Range(0f, 1f)] public float bucketGoodMinCorrectRatio = 0.50f;

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text resultText;

    // runtime (มาจาก rule)
    private float _durationRuntime = 60f;
    private int _bonusRuntime = 100;

    // session state
    private bool _sessionActive;
    private float _sessionTimer;
    private float _bucketTimer;
    private int _bucketIndex;

    // bucket accumulators
    private int _framesTotal;
    private int _framesValid;
    private int _framesCorrect;

    // overall buckets
    private int _goodBuckets;
    private int _badBuckets;

    private int TotalBucketsPlanned => Mathf.CeilToInt(_durationRuntime / checkInterval);

    private void Awake()
    {
        WriteStatus("พร้อมแล้ว: กด A เพื่อเริ่มวัดผล");
        WriteResult("");
    }

    private void Update()
    {
        if (Input.GetKeyDown(startKey))
            StartSession();

        if (!_sessionActive) return;

        _sessionTimer += Time.deltaTime;
        _bucketTimer += Time.deltaTime;

        EvaluateFrame();
        UpdateLiveUI();

        if (_bucketTimer >= checkInterval)
        {
            _bucketTimer -= checkInterval;
            GradeBucketAndCount();
            ResetBucket();
            _bucketIndex++;
        }

        if (_sessionTimer >= _durationRuntime)
            EndSession();
    }

    public void StartSession()
    {
        if (rule == null)
        {
            WriteStatus("❌ ยังไม่ได้ใส่ Rule (PoseRuleBase) ใน Inspector");
            return;
        }

        _durationRuntime = Mathf.Max(1f, rule.DurationSec);
        _bonusRuntime = rule.PassBonusScore;

        _sessionActive = true;
        _sessionTimer = 0f;
        _bucketTimer = 0f;
        _bucketIndex = 0;

        _goodBuckets = 0;
        _badBuckets = 0;

        ResetBucket();
        rule.OnSessionStart();

        WriteResult("");
        WriteStatus($"▶ เริ่ม: {rule.PoseName} ({_durationRuntime:0}s)");
    }

    public void EndSession()
    {
        // ตัดเศษ bucket สุดท้าย
        if (_framesTotal > 0)
            GradeBucketAndCount(finalPartial: true);

        _sessionActive = false;
        rule.OnSessionEnd();

        int totalBuckets = Mathf.Max(1, _goodBuckets + _badBuckets);
        float goodRatio = (float)_goodBuckets / totalBuckets;
        bool pass = goodRatio >= passThreshold;

        if (pass)
        {
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddScore(_bonusRuntime);

            WriteResult($"✅ ผ่าน!\nGOOD {_goodBuckets}/{totalBuckets} ({goodRatio:P0})\n+{_bonusRuntime} คะแนน");
        }
        else
        {
            WriteResult($"❌ ไม่ผ่าน\nGOOD {_goodBuckets}/{totalBuckets} ({goodRatio:P0})");
        }

        WriteStatus("⏹ จบแล้ว — กด A เพื่อเริ่มใหม่");
    }

    private void ResetBucket()
    {
        _framesTotal = 0;
        _framesValid = 0;
        _framesCorrect = 0;
    }

    private void EvaluateFrame()
    {
        _framesTotal++;

        bool valid;
        bool correct = rule.EvaluateThisFrame(out valid);

        // ✅ หลุด tracking = นับเป็นผิด (BAD) ด้วย
        _framesValid++;
        if (valid && correct) _framesCorrect++;
    }

    private void GradeBucketAndCount(bool finalPartial = false)
    {
        float correctRatio = (_framesValid <= 0) ? 0f : (float)_framesCorrect / _framesValid;
        bool bucketIsGood = correctRatio >= bucketGoodMinCorrectRatio;

        if (bucketIsGood) _goodBuckets++;
        else _badBuckets++;

        int secFrom = _bucketIndex * (int)checkInterval;
        int secTo = secFrom + (int)checkInterval;
        Debug.Log($"[{secFrom:00}-{secTo:00}s] {(bucketIsGood ? "GOOD" : "BAD")} | correct={correctRatio:P0} | frames={_framesValid}");
    }

    private void UpdateLiveUI()
    {
        if (statusText == null) return;

        int totalBucketsCounted = Mathf.Max(1, _goodBuckets + _badBuckets);
        float goodRatio = (float)_goodBuckets / totalBucketsCounted;

        int sec = Mathf.Clamp(Mathf.FloorToInt(_sessionTimer), 0, (int)_durationRuntime);
        int bucketNo = Mathf.Clamp(_bucketIndex + 1, 1, TotalBucketsPlanned);

        statusText.text =
            $"ท่า: {rule?.PoseName}\n" +
            $"เวลา: {sec:00}/{(int)_durationRuntime:00}s | ช่วง: {bucketNo}/{TotalBucketsPlanned}\n" +
            $"GOOD: {_goodBuckets} | BAD: {_badBuckets} | GOOD%: {goodRatio:P0}\n" +
            (rule != null ? rule.GetDebugText() : "");
    }

    private void WriteStatus(string msg)
    {
        Debug.Log(msg);
        if (statusText != null) statusText.text = msg;
    }

    private void WriteResult(string msg)
    {
        if (resultText != null) resultText.text = msg;
    }
}