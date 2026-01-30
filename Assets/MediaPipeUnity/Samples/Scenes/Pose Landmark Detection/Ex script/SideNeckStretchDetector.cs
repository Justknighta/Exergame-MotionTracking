using UnityEngine;

using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class SideNeckStretchDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Thresholds (deg)")]
    public float enterAngleDeg = 15f;
    public float exitAngleDeg  = 12f;
    public float holdSeconds   = 0.6f;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.15f;

    [Header("Session Test")]
    public KeyCode startKey = KeyCode.A;
    public float sessionDuration = 60f;
    public float checkInterval   = 5f;

    private PoseLandmarkerResult _result;
    private bool _hasResult;

    private float _filteredAngle;
    private float _lastRawAngle;

    // -------- Session state --------
    private bool _sessionActive;
    private float _sessionTimer;
    private float _bucketTimer;
    private int _bucketIndex;

    // bucket accumulators
    private int _framesTotal;
    private int _framesValid;
    private int _framesCorrect;
    private float _sumAbsAngle;
    private float _sumSqAbsAngle;

    private readonly object _resultLock = new object();

    bool TryGetLm(System.Collections.Generic.IList<NormalizedLandmark> lm, int idx, out NormalizedLandmark p)
    {
        p = default;
        if (lm == null) return false;
        if (idx < 0 || idx >= lm.Count) return false;
        p = lm[idx];
        return true;
    }

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
        Debug.Log("พร้อมแล้ว: กด A เพื่อเริ่มทดสอบ 1 นาที (ประเมินทุก 5 วิ)");
    }

    void OnDestroy()
    {
        if (runner != null) runner.OnPoseResult -= OnPoseResult;
    }

    // ✅ เหลืออันเดียว (มี lock)
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
        if (Input.GetKeyDown(startKey))
        {
            StartSession();
        }

        if (!_sessionActive) return;

        _sessionTimer += Time.deltaTime;
        _bucketTimer += Time.deltaTime;

        EvaluateFrame();

        if (_bucketTimer >= checkInterval)
        {
            _bucketTimer -= checkInterval;
            GradeBucket();
            ResetBucket();
            _bucketIndex++;
        }

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
        Debug.Log("▶ เริ่มทดสอบ 1 นาทีแล้ว (ประเมินทุก 5 วิ)...");
    }

    void EndSession()
    {
        _sessionActive = false;

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

    void EvaluateFrame()
    {
        _framesTotal++;

        System.Collections.Generic.List<NormalizedLandmark> lmSnapshot = null;

        lock (_resultLock)
        {
            if (!_hasResult || _result.poseLandmarks == null || _result.poseLandmarks.Count == 0)
                return;

            var lm = _result.poseLandmarks[0].landmarks;
            if (lm != null)
                lmSnapshot = new System.Collections.Generic.List<NormalizedLandmark>(lm);
        }

        if (lmSnapshot == null) return;

        const int LEFT_EAR = 7, RIGHT_EAR = 8, LEFT_SHOULDER = 11, RIGHT_SHOULDER = 12;

        if (!TryGetLm(lmSnapshot, LEFT_SHOULDER, out var lsP) ||
            !TryGetLm(lmSnapshot, RIGHT_SHOULDER, out var rsP) ||
            !TryGetLm(lmSnapshot, LEFT_EAR, out var leP) ||
            !TryGetLm(lmSnapshot, RIGHT_EAR, out var reP))
        {
            return;
        }

        _framesValid++;

        Vector3 ls = ToVec(lsP);
        Vector3 rs = ToVec(rsP);
        Vector3 le = ToVec(leP);
        Vector3 re = ToVec(reP);

        Vector3 shoulderMid = (ls + rs) * 0.5f;
        Vector3 earMid = (le + re) * 0.5f;
        Vector3 headVec = earMid - shoulderMid;

        float rawAngle = Mathf.Atan2(headVec.x, headVec.y) * Mathf.Rad2Deg;
        rawAngle = Mathf.Clamp(rawAngle, -90f, 90f);

        _lastRawAngle = rawAngle;

        _filteredAngle = Mathf.Lerp(_filteredAngle, rawAngle, smoothing);

        float absA = Mathf.Abs(_filteredAngle);

        if (absA >= enterAngleDeg) _framesCorrect++;

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

        float correctRatio = (float)_framesCorrect / _framesValid;

        float mean = _sumAbsAngle / _framesValid;
        float var = (_sumSqAbsAngle / _framesValid) - (mean * mean);
        float std = Mathf.Sqrt(Mathf.Max(0f, var));

        string grade;
        if (correctRatio >= 0.80f && std <= 6.0f) grade = "EXCELLENT";
        else if (correctRatio >= 0.50f) grade = "GOOD";
        else grade = "BAD";

        int secFrom = _bucketIndex * (int)checkInterval;
        int secTo = secFrom + (int)checkInterval;

        Debug.Log($"[{secFrom:00}-{secTo:00}s] {grade} | correct={correctRatio:P0} | std≈{std:F1} | validFrames={_framesValid}");
        Debug.Log($"rawAngle={_lastRawAngle:F1} filtered={_filteredAngle:F1}");
    }

    string BucketLabel(bool finalPartial)
    {
        if (finalPartial) return "FINAL";
        return $"{_bucketIndex * checkInterval:0}-{(_bucketIndex + 1) * checkInterval:0}s";
    }

    private static Vector3 ToVec(NormalizedLandmark p) => new Vector3(p.x, p.y, p.z);
}
