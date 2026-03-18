using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class NeckRotationRule : PoseRuleBase
{
    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Rotation Settings")]
    public bool rotateLeft = true;   // true = หันซ้าย, false = หันขวา
    public float requiredOffset = 0.05f;  // ระยะที่จมูกต้องเลื่อนไป
    public float smoothing = 0.2f;
    public override string PoseName => "Neck Rotation";
    public override float DurationSec => 20f;     // ✅ 30 วินาที
    public override int PassBonusScore => 100;

    private PoseLandmarkerResult _result;
    private bool _hasResult;
    private readonly object _resultLock = new object();

    private float _filteredOffset;

    public override void OnSessionStart()
    {
        _filteredOffset = 0f;
    }

    private void Awake()
    {
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();
        if (runner == null)
        {
            Debug.LogError("NeckRotationRule: ไม่พบ PoseLandmarkerRunner");
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

        NormalizedLandmark nose = default;
        NormalizedLandmark leftShoulder = default;
        NormalizedLandmark rightShoulder = default;
        bool ok = false;

        lock (_resultLock)
        {
            if (_hasResult && _result.poseLandmarks != null && _result.poseLandmarks.Count > 0)
            {
                var lm = _result.poseLandmarks[0].landmarks;
                if (lm != null &&
                    TryGetLm(lm, 0, out nose) &&       // nose
                    TryGetLm(lm, 11, out leftShoulder) &&
                    TryGetLm(lm, 12, out rightShoulder))
                {
                    ok = true;
                }
            }
        }

        if (!ok) return false;

        valid = true;

        float shoulderMidX = (leftShoulder.x + rightShoulder.x) * 0.5f;
        float rawOffset = nose.x - shoulderMidX;

        _filteredOffset = Mathf.Lerp(_filteredOffset, rawOffset, smoothing);

        if (rotateLeft)
            return _filteredOffset < -requiredOffset;
        else
            return _filteredOffset > requiredOffset;
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