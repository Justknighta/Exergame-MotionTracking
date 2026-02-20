using UnityEngine;
using TMPro;

public class TimedPoseEvaluator : MonoBehaviour
{
    [Header("Rule (ท่าที่จะตรวจ)")]
    [SerializeField] private PoseRuleBase rule;

    [Header("Session")]
    public KeyCode startKey = KeyCode.A;
    public float sessionDuration = 60f;   // ✅ เพิ่มตัวนี้
    public float checkInterval = 5f;

    [Header("Pass & Score")]
    [Range(0f, 1f)] public float passThreshold = 0.50f; // GOOD >= 50% = ผ่าน
    public int passBonusScore = 100;

    [Header("Bucket grading")]
    [Range(0f, 1f)] public float bucketGoodMinCorrectRatio = 0.50f; // ใน bucket ต้องถูก >= 50% ถึงนับเป็น GOOD
    public int minValidFramesPerBucket = 5; // valid น้อยกว่านี้ถือ BAD (pose หลุด)

    [Header("UI")]
    [SerializeField] private TMP_Text statusText; // ระหว่างทำ
    [SerializeField] private TMP_Text resultText; // สรุปตอนจบ

    // session state
    private bool _sessionActive;
    private float _sessionTimer;
    private float _bucketTimer;
    private int _bucketIndex;

    // bucket accumulators
    private int _framesTotal;
    private int _framesValid;
    private int _framesCorrect;

    // overall buckets result
    private int _goodBuckets;
    private int _badBuckets;

    private int TotalBucketsPlanned => Mathf.CeilToInt(sessionDuration / checkInterval);

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

        if (_sessionTimer >= sessionDuration)
        {
            EndSession();
        }
    }

    public void StartSession()
    {
        if (rule == null)
        {
            WriteStatus("❌ ยังไม่ได้ใส่ Rule (PoseRuleBase) ใน Inspector");
            return;
        }

        _sessionActive = true;
        _sessionTimer = 0f;
        _bucketTimer = 0f;
        _bucketIndex = 0;

        _goodBuckets = 0;
        _badBuckets = 0;

        ResetBucket();
        rule.OnSessionStart();

        WriteResult("");
        WriteStatus("▶ เริ่มวัดผล...");
    }

    public void EndSession()
    {
        // นับ bucket เศษท้าย (ถ้ามีข้อมูลสะสม)
        if (_framesTotal > 0)
        {
            GradeBucketAndCount(finalPartial: true);
        }

        _sessionActive = false;
        rule.OnSessionEnd();

        int totalBuckets = Mathf.Max(1, _goodBuckets + _badBuckets);
        float goodRatio = (float)_goodBuckets / totalBuckets;
        bool pass = goodRatio >= passThreshold;

        if (pass)
        {
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddScore(passBonusScore);

            WriteResult($"✅ ผ่าน!\nGOOD {_goodBuckets}/{totalBuckets} ({goodRatio:P0})\n+{passBonusScore} คะแนน");
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
        bool valid;
        bool correct = rule.EvaluateThisFrame(out valid);

        // นับทุกเฟรมเป็น "valid สำหรับการคิดสัดส่วน" เพื่อให้หลุด = bad
        _framesValid++;

        if (valid && correct)
        {
            _framesCorrect++;
        }
        // ถ้า valid=false จะไม่เพิ่ม correct => เท่ากับผิด (BAD) อัตโนมัติ
    }

    private void GradeBucketAndCount(bool finalPartial = false)
    {
        // ถ้า valid น้อยไป = BAD
        if (_framesValid < minValidFramesPerBucket)
        {
            _badBuckets++;
            Debug.Log(finalPartial ? "[FINAL] BAD (pose lost)" : $"[Bucket {_bucketIndex}] BAD (pose lost)");
            return;
        }

        float correctRatio = (float)_framesCorrect / _framesValid; // 0..1
        bool bucketIsGood = correctRatio >= bucketGoodMinCorrectRatio;

        if (bucketIsGood) _goodBuckets++;
        else _badBuckets++;

        int secFrom = _bucketIndex * (int)checkInterval;
        int secTo = secFrom + (int)checkInterval;

        Debug.Log($"[{secFrom:00}-{secTo:00}s] {(bucketIsGood ? "GOOD" : "BAD")} | correct={correctRatio:P0} | validFrames={_framesValid}");
    }

    private void UpdateLiveUI()
    {
        if (statusText == null) return;

        int totalBucketsCounted = Mathf.Max(1, _goodBuckets + _badBuckets);
        float goodRatio = (float)_goodBuckets / totalBucketsCounted;

        int sec = Mathf.Clamp(Mathf.FloorToInt(_sessionTimer), 0, (int)sessionDuration);
        int bucketNo = Mathf.Clamp(_bucketIndex + 1, 1, TotalBucketsPlanned);

        statusText.text =
            $"เวลา: {sec:00}/{(int)sessionDuration:00}s | ช่วง: {bucketNo}/{TotalBucketsPlanned}\n" +
            $"GOOD: {_goodBuckets} | BAD: {_badBuckets} | GOOD%: {goodRatio:P0}\n" +
            rule.GetDebugText();
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