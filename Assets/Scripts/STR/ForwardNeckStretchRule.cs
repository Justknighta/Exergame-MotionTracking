using UnityEngine;

using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class ForwardBackwardNeckStretchRule : PoseRuleBase
{
    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Direction")]
    public bool stretchForward = true;
    // ✅ ติ๊ก = ก้มคอ (Forward / Chin to chest)
    // ❌ ไม่ติ๊ก = เงยคอ (Backward / Look up)

    [Header("Threshold (delta from neutral)")]
    public float forwardThreshold = 0.20f;   // ✅ ก้มเกินค่านี้ = ผ่าน
    public float backwardThreshold = 0.10f;  // ✅ เงยเกินค่านี้ = ผ่าน

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.3f; // เพิ่มนิดให้เด้งน้อยลง

    [Header("Calibration")]
    public float calibrateSeconds = 0.7f; // ช่วงแรกให้ผู้เล่น “หัวตรง” เพื่อเก็บ baseline

    [Header("If direction feels reversed")]
    public bool invertDelta = false; // ถ้าก้มแล้วควรเป็น + แต่ดันเป็น - ให้ติ๊กอันนี้

    public override string PoseName => stretchForward ? "Forward Neck Stretch" : "Backward Neck Stretch";
    public override float DurationSec => 30f;
    public override int PassBonusScore => 100;

    private PoseLandmarkerResult _result;
    private bool _hasResult;
    private readonly object _resultLock = new object();

    private float _filteredDelta;
    private float _lastRawDelta;

    // baseline calibration
    private float _baselineRaw;
    private float _calibTimer;
    private int _calibCount;
    private float _calibSum;
    private bool _baselineReady;

    public override void OnSessionStart()
    {
        _filteredDelta = 0f;
        _lastRawDelta = 0f;

        _baselineRaw = 0f;
        _calibTimer = 0f;
        _calibCount = 0;
        _calibSum = 0f;
        _baselineReady = false;
    }

    private void Awake()
    {
        if (runner == null) runner = GetComponent<PoseLandmarkerRunner>();
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();

        if (runner == null)
        {
            Debug.LogError("[ForwardBackwardNeckStretchRule] ไม่พบ PoseLandmarkerRunner (ลากใส่ช่อง runner ใน Inspector)");
            enabled = false;
            return;
        }

        runner.OnPoseResult += OnPoseResult;
    }

    private void OnDestroy()
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

    public override bool EvaluateThisFrame(out bool valid)
    {
        valid = false;

        NormalizedLandmark ls = default, rs = default, le = default, re = default;
        bool ok = false;

        lock (_resultLock)
        {
            if (_hasResult && _result.poseLandmarks != null && _result.poseLandmarks.Count > 0)
            {
                var lm = _result.poseLandmarks[0].landmarks;
                if (lm != null
                    && TryGetLm(lm, 11, out ls) // left shoulder
                    && TryGetLm(lm, 12, out rs) // right shoulder
                    && TryGetLm(lm, 7, out le)  // left ear
                    && TryGetLm(lm, 8, out re)) // right ear
                {
                    ok = true;
                }
            }
        }

        if (!ok) return false;
        valid = true;

        // --- raw metric: earMidY relative to shoulderMidY normalized by shoulder width (with clamp) ---
        float shoulderMidY = (ls.y + rs.y) * 0.5f;
        float earMidY = (le.y + re.y) * 0.5f;

        float shoulderWidth = Mathf.Abs(ls.x - rs.x);
        shoulderWidth = Mathf.Max(shoulderWidth, 0.15f); // กันหารแล้วเด้งเวอร์

        float raw = (earMidY - shoulderMidY) / shoulderWidth;

        // --- calibration baseline (ผู้เล่นควรหัวตรงช่วงแรก) ---
        if (!_baselineReady)
        {
            _calibTimer += Time.deltaTime;
            _calibSum += raw;
            _calibCount++;

            if (_calibTimer >= calibrateSeconds && _calibCount > 0)
            {
                _baselineRaw = _calibSum / _calibCount;
                _baselineReady = true;
            }

            _lastRawDelta = 0f;
            _filteredDelta = Mathf.Lerp(_filteredDelta, 0f, smoothing);
            return false; // ระหว่างคาลิเบรตยังไม่ให้ผ่าน/ไม่ผ่าน
        }

        // --- delta from neutral ---
        float delta = raw - _baselineRaw;
        if (invertDelta) delta = -delta;

        _lastRawDelta = delta;
        _filteredDelta = Mathf.Lerp(_filteredDelta, delta, smoothing);

        // ✅ NEW: ใช้ threshold ไม่ใช้ target±tol
        bool inTarget;
        if (stretchForward)
        {
            // ก้ม: delta ต้อง "มากพอ" (>= threshold)
            inTarget = _filteredDelta >= forwardThreshold;
        }
        else
        {
            // เงย: delta ต้อง "น้อยพอในทิศตรงข้าม" (<= -threshold)
            inTarget = _filteredDelta <= -backwardThreshold;
        }

        return inTarget;
    }

    public override string GetDebugText()
    {
        string dir = stretchForward ? "FORWARD" : "BACKWARD";
        string cal = _baselineReady ? "CAL:OK" : $"CAL:{_calibTimer:F1}/{calibrateSeconds:F1}s";
        string thr = stretchForward ? $">={forwardThreshold:F2}" : $"<={-backwardThreshold:F2}";
        return $"{dir} ({cal}) delta raw/filtered: {_lastRawDelta:F3}/{_filteredDelta:F3} | thr {thr}";
    }

    private bool TryGetLm(System.Collections.Generic.IList<NormalizedLandmark> lm, int idx, out NormalizedLandmark p)
    {
        p = default;
        if (lm == null) return false;
        if (idx < 0 || idx >= lm.Count) return false;
        p = lm[idx];
        return true;
    }
}