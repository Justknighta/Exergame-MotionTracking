using UnityEngine;

using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class LPoseRule : PoseRuleBase
{
    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Core (Easy) شروطหลัก")]
    [Tooltip("ช่วงมุมศอก (องศา) สำหรับ L pose (ใกล้ 90 แต่ให้กว้างเพื่อผ่านง่าย)")]
    public float minElbowAngleDeg = 60f;
    public float maxElbowAngleDeg = 130f;

    [Tooltip("ศอกต้องชิดลำตัว: ใช้ระยะ (elbow-to-shoulder) เทียบกับ shoulderWidth เพื่อ normalize")]
    public float maxElbowOutRatio = 0.35f; // ยิ่งมากยิ่งง่าย (0.30-0.45)

    [Tooltip("ข้อมือควรออกด้านข้างจากศอก (เพื่อให้เป็นรูป L)")]
    public float minWristOutRatio = 0.20f; // ยิ่งมากยิ่งเข้ม (0.15-0.30)

    [Header("Optional: Forearm Horizontal (กันมั่ว)")]
    public bool requireForearmNearHorizontal = false;
    public float maxForearmVerticalDeviationDeg = 35f; // ยิ่งมากยิ่งง่าย

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.40f;

    public override string PoseName => "YTWL - L (Easy)";
    public override float DurationSec => 20f;
    public override int PassBonusScore => 100;

    private PoseLandmarkerResult _result;
    private bool _hasResult;
    private readonly object _lock = new object();

    private float _rawLElbow, _rawRElbow;
    private float _fLElbow, _fRElbow;

    private float _rawElbowOutL, _rawElbowOutR; // normalized by shoulder width
    private float _rawWristOutL, _rawWristOutR; // normalized by shoulder width

    public override void OnSessionStart()
    {
        _rawLElbow = _rawRElbow = 0f;
        _fLElbow = _fRElbow = 0f;
        _rawElbowOutL = _rawElbowOutR = 0f;
        _rawWristOutL = _rawWristOutR = 0f;
    }

    private void Awake()
    {
        if (runner == null) runner = GetComponent<PoseLandmarkerRunner>();
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();

        if (runner == null)
        {
            Debug.LogError("[LPoseRule] ไม่พบ PoseLandmarkerRunner (ลากใส่ช่อง runner ใน Inspector)");
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
        lock (_lock)
        {
            _result = result;
            _hasResult = true;
        }
    }

    public override bool EvaluateThisFrame(out bool valid)
    {
        valid = false;

        NormalizedLandmark lsP = default, rsP = default, leP = default, reP = default, lwP = default, rwP = default;
        bool ok = false;

        lock (_lock)
        {
            if (_hasResult && _result.poseLandmarks != null && _result.poseLandmarks.Count > 0)
            {
                var lm = _result.poseLandmarks[0].landmarks;
                if (lm != null
                    && TryGet(lm, 11, out lsP)
                    && TryGet(lm, 12, out rsP)
                    && TryGet(lm, 13, out leP)
                    && TryGet(lm, 14, out reP)
                    && TryGet(lm, 15, out lwP)
                    && TryGet(lm, 16, out rwP))
                {
                    ok = true;
                }
            }
        }

        if (!ok) return false;
        valid = true;

        // มุมศอก
        _rawLElbow = JointAngle(lsP, leP, lwP);
        _rawRElbow = JointAngle(rsP, reP, rwP);

        _fLElbow = Mathf.Lerp(_fLElbow, _rawLElbow, smoothing);
        _fRElbow = Mathf.Lerp(_fRElbow, _rawRElbow, smoothing);

        bool elbowAngleOK =
            (_fLElbow >= minElbowAngleDeg && _fLElbow <= maxElbowAngleDeg) &&
            (_fRElbow >= minElbowAngleDeg && _fRElbow <= maxElbowAngleDeg);

        if (!elbowAngleOK) return false;

        Vector2 ls = new Vector2(lsP.x, lsP.y);
        Vector2 rs = new Vector2(rsP.x, rsP.y);
        Vector2 le = new Vector2(leP.x, leP.y);
        Vector2 re = new Vector2(reP.x, reP.y);
        Vector2 lw = new Vector2(lwP.x, lwP.y);
        Vector2 rw = new Vector2(rwP.x, rwP.y);

        float shoulderWidth = Vector2.Distance(ls, rs);
        if (shoulderWidth < 1e-4f) return false;

        // ศอกชิดลำตัว: ระยะในแกน X จากไหล่ -> ศอก (normalize ด้วย shoulderWidth)
        _rawElbowOutL = Mathf.Abs(le.x - ls.x) / shoulderWidth;
        _rawElbowOutR = Mathf.Abs(re.x - rs.x) / shoulderWidth;

        bool elbowsCloseToBody = (_rawElbowOutL <= maxElbowOutRatio) && (_rawElbowOutR <= maxElbowOutRatio);
        if (!elbowsCloseToBody) return false;

        // ข้อมือออกด้านข้างจากศอก: |wrist.x - elbow.x| ต้องพอ (normalize)
        _rawWristOutL = Mathf.Abs(lw.x - le.x) / shoulderWidth;
        _rawWristOutR = Mathf.Abs(rw.x - re.x) / shoulderWidth;

        bool wristsOut = (_rawWristOutL >= minWristOutRatio) && (_rawWristOutR >= minWristOutRatio);
        if (!wristsOut) return false;

        // เสริม: forearm ควรใกล้แนวนอน (ไม่ชี้ขึ้น/ลงมาก)
        if (requireForearmNearHorizontal)
        {
            float leftForearmAngleFromHorizontal = AngleFromHorizontal(le, lw);
            float rightForearmAngleFromHorizontal = AngleFromHorizontal(re, rw);

            bool forearmOK =
                leftForearmAngleFromHorizontal <= maxForearmVerticalDeviationDeg &&
                rightForearmAngleFromHorizontal <= maxForearmVerticalDeviationDeg;

            if (!forearmOK) return false;
        }

        return true;
    }

    public override string GetDebugText()
    {
        return $"L elbow(L/R): {_fLElbow:F1}/{_fRElbow:F1} in [{minElbowAngleDeg:F0}-{maxElbowAngleDeg:F0}]"
             + $" | elbowOut(L/R): {_rawElbowOutL:F2}/{_rawElbowOutR:F2} <= {maxElbowOutRatio:F2}"
             + $" | wristOut(L/R): {_rawWristOutL:F2}/{_rawWristOutR:F2} >= {minWristOutRatio:F2}";
    }

    private static float JointAngle(NormalizedLandmark a, NormalizedLandmark b, NormalizedLandmark c)
    {
        Vector2 ba = new Vector2(a.x - b.x, a.y - b.y);
        Vector2 bc = new Vector2(c.x - b.x, c.y - b.y);
        if (ba.sqrMagnitude < 1e-6f || bc.sqrMagnitude < 1e-6f) return 0f;
        return Vector2.Angle(ba, bc);
    }

    private static float AngleFromHorizontal(Vector2 from, Vector2 to)
    {
        Vector2 v = (to - from);
        if (v.sqrMagnitude < 1e-6f) return 999f;
        // วัดมุมเทียบแนวนอน (Vector2.right)
        return Vector2.Angle(Vector2.right, v.normalized); // 0..180 (แนวนอน=0/180)
    }

    private static bool TryGet(System.Collections.Generic.IList<NormalizedLandmark> lm, int idx, out NormalizedLandmark p)
    {
        p = default;
        if (lm == null) return false;
        if (idx < 0 || idx >= lm.Count) return false;
        p = lm[idx];
        return true;
    }
}