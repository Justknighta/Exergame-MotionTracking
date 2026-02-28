using UnityEngine;

using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class TPoseRule : PoseRuleBase
{
    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Easy T Settings")]
    [Tooltip("ใช้ศอกแทนข้อมือ (แนะนำ: true เพราะนิ่งกว่า)")]
    public bool useElbowInsteadOfWrist = true;

    [Tooltip("ความยืดหยุ่นของมุมจากแนวนอน (ยิ่งมากยิ่งง่าย)")]
    public float toleranceDeg = 25f;   // แนะนำ 25-35

    [Header("Optional: Elbow Angle (ปิดได้)")]
    public bool requireElbowAlmostStraight = false;
    public float minElbowAngleDeg = 140f;

    [Header("Optional: Scapula Squeeze (บีบสะบัก)")]
    public bool requireScapulaSqueeze = false;

    [Tooltip("ยิ่งมากยิ่งง่าย (ระยะไหล่ซ้าย-ขวา ต้อง 'สั้นลง' ถึงจะถือว่าบีบสะบัก)")]
    public float scapulaSqueezeRatio = 0.92f;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.40f;

    public override string PoseName => "YTWL - T (Easy)";
    public override float DurationSec => 20f;
    public override int PassBonusScore => 100;

    private PoseLandmarkerResult _result;
    private bool _hasResult;
    private readonly object _resultLock = new object();

    private float _rawLeft, _rawRight;
    private float _fLeft, _fRight;

    // เพิ่ม: deviation (0 = แนวนอนพอดี)
    private float _devLeft, _devRight;

    private float _shoulderDistBaseline = -1f;
    private float _rawShoulderDist;

    public override void OnSessionStart()
    {
        _rawLeft = _rawRight = 0f;
        _fLeft = _fRight = 0f;
        _devLeft = _devRight = 999f;

        _shoulderDistBaseline = -1f;
        _rawShoulderDist = 0f;
    }

    private void Awake()
    {
        if (runner == null) runner = GetComponent<PoseLandmarkerRunner>();
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();

        if (runner == null)
        {
            Debug.LogError("[TPoseRule] ไม่พบ PoseLandmarkerRunner (ลากใส่ช่อง runner ใน Inspector)");
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

        NormalizedLandmark lsP = default, rsP = default, leP = default, reP = default, lwP = default, rwP = default;
        bool ok = false;

        lock (_resultLock)
        {
            if (_hasResult && _result.poseLandmarks != null && _result.poseLandmarks.Count > 0)
            {
                var lm = _result.poseLandmarks[0].landmarks;
                if (lm != null
                    && TryGetLm(lm, 11, out lsP)
                    && TryGetLm(lm, 12, out rsP)
                    && TryGetLm(lm, 13, out leP)
                    && TryGetLm(lm, 14, out reP)
                    && TryGetLm(lm, 15, out lwP)
                    && TryGetLm(lm, 16, out rwP))
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
        Vector3 lw = ToVec(lwP);
        Vector3 rw = ToVec(rwP);

        Vector3 leftEnd = useElbowInsteadOfWrist ? le : lw;
        Vector3 rightEnd = useElbowInsteadOfWrist ? re : rw;

        // เราจะวัด "ความเบี่ยงจากแนวนอน" โดยยอมรับทั้ง 0° และ 180°
        // เพื่อกันปัญหากล้อง mirror / เวกเตอร์กลับทิศ
        _rawLeft = AngleFromHorizontal(ls, leftEnd);
        _rawRight = AngleFromHorizontal(rs, rightEnd);

        _fLeft = Mathf.Lerp(_fLeft, _rawLeft, smoothing);
        _fRight = Mathf.Lerp(_fRight, _rawRight, smoothing);

        _devLeft = HorizontalDeviation(_fLeft);
        _devRight = HorizontalDeviation(_fRight);

        bool leftOK = _devLeft <= toleranceDeg;
        bool rightOK = _devRight <= toleranceDeg;

        bool elbowStraightOK = true;
        if (requireElbowAlmostStraight)
        {
            float leftElbowAngle = JointAngle(lsP, leP, lwP);
            float rightElbowAngle = JointAngle(rsP, reP, rwP);
            elbowStraightOK = (leftElbowAngle >= minElbowAngleDeg) && (rightElbowAngle >= minElbowAngleDeg);
        }

        bool scapulaOK = true;
        if (requireScapulaSqueeze)
        {
            _rawShoulderDist = Vector2.Distance(new Vector2(ls.x, ls.y), new Vector2(rs.x, rs.y));
            if (_shoulderDistBaseline < 0f) _shoulderDistBaseline = _rawShoulderDist;

            float threshold = _shoulderDistBaseline * scapulaSqueezeRatio;
            scapulaOK = _rawShoulderDist <= threshold;
        }

        return leftOK && rightOK && elbowStraightOK && scapulaOK;
    }

    public override string GetDebugText()
    {
        string end = useElbowInsteadOfWrist ? "ELBOW" : "WRIST";
        string sca = requireScapulaSqueeze
            ? $" | shoulderDist={_rawShoulderDist:F3} base={_shoulderDistBaseline:F3} ratio={scapulaSqueezeRatio:F2}"
            : "";

        return $"T({end}) angle(L/R): {_fLeft:F1}/{_fRight:F1} | dev(L/R): {_devLeft:F1}/{_devRight:F1} <= {toleranceDeg:F0}{sca}";
    }

    // ✅ มุมระหว่างแขน (shoulder -> endPoint) กับ "แนวนอน" โดยไม่สนทิศซ้าย/ขวา
    // 0 = แนวนอนพอดี, 90 = ชี้ขึ้น/ลง
    private static float AngleFromHorizontal(Vector3 shoulder, Vector3 endPoint)
    {
        Vector2 v = new Vector2(endPoint.x - shoulder.x, endPoint.y - shoulder.y);
        if (v.sqrMagnitude < 1e-6f) return 999f;

        // วัดกับแกนแนวนอน (1,0) ได้เลย
        return Vector2.Angle(Vector2.right, v.normalized);
    }

    // ✅ แปลง angle ให้เป็น "เบี่ยงจากแนวนอน" แบบยอมรับทั้ง 0 และ 180
    private static float HorizontalDeviation(float angleDeg)
    {
        // angle ที่มาจาก Vector2.Angle จะอยู่ 0..180 อยู่แล้ว
        return Mathf.Min(angleDeg, 180f - angleDeg);
    }

    private static float JointAngle(NormalizedLandmark a, NormalizedLandmark b, NormalizedLandmark c)
    {
        Vector2 ba = new Vector2(a.x - b.x, a.y - b.y);
        Vector2 bc = new Vector2(c.x - b.x, c.y - b.y);
        if (ba.sqrMagnitude < 1e-6f || bc.sqrMagnitude < 1e-6f) return 0f;
        return Vector2.Angle(ba, bc);
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