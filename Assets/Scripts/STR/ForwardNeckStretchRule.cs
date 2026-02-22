using UnityEngine;

using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class ForwardNeckStretchRule : PoseRuleBase
{
    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Target (normalized by shoulder width)")]
    public float targetForward = 0.20f;   // เป้าหมายก้ม (ยิ่งมาก = ก้มมาก) ปรับได้
    public float tolerance = 0.05f;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.20f;

    public override string PoseName => "Forward Neck Stretch";
    public override float DurationSec => 30f;
    public override int PassBonusScore => 100;

    private PoseLandmarkerResult _result;
    private bool _hasResult;
    private readonly object _resultLock = new object();

    private float _filteredForward;
    private float _lastRawForward;

    public override void OnSessionStart()
    {
        _filteredForward = 0f;
        _lastRawForward = 0f;
    }

    private void Awake()
    {
        if (runner == null) runner = GetComponent<PoseLandmarkerRunner>();
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();

        if (runner == null)
        {
            Debug.LogError("[ForwardNeckStretchRule] ไม่พบ PoseLandmarkerRunner (ลากใส่ช่อง runner ใน Inspector)");
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

        NormalizedLandmark lsP = default, rsP = default, leP = default, reP = default;
        bool ok = false;

        lock (_resultLock)
        {
            if (_hasResult && _result.poseLandmarks != null && _result.poseLandmarks.Count > 0)
            {
                var lm = _result.poseLandmarks[0].landmarks;
                if (lm != null
                    && TryGetLm(lm, 11, out lsP) // left shoulder
                    && TryGetLm(lm, 12, out rsP) // right shoulder
                    && TryGetLm(lm, 7, out leP)  // left ear
                    && TryGetLm(lm, 8, out reP)) // right ear
                {
                    ok = true;
                }
            }
        }

        if (!ok) return false;
        valid = true;

        // ใช้ค่า normalized (x,y) โดยตรงพอ (ไม่ต้องแปลงเป็น Vector3 ก็ได้)
        float shoulderMidY = (lsP.y + rsP.y) * 0.5f;
        float earMidY = (leP.y + reP.y) * 0.5f;

        float shoulderWidth = Mathf.Abs(lsP.x - rsP.x) + 1e-5f;

        // MediaPipe: y มักจะ "มากขึ้นเมื่ออยู่ต่ำลง" บนจอ
        // ดังนั้น (earMidY - shoulderMidY) มากขึ้น = หูอยู่ต่ำลง = ก้มมากขึ้น
        float rawForward = (earMidY - shoulderMidY) / shoulderWidth;

        _lastRawForward = rawForward;
        _filteredForward = Mathf.Lerp(_filteredForward, rawForward, smoothing);

        bool inTarget = Mathf.Abs(_filteredForward - targetForward) <= tolerance;
        return inTarget;
    }

    public override string GetDebugText()
    {
        return $"Forward raw/filtered: {_lastRawForward:F3}/{_filteredForward:F3} | target={targetForward:F3} tol=±{tolerance:F3}";
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