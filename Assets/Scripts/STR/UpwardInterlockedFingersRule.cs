using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class UpwardInterlockedFingersRule : PoseRuleBase
{
    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Rule (Simple)")]
    [Tooltip("ศอกต้องสูงกว่าไหล่เท่าไหร่ (normalized y). ยิ่งน้อย = เข้มขึ้น")]
    public float elbowAboveShoulderOffset = 0.02f;

    [Tooltip("หัวต้องอยู่ 'ภายในช่วงแขน' เผื่อระยะได้เท่าไหร่ (normalized x)")]
    public float headBetweenMarginX = 0.05f;

    [Tooltip("กันมั่ว: ศอกสองข้างควรห่างกันอย่างน้อยเท่าไหร่ (normalized x)")]
    public float minElbowSpanX = 0.15f;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.35f;

    public override string PoseName => "Upward Facing (Elbows Up)";
    public override float DurationSec => 30f;
    public override int PassBonusScore => 100;

    private PoseLandmarkerResult _result;
    private bool _hasResult;
    private readonly object _lock = new object();

    private float _filteredScore;

    // debug
    private float _lastElbowSpan;
    private float _lastHeadX;
    private bool _lastElbowsAbove;
    private bool _lastHeadBetween;

    public override void OnSessionStart()
    {
        _filteredScore = 0f;
        _lastElbowSpan = 0f;
        _lastHeadX = 0f;
        _lastElbowsAbove = false;
        _lastHeadBetween = false;
    }

    private void Awake()
    {
        if (runner == null) runner = GetComponent<PoseLandmarkerRunner>();
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();

        if (runner == null)
        {
            Debug.LogError("[UpwardInterlockedFingersRule] ไม่พบ PoseLandmarkerRunner (ลากใส่ช่อง runner ใน Inspector)");
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

        NormalizedLandmark ls = default, rs = default;
        NormalizedLandmark le = default, re = default;
        NormalizedLandmark earL = default, earR = default;

        bool ok = false;

        lock (_lock)
        {
            if (_hasResult && _result.poseLandmarks != null && _result.poseLandmarks.Count > 0)
            {
                var lm = _result.poseLandmarks[0].landmarks;
                if (lm != null
                    && TryGet(lm, 11, out ls)   // left shoulder
                    && TryGet(lm, 12, out rs)   // right shoulder
                    && TryGet(lm, 13, out le)   // left elbow
                    && TryGet(lm, 14, out re)   // right elbow
                    && TryGet(lm, 7, out earL)  // left ear
                    && TryGet(lm, 8, out earR)) // right ear
                {
                    ok = true;
                }
            }
        }

        if (!ok) return false;
        valid = true;

        // ✅ 1) ศอกสูงกว่าไหล่ (หมายเหตุ: ใน normalized y ของ mediapipe -> ค่าน้อยคืออยู่สูง)
        bool leftElbowAbove  = le.y < ls.y - elbowAboveShoulderOffset;
        bool rightElbowAbove = re.y < rs.y - elbowAboveShoulderOffset;
        bool elbowsAbove = leftElbowAbove && rightElbowAbove;
        _lastElbowsAbove = elbowsAbove;

        // ✅ 2) หัวอยู่ระหว่างแขน (ใช้ earMidX)
        float headX = (earL.x + earR.x) * 0.5f;
        _lastHeadX = headX;

        float minElbowX = Mathf.Min(le.x, re.x);
        float maxElbowX = Mathf.Max(le.x, re.x);
        float spanX = maxElbowX - minElbowX;
        _lastElbowSpan = spanX;

        bool elbowSpanOk = spanX >= minElbowSpanX;

        bool headBetween =
            headX >= (minElbowX - headBetweenMarginX) &&
            headX <= (maxElbowX + headBetweenMarginX);

        _lastHeadBetween = headBetween;

        bool poseOK = elbowsAbove && elbowSpanOk && headBetween;

        float rawScore = poseOK ? 1f : 0f;
        _filteredScore = Mathf.Lerp(_filteredScore, rawScore, smoothing);

        return _filteredScore > 0.5f;
    }

    public override string GetDebugText()
    {
        return
            $"Upward score:{_filteredScore:F2} | elbowsAbove:{_lastElbowsAbove} | headBetween:{_lastHeadBetween}\n" +
            $"spanX:{_lastElbowSpan:F3} | headX:{_lastHeadX:F3}";
    }

    private bool TryGet(System.Collections.Generic.IList<NormalizedLandmark> lm, int i, out NormalizedLandmark p)
    {
        p = default;
        if (lm == null || i < 0 || i >= lm.Count) return false;
        p = lm[i];
        return true;
    }
}