using UnityEngine;

using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class YPoseRule : PoseRuleBase
{
    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Easy Y Settings")]
    [Tooltip("ใช้ศอกแทนข้อมือ (แนะนำ: true เพราะนิ่งกว่า)")]
    public bool useElbowInsteadOfWrist = true;

    [Tooltip("มุมจากแนวตั้งขึ้น (องศา). ค่ายิ่งมาก = แขนเอียงออกด้านข้างมากขึ้น (ง่ายขึ้น)")]
    public float targetFromUpDeg = 60f;

    [Tooltip("ความยืดหยุ่นของมุม (ยิ่งมากยิ่งง่าย)")]
    public float toleranceDeg = 30f;

    [Header("Optional: Elbow Straight Check (ปิดได้)")]
    public bool requireElbowAlmostStraight = false;

    [Tooltip("ถ้าเปิด requireElbowAlmostStraight: ศอกควรเหยียดอย่างน้อยกี่องศา (180 = เหยียดสุด)")]
    public float minElbowAngleDeg = 140f;

    [Header("Optional: Elbow Above Shoulder (ช่วยกันมั่วท่า)")]
    public bool requireElbowAboveShoulder = true;

    [Tooltip("ยอมให้ศอกต่ำกว่าไหล่ได้เล็กน้อย (ค่ามาก = ง่ายขึ้น)")]
    public float elbowAboveShoulderMargin = 0.03f;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.40f;

    public override string PoseName => "YTWL - Y (Easy)";
    public override float DurationSec => 20f;
    public override int PassBonusScore => 100;

    private PoseLandmarkerResult _result;
    private bool _hasResult;
    private readonly object _resultLock = new object();

    private float _fLeft, _fRight;
    private float _rawLeft, _rawRight;

    public override void OnSessionStart()
    {
        _fLeft = _fRight = 0f;
        _rawLeft = _rawRight = 0f;
    }

    private void Awake()
    {
        if (runner == null) runner = GetComponent<PoseLandmarkerRunner>();
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();

        if (runner == null)
        {
            Debug.LogError("[YPoseRule] ไม่พบ PoseLandmarkerRunner (ลากใส่ช่อง runner ใน Inspector)");
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

        // landmarks
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

        // เลือกจุดปลายแขน: ศอก (ง่าย/นิ่ง) หรือ ข้อมือ (ยากกว่า)
        Vector3 leftEnd  = useElbowInsteadOfWrist ? le : lw;
        Vector3 rightEnd = useElbowInsteadOfWrist ? re : rw;

        // แนว "ขึ้น" ในภาพ mediapipe: y น้อย = สูงขึ้น
        Vector2 upDir = new Vector2(0f, -1f);

        _rawLeft  = AngleFromUp(ls, leftEnd, upDir);
        _rawRight = AngleFromUp(rs, rightEnd, upDir);

        _fLeft  = Mathf.Lerp(_fLeft,  _rawLeft,  smoothing);
        _fRight = Mathf.Lerp(_fRight, _rawRight, smoothing);

        bool leftAngleOK  = Mathf.Abs(_fLeft  - targetFromUpDeg) <= toleranceDeg;
        bool rightAngleOK = Mathf.Abs(_fRight - targetFromUpDeg) <= toleranceDeg;

        bool elbowAboveOK = true;
        if (requireElbowAboveShoulder)
        {
            // elbow y ต้อง "น้อยกว่า" shoulder y (สูงกว่า) โดยเผื่อ margin ได้
            elbowAboveOK =
                (le.y <= ls.y + elbowAboveShoulderMargin) &&
                (re.y <= rs.y + elbowAboveShoulderMargin);
        }

        bool elbowStraightOK = true;
        if (requireElbowAlmostStraight)
        {
            float leftElbowAngle  = JointAngle(lsP, leP, lwP); // shoulder-elbow-wrist
            float rightElbowAngle = JointAngle(rsP, reP, rwP);

            elbowStraightOK = (leftElbowAngle >= minElbowAngleDeg) && (rightElbowAngle >= minElbowAngleDeg);
        }

        return leftAngleOK && rightAngleOK && elbowAboveOK && elbowStraightOK;
    }

    public override string GetDebugText()
    {
        string end = useElbowInsteadOfWrist ? "ELBOW" : "WRIST";
        return $"Y({end}) raw(L/R): {_rawLeft:F1}/{_rawRight:F1} | filt(L/R): {_fLeft:F1}/{_fRight:F1} | target={targetFromUpDeg:F0} tol=±{toleranceDeg:F0}";
    }

    private static float AngleFromUp(Vector3 shoulder, Vector3 endPoint, Vector2 upDir)
    {
        Vector2 v = new Vector2(endPoint.x - shoulder.x, endPoint.y - shoulder.y);
        if (v.sqrMagnitude < 1e-6f) return 999f;
        return Vector2.Angle(upDir, v.normalized);
    }

    // มุมที่ศอก (shoulder-elbow-wrist)
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