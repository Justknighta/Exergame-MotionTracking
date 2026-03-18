using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class CrossBodyArmStretchRule : PoseRuleBase
{
    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Direction")]
    public bool stretchLeftArm = true;
    // ✅ ติ๊ก = ดึงแขนซ้ายข้ามอก
    // ❌ ไม่ติ๊ก = ดึงแขนขวาข้ามอก

    [Header("Threshold")]
    public float crossThreshold = 0.05f;  // ระยะที่ข้อมือต้องข้ามลำตัว
    public float heightTolerance = 0.12f; // ข้อมือไม่ควรต่ำกว่าไหล่มาก

    [Header("Smoothing")]
    [Range(0f,1f)] public float smoothing = 0.3f;

    public override string PoseName => "Cross-body Arm Stretch";
    public override float DurationSec => 20f;
    public override int PassBonusScore => 100;

    private PoseLandmarkerResult _result;
    private bool _hasResult;
    private readonly object _lock = new object();

    private float _filteredScore;

    private void Awake()
    {
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();
        if (runner == null)
        {
            Debug.LogError("PoseLandmarkerRunner not found");
            enabled = false;
            return;
        }
        runner.OnPoseResult += OnPoseResult;
    }

    private void OnDestroy()
    {
        if (runner != null) runner.OnPoseResult -= OnPoseResult;
    }

    private void OnPoseResult(PoseLandmarkerResult r)
    {
        lock (_lock)
        {
            _result = r;
            _hasResult = true;
        }
    }

    public override bool EvaluateThisFrame(out bool valid)
    {
        valid = false;

        NormalizedLandmark ls=default, rs=default, lw=default, rw=default;
        bool ok = false;

        lock (_lock)
        {
            if (_hasResult && _result.poseLandmarks != null && _result.poseLandmarks.Count > 0)
            {
                var lm = _result.poseLandmarks[0].landmarks;
                if (lm != null
                    && TryGet(lm,11,out ls)
                    && TryGet(lm,12,out rs)
                    && TryGet(lm,15,out lw)
                    && TryGet(lm,16,out rw))
                {
                    ok = true;
                }
            }
        }

        if (!ok) return false;
        valid = true;

        float shoulderMidX = (ls.x + rs.x) * 0.5f;
        float shoulderMidY = (ls.y + rs.y) * 0.5f;

        float wristX = stretchLeftArm ? lw.x : rw.x;
        float wristY = stretchLeftArm ? lw.y : rw.y;

        float crossAmount = stretchLeftArm
            ? (shoulderMidX - wristX)   // ซ้ายต้องข้ามไปขวา
            : (wristX - shoulderMidX);  // ขวาต้องข้ามไปซ้าย

        float heightDiff = Mathf.Abs(wristY - shoulderMidY);

        float rawScore = crossAmount;
        _filteredScore = Mathf.Lerp(_filteredScore, rawScore, smoothing);

        bool crossedEnough = _filteredScore >= crossThreshold;
        bool heightOK = heightDiff <= heightTolerance;

        return crossedEnough && heightOK;
    }

    public override string GetDebugText()
    {
        return $"CrossBody score: {_filteredScore:F3}";
    }

    private bool TryGet(System.Collections.Generic.IList<NormalizedLandmark> lm, int i, out NormalizedLandmark p)
    {
        p = default;
        if (lm == null || i < 0 || i >= lm.Count) return false;
        p = lm[i];
        return true;
    }
}