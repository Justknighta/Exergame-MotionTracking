using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class RhomboidStretchRule : PoseRuleBase
{
    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Thresholds")]
    [Tooltip("ระยะห่างระหว่างข้อมือ (normalized) ยิ่งน้อย = มือชิดกันมากขึ้น")]
    public float wristCloseThreshold = 0.10f;

    [Tooltip("มุมศอกขั้นต่ำเพื่อถือว่าแขนเหยียดตรง (deg) เช่น 160-175")]
    public float minElbowAngleDeg = 165f;

    [Tooltip("ข้อมือควรอยู่ใกล้แนวกลางลำตัว (ช่วยกันติดมั่ว) ยิ่งน้อยยิ่งต้องอยู่ตรงกลาง")]
    public float wristCenterMaxOffset = 0.20f;

    [Tooltip("ข้อมือไม่ควรต่ำกว่าไหล่มากเกินไป (กันกรณีปล่อยแขนลงล่าง)")]
    public float wristBelowShoulderMax = 0.18f;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.30f;

    public override string PoseName => "Rhomboid Stretch";
    public override float DurationSec => 30f;
    public override int PassBonusScore => 100;

    private PoseLandmarkerResult _result;
    private bool _hasResult;
    private readonly object _lock = new object();

    private float _filteredScore; // 0..1
    private float _lastWristDist;
    private float _lastElbowL, _lastElbowR;

    private void Awake()
    {
        if (runner == null) runner = GetComponent<PoseLandmarkerRunner>();
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();

        if (runner == null)
        {
            Debug.LogError("[RhomboidStretchRule] ไม่พบ PoseLandmarkerRunner (ลากใส่ช่อง runner ใน Inspector)");
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

    public override void OnSessionStart()
    {
        _filteredScore = 0f;
        _lastWristDist = 0f;
        _lastElbowL = 0f;
        _lastElbowR = 0f;
    }

    public override bool EvaluateThisFrame(out bool valid)
    {
        valid = false;

        NormalizedLandmark ls = default, rs = default;
        NormalizedLandmark le = default, re = default;
        NormalizedLandmark lw = default, rw = default;

        bool ok = false;

        lock (_lock)
        {
            if (_hasResult && _result.poseLandmarks != null && _result.poseLandmarks.Count > 0)
            {
                var lm = _result.poseLandmarks[0].landmarks;
                if (lm != null
                    && TryGet(lm, 11, out ls) // left shoulder
                    && TryGet(lm, 12, out rs) // right shoulder
                    && TryGet(lm, 13, out le) // left elbow
                    && TryGet(lm, 14, out re) // right elbow
                    && TryGet(lm, 15, out lw) // left wrist
                    && TryGet(lm, 16, out rw))// right wrist
                {
                    ok = true;
                }
            }
        }

        if (!ok) return false;
        valid = true;

        // --- 1) มือชิดกัน ---
        float wristDist = Dist2D(lw, rw);
        _lastWristDist = wristDist;
        bool wristsClose = wristDist <= wristCloseThreshold;

        // --- 2) แขนเหยียดตรง: มุมศอก ---
        // angle(shoulder - elbow - wrist)
        float elbowAngleL = AngleDeg(ToVec2(ls), ToVec2(le), ToVec2(lw));
        float elbowAngleR = AngleDeg(ToVec2(rs), ToVec2(re), ToVec2(rw));
        _lastElbowL = elbowAngleL;
        _lastElbowR = elbowAngleR;

        bool armsStraight = (elbowAngleL >= minElbowAngleDeg) && (elbowAngleR >= minElbowAngleDeg);

        // --- 3) มือใกล้แนวกลาง + อยู่ระดับอก/ไหล่ ---
        float shoulderMidX = (ls.x + rs.x) * 0.5f;
        float shoulderMidY = (ls.y + rs.y) * 0.5f;

        float wristMidX = (lw.x + rw.x) * 0.5f;
        float wristMidY = (lw.y + rw.y) * 0.5f;

        bool nearCenter = Mathf.Abs(wristMidX - shoulderMidX) <= wristCenterMaxOffset;
        bool notTooLow = (wristMidY - shoulderMidY) <= wristBelowShoulderMax; 
        // y มากขึ้น = ลงล่าง (โดยทั่วไป) → ถ้า wrist ต่ำกว่าไหล่มาก จะ fail

        bool poseOK = wristsClose && armsStraight && nearCenter && notTooLow;

        float rawScore = poseOK ? 1f : 0f;
        _filteredScore = Mathf.Lerp(_filteredScore, rawScore, smoothing);

        return _filteredScore > 0.5f;
    }

    public override string GetDebugText()
    {
        return $"Rhomboid score:{_filteredScore:F2} | wristDist:{_lastWristDist:F3} | elbowL/R:{_lastElbowL:F0}/{_lastElbowR:F0}";
    }

    private bool TryGet(System.Collections.Generic.IList<NormalizedLandmark> lm, int i, out NormalizedLandmark p)
    {
        p = default;
        if (lm == null || i < 0 || i >= lm.Count) return false;
        p = lm[i];
        return true;
    }

    private static Vector2 ToVec2(NormalizedLandmark p) => new Vector2(p.x, p.y);

    private static float Dist2D(NormalizedLandmark a, NormalizedLandmark b)
    {
        float dx = a.x - b.x;
        float dy = a.y - b.y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    // มุม ABC ที่ B
    private static float AngleDeg(Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 ba = (a - b).normalized;
        Vector2 bc = (c - b).normalized;
        float dot = Mathf.Clamp(Vector2.Dot(ba, bc), -1f, 1f);
        return Mathf.Acos(dot) * Mathf.Rad2Deg;
    }
}