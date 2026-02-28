using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class ArmFrontRaiseRule : PoseRuleBase
{
    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Front Raise Settings")]
    [Tooltip("ศอกต้องเหยียดตรง (ใช้โหมด Bend 0-20)")]
    public bool requireElbowStraight = true;

    [Tooltip("ความงอของศอกที่ยอมรับได้ (0 = เหยียดสุด)")]
    public float maxElbowBendDeg = 20f;   // ✅ ตามที่ขอ

    [Tooltip("ข้อมือต้องสูงกว่าไหล่เล็กน้อย")]
    public float wristAboveShoulderMargin = 0.02f;

    [Tooltip("ข้อมือควรอยู่ใกล้แนวกึ่งกลางลำตัว")]
    public float maxWristCenterRatio = 0.55f;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.4f;

    public override string PoseName => "Arm Front Raise";
    public override float DurationSec => 20f;
    public override int PassBonusScore => 100;

    private PoseLandmarkerResult _result;
    private bool _hasResult;
    private readonly object _lock = new object();

    private float _rawLeftElbow, _rawRightElbow;
    private float _fLeftElbow, _fRightElbow;

    private float _bendL, _bendR;
    private float _centerRatioL, _centerRatioR;

    public override void OnSessionStart()
    {
        _bendL = _bendR = 0f;
        _fLeftElbow = _fRightElbow = 0f;
    }

    private void Awake()
    {
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();

        if (runner == null)
        {
            Debug.LogError("[ArmFrontRaiseRule] PoseLandmarkerRunner not found");
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

        if (!_hasResult || _result.poseLandmarks == null || _result.poseLandmarks.Count == 0)
            return false;

        var lm = _result.poseLandmarks[0].landmarks;

        if (lm == null || lm.Count < 17)
            return false;

        valid = true;

        var ls = lm[11];
        var rs = lm[12];
        var le = lm[13];
        var re = lm[14];
        var lw = lm[15];
        var rw = lm[16];

        // -------------------------
        // 1️⃣ ศอกเหยียด (Bend 0-20)
        // -------------------------
        if (requireElbowStraight)
        {
            _rawLeftElbow  = JointAngle(ls, le, lw);
            _rawRightElbow = JointAngle(rs, re, rw);

            _fLeftElbow  = Mathf.Lerp(_fLeftElbow,  _rawLeftElbow,  smoothing);
            _fRightElbow = Mathf.Lerp(_fRightElbow, _rawRightElbow, smoothing);

            // Bend = 180 - angle
            _bendL = Mathf.Clamp(180f - _fLeftElbow,  0f, 180f);
            _bendR = Mathf.Clamp(180f - _fRightElbow, 0f, 180f);

            if (_bendL > maxElbowBendDeg || _bendR > maxElbowBendDeg)
                return false;
        }

        // -------------------------
        // 2️⃣ ข้อมือต้องสูงกว่าไหล่
        // -------------------------
        if (lw.y > ls.y - wristAboveShoulderMargin) return false;
        if (rw.y > rs.y - wristAboveShoulderMargin) return false;

        // -------------------------
        // 3️⃣ ข้อมืออยู่ใกล้กึ่งกลาง
        // -------------------------
        float shoulderWidth = Mathf.Abs(rs.x - ls.x);

        _centerRatioL = Mathf.Abs(lw.x - ls.x) / shoulderWidth;
        _centerRatioR = Mathf.Abs(rw.x - rs.x) / shoulderWidth;

        if (_centerRatioL > maxWristCenterRatio) return false;
        if (_centerRatioR > maxWristCenterRatio) return false;

        return true;
    }

    public override string GetDebugText()
    {
        return $"FrontRaise elbowBend(L/R): {_bendL:F0}/{_bendR:F0} <= {maxElbowBendDeg:F0} | centerRatio(L/R): {_centerRatioL:F2}/{_centerRatioR:F2}";
    }

    private float JointAngle(NormalizedLandmark a, NormalizedLandmark b, NormalizedLandmark c)
    {
        Vector2 ba = new Vector2(a.x - b.x, a.y - b.y);
        Vector2 bc = new Vector2(c.x - b.x, c.y - b.y);

        if (ba.sqrMagnitude < 1e-6f || bc.sqrMagnitude < 1e-6f)
            return 0f;

        return Vector2.Angle(ba, bc);
    }
}