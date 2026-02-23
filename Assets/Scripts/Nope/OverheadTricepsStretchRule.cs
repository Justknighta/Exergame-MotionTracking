using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class OverheadTricepsStretchRule : PoseRuleBase
{
    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Direction")]
    public bool stretchLeftArm = true;
    // ✅ ติ๊ก = ยกแขนซ้าย
    // ❌ ไม่ติ๊ก = ยกแขนขวา

    [Header("Threshold")]
    public float aboveHeadOffset = 0.05f;  // ต้องสูงกว่าหัวแค่ไหน
    public float elbowAboveShoulder = 0.02f;
    public float bendThreshold = 0.12f;    // wrist ต้องเข้าใกล้หัวพอสมควร

    [Header("Smoothing")]
    [Range(0f,1f)] public float smoothing = 0.3f;

    public override string PoseName => "Overhead Triceps Stretch";
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

        NormalizedLandmark ls=default, rs=default;
        NormalizedLandmark le=default, re=default;
        NormalizedLandmark lw=default, rw=default;
        NormalizedLandmark earL=default, earR=default;

        bool ok = false;

        lock (_lock)
        {
            if (_hasResult && _result.poseLandmarks != null && _result.poseLandmarks.Count > 0)
            {
                var lm = _result.poseLandmarks[0].landmarks;
                if (lm != null &&
                    TryGet(lm,11,out ls) &&
                    TryGet(lm,12,out rs) &&
                    TryGet(lm,13,out le) &&
                    TryGet(lm,14,out re) &&
                    TryGet(lm,15,out lw) &&
                    TryGet(lm,16,out rw) &&
                    TryGet(lm,7,out earL) &&
                    TryGet(lm,8,out earR))
                {
                    ok = true;
                }
            }
        }

        if (!ok) return false;
        valid = true;

        var elbow = stretchLeftArm ? le : re;
        var wrist = stretchLeftArm ? lw : rw;
        var shoulder = stretchLeftArm ? ls : rs;

        float headY = (earL.y + earR.y) * 0.5f;

        // ต้องอยู่เหนือหัว
        bool wristAboveHead = wrist.y < headY - aboveHeadOffset;

        // ศอกต้องอยู่เหนือไหล่
        bool elbowAbove = elbow.y < shoulder.y - elbowAboveShoulder;

        // แขนต้องงอ (wrist ใกล้หัวมากกว่าไหล่)
        float wristToHead = Mathf.Abs(wrist.y - headY);
        float wristToShoulder = Mathf.Abs(wrist.y - shoulder.y);

        bool bentEnough = wristToHead < wristToShoulder - bendThreshold;

        float rawScore = (wristAboveHead && elbowAbove && bentEnough) ? 1f : 0f;
        _filteredScore = Mathf.Lerp(_filteredScore, rawScore, smoothing);

        return _filteredScore > 0.5f;
    }

    public override string GetDebugText()
    {
        return $"Overhead score: {_filteredScore:F2}";
    }

    private bool TryGet(System.Collections.Generic.IList<NormalizedLandmark> lm, int i, out NormalizedLandmark p)
    {
        p = default;
        if (lm == null || i < 0 || i >= lm.Count) return false;
        p = lm[i];
        return true;
    }
}