using UnityEngine;

using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class WPoseRule : PoseRuleBase
{
    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("W Pose - Elbow Angle")]
    [Tooltip("ใช้มุมด้านกว้าง (แก้กรณีค่าออกมา 20-40 แล้วจริงๆคือ 140-160)")]
    public bool useObtuseElbowAngle = true;

    [Tooltip("ช่วงมุมศอกที่ยอมรับได้ (หลังแปลง). แนะนำเริ่ม 120-170")]
    public float minElbowAngleDeg = 120f;
    public float maxElbowAngleDeg = 170f;

    [Header("Optional: Forearm Up (ข้อมือสูงกว่าศอก)")]
    public bool requireForearmUp = true;

    [Tooltip("ค่ามาก = ง่ายขึ้น (ยอมให้ข้อมือไม่สูงกว่าศอกมากก็ได้)")]
    public float wristAboveElbowMargin = 0.04f;

    [Header("Optional: Elbow Below Shoulder (กันกลายเป็น Y)")]
    public bool requireElbowBelowShoulder = true;

    [Tooltip("เริ่มต้นให้ 0.00 จะง่ายสุด / เพิ่มจะเข้มขึ้น")]
    public float elbowBelowShoulderMargin = 0.00f;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.40f;

    public override string PoseName => "YTWL - W (Easy)";
    public override float DurationSec => 20f;
    public override int PassBonusScore => 100;

    private PoseLandmarkerResult _result;
    private bool _hasResult;
    private readonly object _lock = new object();

    private float _rawLeftElbow, _rawRightElbow;
    private float _fLeftElbow, _fRightElbow;

    public override void OnSessionStart()
    {
        _rawLeftElbow = _rawRightElbow = 0f;
        _fLeftElbow = _fRightElbow = 0f;
    }

    private void Awake()
    {
        if (runner == null) runner = GetComponent<PoseLandmarkerRunner>();
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();

        if (runner == null)
        {
            Debug.LogError("[WPoseRule] ไม่พบ PoseLandmarkerRunner (ลากใส่ช่อง runner ใน Inspector)");
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

        // 1) คำนวณมุมศอก (shoulder-elbow-wrist)
        float leftAngle = JointAngle(lsP, leP, lwP);
        float rightAngle = JointAngle(rsP, reP, rwP);

        // 2) แปลงเป็น "มุมด้านกว้าง" (แก้ปัญหา 26->154)
        if (useObtuseElbowAngle)
        {
            leftAngle = Mathf.Max(leftAngle, 180f - leftAngle);
            rightAngle = Mathf.Max(rightAngle, 180f - rightAngle);
        }

        _rawLeftElbow = leftAngle;
        _rawRightElbow = rightAngle;

        _fLeftElbow = Mathf.Lerp(_fLeftElbow, _rawLeftElbow, smoothing);
        _fRightElbow = Mathf.Lerp(_fRightElbow, _rawRightElbow, smoothing);

        bool elbowAngleOK =
            (_fLeftElbow >= minElbowAngleDeg && _fLeftElbow <= maxElbowAngleDeg) &&
            (_fRightElbow >= minElbowAngleDeg && _fRightElbow <= maxElbowAngleDeg);

        if (!elbowAngleOK) return false;

        Vector3 ls = ToVec(lsP);
        Vector3 rs = ToVec(rsP);
        Vector3 le = ToVec(leP);
        Vector3 re = ToVec(reP);
        Vector3 lw = ToVec(lwP);
        Vector3 rw = ToVec(rwP);

        // 3) Forearm up: ข้อมือควรสูงกว่าศอก (mediapipe: y น้อย = สูง)
        bool forearmUpOK = true;
        if (requireForearmUp)
        {
            forearmUpOK =
                (lw.y <= le.y + wristAboveElbowMargin) &&
                (rw.y <= re.y + wristAboveElbowMargin);
        }

        // 4) Elbow below shoulder: ศอกควรต่ำกว่าไหล่เล็กน้อย (y มากกว่า)
        bool elbowBelowOK = true;
        if (requireElbowBelowShoulder)
        {
            elbowBelowOK =
                (le.y >= ls.y + elbowBelowShoulderMargin) &&
                (re.y >= rs.y + elbowBelowShoulderMargin);
        }

        return forearmUpOK && elbowBelowOK;
    }

    public override string GetDebugText()
    {
        return $"W elbow(L/R): {_fLeftElbow:F1}/{_fRightElbow:F1} in [{minElbowAngleDeg:F0}-{maxElbowAngleDeg:F0}] (obtuse={useObtuseElbowAngle})";
    }

    private static float JointAngle(NormalizedLandmark a, NormalizedLandmark b, NormalizedLandmark c)
    {
        Vector2 ba = new Vector2(a.x - b.x, a.y - b.y);
        Vector2 bc = new Vector2(c.x - b.x, c.y - b.y);
        if (ba.sqrMagnitude < 1e-6f || bc.sqrMagnitude < 1e-6f) return 0f;
        return Vector2.Angle(ba, bc); // 0..180
    }

    private static bool TryGet(System.Collections.Generic.IList<NormalizedLandmark> lm, int idx, out NormalizedLandmark p)
    {
        p = default;
        if (lm == null) return false;
        if (idx < 0 || idx >= lm.Count) return false;
        p = lm[idx];
        return true;
    }

    private static Vector3 ToVec(NormalizedLandmark p) => new Vector3(p.x, p.y, p.z);
}