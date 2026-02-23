using UnityEngine;
using TMPro;

public class TimedPoseEvaluator : MonoBehaviour
{
    [Header("Rule (ท่าที่จะตรวจ)")]
    [SerializeField] private PoseRuleBase rule;

    [Header("Session")]
    public KeyCode startKey = KeyCode.A;
    public float checkInterval = 5f;

    [Header("Start Gate (wait until pose is correct)")]
    public bool waitForCorrectPoseBeforeStart = true;
    public float requiredHoldBeforeStart = 0.5f; // ต้องค้างท่าถูกกี่วิ ก่อนเริ่มนับจริง

    [Header("Speed Bonus (eligible if start is quick)")]
    public float maxWaitTime = 10f;     // ✅ ภายในกี่วิถึง "มีสิทธิ์" ได้โบนัส (<=0 = ปิด)
    public int speedBonusScore = 100;   // ✅ โบนัส (จะได้จริงเฉพาะตอน PASS)

    [Header("Pass Rule")]
    [Range(0f, 1f)] public float passThreshold = 0.50f;
    [Range(0f, 1f)] public float bucketGoodMinCorrectRatio = 0.50f;

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text resultText;

    // runtime (มาจาก rule)
    private float _durationRuntime = 60f;
    private int _bonusRuntime = 100;

    // session/bucket state
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

    // waiting gate state
    private float _waitTimer;
    private float _holdCorrectTimer;

    // ✅ โบนัสแบบ "มีสิทธิ์" (จะบวกจริงตอน PASS เท่านั้น)
    private bool _eligibleSpeedBonus;

    private enum Phase { Idle, WaitingForPose, Active }
    private Phase _phase = Phase.Idle;

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

        if (_phase == Phase.WaitingForPose)
        {
            WaitUntilPoseCorrect();
            return;
        }

        if (_phase != Phase.Active) return;

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

        // reset everything
        _sessionTimer = 0f;
        _bucketTimer = 0f;
        _bucketIndex = 0;

        _goodBuckets = 0;
        _badBuckets = 0;

        ResetBucket();

        _waitTimer = 0f;
        _holdCorrectTimer = 0f;
        _eligibleSpeedBonus = false;

        rule.OnSessionStart();
        WriteResult("");

        if (waitForCorrectPoseBeforeStart)
        {
            _phase = Phase.WaitingForPose;

            string bonusInfo = (maxWaitTime > 0f)
                ? $"\n🎁 ทำทันภายใน {maxWaitTime:0}s = มีสิทธิ์โบนัส +{speedBonusScore} (ได้จริงเมื่อผ่านเท่านั้น)"
                : "";

            WriteStatus($"⏳ เข้าท่าให้ถูกก่อน แล้วค้าง {requiredHoldBeforeStart:0.0}s เพื่อเริ่มนับเวลา\nท่า: {rule.PoseName}{bonusInfo}");
            return;
        }

        _phase = Phase.Active;
        WriteStatus($"▶ เริ่ม: {rule.PoseName} ({_durationRuntime:0}s)");
    }

    private void WaitUntilPoseCorrect()
    {
        if (rule == null)
        {
            _phase = Phase.Idle;
            WriteStatus("❌ ไม่มี Rule");
            return;
        }

        _waitTimer += Time.deltaTime;

        bool valid;
        bool correct = rule.EvaluateThisFrame(out valid);

        // ต้อง valid + correct ต่อเนื่องเท่านั้นถึงนับ hold
        if (valid && correct) _holdCorrectTimer += Time.deltaTime;
        else _holdCorrectTimer = 0f;

        // UI ระหว่างรอ
        if (statusText != null)
        {
            string bonusLine = "";
            if (maxWaitTime > 0f)
            {
                float remain = Mathf.Max(0f, maxWaitTime - _waitTimer);
                bonusLine = $"\nโบนัส: ทำทันภายใน {maxWaitTime:0}s (เหลือ {remain:0.0}s)";
            }

            statusText.text =
                $"⏳ รอเข้าท่าให้ถูก: {rule.PoseName}\n" +
                $"ค้างถูก: {_holdCorrectTimer:0.0}/{requiredHoldBeforeStart:0.0}s" +
                bonusLine + "\n" +
                (rule != null ? rule.GetDebugText() : "");
        }

        // ครบเงื่อนไข → เริ่มนับจริง
        if (_holdCorrectTimer >= requiredHoldBeforeStart)
        {
            // ✅ แค่ "บันทึกสิทธิ์โบนัส" ยังไม่ AddScore
            _eligibleSpeedBonus = (maxWaitTime > 0f && _waitTimer <= maxWaitTime);

            WriteResult(_eligibleSpeedBonus
                ? $"🎁 ทำทัน! โบนัส +{speedBonusScore} (จะได้เมื่อผ่านเท่านั้น)"
                : "");

            _phase = Phase.Active;

            // เริ่มนับจริงจากศูนย์ ณ ตอนที่เข้าท่าถูก
            _sessionTimer = 0f;
            _bucketTimer = 0f;
            _bucketIndex = 0;

            _goodBuckets = 0;
            _badBuckets = 0;
            ResetBucket();

            WriteStatus($"▶ เริ่มนับเวลาแล้ว: {rule.PoseName} ({_durationRuntime:0}s)");
            return;
        }

        // ✅ ไม่ยกเลิกเมื่อรอนาน (ไม่จำกัดเวลา)
    }

    public void EndSession()
    {
        if (_phase == Phase.Idle) return;

        // ถ้ายังรออยู่ ให้จบเฉย ๆ
        if (_phase == Phase.WaitingForPose)
        {
            _phase = Phase.Idle;
            rule.OnSessionEnd();
            WriteStatus("⏹ ยกเลิกก่อนเริ่มนับเวลา — กด A เพื่อเริ่มใหม่");
            return;
        }

        // ตัดเศษ bucket สุดท้าย
        if (_framesTotal > 0)
            GradeBucketAndCount(finalPartial: true);

        _phase = Phase.Idle;
        rule.OnSessionEnd();

        int totalBuckets = Mathf.Max(1, _goodBuckets + _badBuckets);
        float goodRatio = (float)_goodBuckets / totalBuckets;
        bool pass = goodRatio >= passThreshold;

        if (pass)
        {
            int totalAdd = _bonusRuntime + (_eligibleSpeedBonus ? speedBonusScore : 0);

            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddScore(totalAdd);

            string bonusText = _eligibleSpeedBonus ? $"\n🎁 โบนัสเร็ว +{speedBonusScore}" : "";
            WriteResult($"✅ ผ่าน!\nGOOD {_goodBuckets}/{totalBuckets} ({goodRatio:P0})\n+{_bonusRuntime} คะแนน{bonusText}\nรวม +{totalAdd} คะแนน");
        }
        else
        {
            // ✅ ไม่ผ่าน = ไม่ได้คะแนนเลย (รวมถึงโบนัส)
            WriteResult($"❌ ไม่ผ่าน\nGOOD {_goodBuckets}/{totalBuckets} ({goodRatio:P0})\n+0 คะแนน");
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

        // หลุด tracking = นับเป็นผิด (BAD)
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