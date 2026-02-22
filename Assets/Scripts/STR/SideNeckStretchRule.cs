using UnityEngine;

using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class SideNeckStretchRule : PoseRuleBase
{
    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Target Tilt (deg)")]
    public float targetAngleDeg = 20f;
    public float toleranceDeg = 8f;

    [Header("Direction")]
    public bool stretchLeft = true; // ✅ ติ๊ก = ซ้าย, ไม่ติ๊ก = ขวา

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.20f;

    public override string PoseName => "Side Neck Stretch";
    public override float DurationSec => 60f;
    public override int PassBonusScore => 100;

    private PoseLandmarkerResult _result;
    private bool _hasResult;
    private readonly object _resultLock = new object();

    private float _filteredAngle;
    private float _lastRawAngle;

    public override void OnSessionStart()
    {
        _filteredAngle = 0f;
        _lastRawAngle = 0f;
    }

    private void Awake()
    {
        if (runner == null) runner = GetComponent<PoseLandmarkerRunner>();
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();

        if (runner == null)
        {
            Debug.LogError("[SideNeckStretchRule] ไม่พบ PoseLandmarkerRunner (ลากใส่ช่อง runner ใน Inspector)");
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
                    && TryGetLm(lm, 11, out lsP)
                    && TryGetLm(lm, 12, out rsP)
                    && TryGetLm(lm, 7, out leP)
                    && TryGetLm(lm, 8, out reP))
                {
                    ok = true;
                }
            }
        }

        if (!ok) return false;
        valid = true;

        Vector3 ls = ToVec(lsP);
        Vector3 rs = ToVec(rsP);
        Vector3 le = ToVec(leP);
        Vector3 re = ToVec(reP);

        Vector3 shoulderMid = (ls + rs) * 0.5f;
        Vector3 earMid = (le + re) * 0.5f;
        Vector3 headVec = earMid - shoulderMid;

        float dx = headVec.x;
        float dy = Mathf.Abs(headVec.y) + 1e-5f;

        float rawAngle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;
        rawAngle = Mathf.Clamp(rawAngle, -80f, 80f);

        _lastRawAngle = rawAngle;
        _filteredAngle = Mathf.Lerp(_filteredAngle, rawAngle, smoothing);

        // ✅ เลือกข้าง: ซ้าย = มุมติดลบ, ขวา = มุมบวก
        float desired = stretchLeft ? -targetAngleDeg : +targetAngleDeg;
        bool inTarget = Mathf.Abs(_filteredAngle - desired) <= toleranceDeg;

        return inTarget;
    }

    public override string GetDebugText()
    {
        string dir = stretchLeft ? "LEFT" : "RIGHT";
        float desired = stretchLeft ? -targetAngleDeg : +targetAngleDeg;
        return $"SideNeck({dir}) raw/filtered: {_lastRawAngle:F1}/{_filteredAngle:F1} | target={desired:F1} tol=±{toleranceDeg}";
    }

    private bool TryGetLm(System.Collections.Generic.IList<NormalizedLandmark> lm, int idx, out NormalizedLandmark p)
    {
        p = default;
        if (lm == null) return false;
        if (idx < 0 || idx >= lm.Count) return false;
        p = lm[idx];
        return true;
    }

    private static Vector3 ToVec(NormalizedLandmark p) => new Vector3(p.x, p.y, p.z);
}