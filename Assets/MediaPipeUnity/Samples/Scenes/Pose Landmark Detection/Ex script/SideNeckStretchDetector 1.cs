using UnityEngine;

using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class SideNeckStretchDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Target Tilt (deg)")]
    public float targetAngleDeg = 20f;   // เป้าหมายเอียงคอ 45 องศา
    public float toleranceDeg   = 8f;   // คลาดเคลื่อนได้ ± เท่านี้ (เช่น 10 = ช่วง 35-55)

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.20f;

    [Header("Session Test")]
    public KeyCode startKey = KeyCode.A;
    public float sessionDuration = 60f;   // 1 นาที
    public float checkInterval   = 5f;    // ทุก 5 วิ

    private PoseLandmarkerResult _result;
    private bool _hasResult;

    private float _filteredAngle;
    private float _lastRawAngle;

    // -------- Session state --------
    private bool _sessionActive;
    private float _sessionTimer;
    private float _bucketTimer;
    private int _bucketIndex; // 0..11

    // bucket accumulators (ในช่วง 5 วิ)
    private int _framesTotal;
    private int _framesValid;
    private int _framesCorrect;   // เข้าโซน ±45 ภายใน tolerance
    private float _sumAbsAngle;   // สำหรับดูความนิ่ง
    private float _sumSqAbsAngle;

    private readonly object _resultLock = new object();

    void Awake()
    {
        if (runner == null) runner = GetComponent<PoseLandmarkerRunner>();
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();

        if (runner == null)
        {
            Debug.LogError("[SideNeckStretchDetector] ไม่พบ PoseLandmarkerRunner ในซีน (ลองลากใส่ช่อง runner ใน Inspector)");
            enabled = false;
            return;
        }

        runner.OnPoseResult += OnPoseResult;
        Debug.Log("พร้อมแล้ว: กด A เพื่อเริ่มทดสอบ 1 นาที (ประเมินทุก 5 วิ) | โซนถูก ≈ ±45°");
    }

    void OnDestroy()
    {
        if (runner != null) runner.OnPoseResult -= OnPoseResult;
    }

    private void OnPoseResult(PoseLandmarkerResult result)
    {
        lock (_resultLock)
        {
            _result = result;
            _hasResult = true;
        }
    }

    void Update()
    {
        // กด A เพื่อเริ่มใหม่
        if (Input.GetKeyDown(startKey))
        {
            StartSession();
        }

        if (!_sessionActive) return;

        _sessionTimer += Time.deltaTime;
        _bucketTimer += Time.deltaTime;

        EvaluateFrame();

        // ครบ 5 วิ → ตัดเกรดช่วงนี้
        if (_bucketTimer >= checkInterval)
        {
            _bucketTimer -= checkInterval;
            GradeBucket();
            ResetBucket();
            _bucketIndex++;
        }

        // ครบ 60 วิ → จบ session
        if (_sessionTimer >= sessionDuration)
        {
            EndSession();
        }
    }

    void StartSession()
    {
        _sessionActive = true;
        _sessionTimer = 0f;
        _bucketTimer = 0f;
        _bucketIndex = 0;

        ResetBucket();

        _filteredAngle = 0f;
        _lastRawAngle = 0f;

        Debug.Log("▶ เริ่มทดสอบ 1 นาทีแล้ว (ประเมินทุก 5 วิ)...");
    }


    void EndSession()
    {
        _sessionActive = false;

        // เผื่อจบคาเฟรมสุดท้าย
        if (_framesTotal > 0)
        {
            GradeBucket(finalPartial: true);
        }

        Debug.Log("⏹ จบการทดสอบ 1 นาทีแล้ว — กด A เพื่อเริ่มใหม่");
    }

    void ResetBucket()
    {
        _framesTotal = 0;
        _framesValid = 0;
        _framesCorrect = 0;
        _sumAbsAngle = 0f;
        _sumSqAbsAngle = 0f;
    }

    bool TryGetLm(System.Collections.Generic.IList<NormalizedLandmark> lm, int idx, out NormalizedLandmark p)
    {
        p = default;
        if (lm == null) return false;
        if (idx < 0 || idx >= lm.Count) return false;
        p = lm[idx];
        return true;
    }

    void EvaluateFrame()
    {
        _framesTotal++;

        NormalizedLandmark lsP = default, rsP = default, leP = default, reP = default;
        bool ok = false;

        lock (_resultLock)
        {
            if (_hasResult && _result.poseLandmarks != null && _result.poseLandmarks.Count > 0)
            {
                var lm = _result.poseLandmarks[0].landmarks;
                if (lm != null
                    && TryGetLm(lm, 11, out lsP)
                    && TryGetLm(lm, 12, out rsP)
                    && TryGetLm(lm, 7,  out leP)
                    && TryGetLm(lm, 8,  out reP))
                {
                    ok = true;
                }
            }
        }

        if (!ok) return;
        _framesValid++;

        Vector3 ls = ToVec(lsP);
        Vector3 rs = ToVec(rsP);
        Vector3 le = ToVec(leP);
        Vector3 re = ToVec(reP);

        Vector3 shoulderMid = (ls + rs) * 0.5f;
        Vector3 earMid = (le + re) * 0.5f;
        Vector3 headVec = earMid - shoulderMid;

        // ใช้ dy แบบ Abs เพื่อลดปัญหาแกน y กลับทิศ/ติดลบ
        float dx = headVec.x;
        float dy = Mathf.Abs(headVec.y) + 1e-5f;

        // 0 = หัวตรง, เอียงมากขึ้น = องศามากขึ้น (ซ้าย/ขวาใช้เครื่องหมายจาก dx)
        float rawAngle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;

        // จำกัดช่วงเอียงคอ
        rawAngle = Mathf.Clamp(rawAngle, -80f, 80f);

        _lastRawAngle = rawAngle;

        // กรองสั่น (เอาแค่ครั้งเดียว)
        _filteredAngle = Mathf.Lerp(_filteredAngle, rawAngle, smoothing);

        float absA = Mathf.Abs(_filteredAngle);


        // ✅ “ถูก” เมื่อเข้าโซนใกล้ +45 หรือ -45 ภายใน tolerance
        bool inTarget =
            Mathf.Abs(_filteredAngle - targetAngleDeg) <= toleranceDeg ||
            Mathf.Abs(_filteredAngle + targetAngleDeg) <= toleranceDeg;

        if (inTarget) _framesCorrect++;

        // ความนิ่ง (std ของ abs(angle))
        _sumAbsAngle += absA;
        _sumSqAbsAngle += absA * absA;
    }

    void GradeBucket(bool finalPartial = false)
    {
        if (_framesValid < 5)
        {
            Debug.Log($"[{BucketLabel(finalPartial)}] BAD (pose ไม่ชัด/หลุดเฟรม)");
            return;
        }

        float correctRatio = (float)_framesCorrect / _framesValid; // 0..1

        float mean = _sumAbsAngle / _framesValid;
        float var = (_sumSqAbsAngle / _framesValid) - (mean * mean);
        float std = Mathf.Sqrt(Mathf.Max(0f, var));

        // เกณฑ์คะแนน
        string grade;
        if (correctRatio >= 0.80f && std <= 6.0f) grade = "EXCELLENT";
        else if (correctRatio >= 0.50f) grade = "GOOD";
        else grade = "BAD";

        int secFrom = _bucketIndex * (int)checkInterval;
        int secTo = secFrom + (int)checkInterval;

        Debug.Log($"[{secFrom:00}-{secTo:00}s] {grade} | correct={correctRatio:P0} | std≈{std:F1} | validFrames={_framesValid}");
        Debug.Log($"rawAngle={_lastRawAngle:F1} filtered={_filteredAngle:F1} | target=±{targetAngleDeg} tol=±{toleranceDeg}");
    }

    string BucketLabel(bool finalPartial)
    {
        if (finalPartial) return "FINAL";
        return $"{_bucketIndex * checkInterval:0}-{(_bucketIndex + 1) * checkInterval:0}s";
    }

    private static Vector3 ToVec(NormalizedLandmark p) => new Vector3(p.x, p.y, p.z);
}
