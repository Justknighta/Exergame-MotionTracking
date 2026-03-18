using UnityEngine;

using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class StandingLateralStretchRule : PoseRuleBase
{
    public enum LeanDir { Left, Right, Either }

    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Target")]
    public LeanDir target = LeanDir.Either;

    [Tooltip("ถ้ากล้องเป็น mirror แล้วซ้าย/ขวาสลับ ให้ติ๊กอันนี้")]
    public bool mirrorX = false;

    [Header("Arms Up")]
    [Tooltip("ข้อมือควรสูงกว่าไหล่เท่าไหร่ (หน่วย normalized y; ค่าน้อย=ง่ายขึ้น)")]
    public float wristAboveShoulderMargin = 0.05f;

    [Header("Torso Lean")]
    [Tooltip("องศาเอียงลำตัวขั้นต่ำ (ค่ามาก=ยากขึ้น)")]
    public float minLeanAngleDeg = 12f;  // แนะนำ 10-18

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.40f;

    public override string PoseName => "Standing Lateral Stretch";
    public override float DurationSec => 30f;
    public override int PassBonusScore => 100;

    private PoseLandmarkerResult _result;
    private bool _hasResult;
    private readonly object _lock = new object();

    private float _rawLeanDeg, _fLeanDeg;
    private float _rawLeanSign, _fLeanSign; // ซ้าย/ขวา (ติดลบ = ซ้าย, บวก = ขวา)

    public override void OnSessionStart()
    {
        _rawLeanDeg = _fLeanDeg = 0f;
        _rawLeanSign = _fLeanSign = 0f;
    }

    private void Awake()
    {
        if (runner == null) runner = GetComponent<PoseLandmarkerRunner>();
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();

        if (runner == null)
        {
            Debug.LogError("[StandingLateralStretchRule] ไม่พบ PoseLandmarkerRunner (ลากใส่ช่อง runner ใน Inspector)");
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

        NormalizedLandmark lsP = default, rsP = default, lhP = default, rhP = default, lwP = default, rwP = default;
        bool ok = false;

        lock (_lock)
        {
            if (_hasResult && _result.poseLandmarks != null && _result.poseLandmarks.Count > 0)
            {
                var lm = _result.poseLandmarks[0].landmarks;
                if (lm != null
                    && TryGet(lm, 11, out lsP) // L shoulder
                    && TryGet(lm, 12, out rsP) // R shoulder
                    && TryGet(lm, 23, out lhP) // L hip
                    && TryGet(lm, 24, out rhP) // R hip
                    && TryGet(lm, 15, out lwP) // L wrist
                    && TryGet(lm, 16, out rwP)) // R wrist
                {
                    ok = true;
                }
            }
        }

        if (!ok) return false;
        valid = true;

        Vector2 ls = new Vector2(lsP.x, lsP.y);
        Vector2 rs = new Vector2(rsP.x, rsP.y);
        Vector2 lh = new Vector2(lhP.x, lhP.y);
        Vector2 rh = new Vector2(rhP.x, rhP.y);
        Vector2 lw = new Vector2(lwP.x, lwP.y);
        Vector2 rw = new Vector2(rwP.x, rwP.y);

        // 1) Arms up: y น้อย = สูงขึ้น
        float shoulderY = (ls.y + rs.y) * 0.5f;
        bool armsUp =
            (lw.y <= shoulderY - wristAboveShoulderMargin) &&
            (rw.y <= shoulderY - wristAboveShoulderMargin);

        if (!armsUp) return false;

        // 2) Torso vector: hipMid -> shoulderMid
        Vector2 shoulderMid = (ls + rs) * 0.5f;
        Vector2 hipMid = (lh + rh) * 0.5f;

        Vector2 torso = shoulderMid - hipMid;
        if (torso.sqrMagnitude < 1e-6f) return false;

        // มุมเอียงจากแนวดิ่ง (0,-1 คือ "ขึ้น" ในภาพ mediapipe)
        float leanDeg = Vector2.Angle(new Vector2(0f, -1f), torso.normalized);

        // สัญญาณซ้าย/ขวา: torso.x (บวก = เอียงขวา, ลบ = เอียงซ้าย)
        float sign = torso.x;
        if (mirrorX) sign = -sign;

        _rawLeanDeg = leanDeg;
        _rawLeanSign = sign;

        _fLeanDeg = Mathf.Lerp(_fLeanDeg, _rawLeanDeg, smoothing);
        _fLeanSign = Mathf.Lerp(_fLeanSign, _rawLeanSign, smoothing);

        bool angleOK = _fLeanDeg >= minLeanAngleDeg;

        bool dirOK = true;
        switch (target)
        {
            case LeanDir.Left:
                dirOK = _fLeanSign < 0f;
                break;
            case LeanDir.Right:
                dirOK = _fLeanSign > 0f;
                break;
            case LeanDir.Either:
                dirOK = Mathf.Abs(_fLeanSign) > 0.0001f;
                break;
        }

        return angleOK && dirOK;
    }

    public override string GetDebugText()
    {
        string d = target.ToString();
        return $"Lateral armsUp OK | leanDeg={_fLeanDeg:F1} >= {minLeanAngleDeg:F0} | sign={( _fLeanSign>=0 ? "+" : "-")} | target={d}";
    }

    private static bool TryGet(System.Collections.Generic.IList<NormalizedLandmark> lm, int idx, out NormalizedLandmark p)
    {
        p = default;
        if (lm == null || idx < 0 || idx >= lm.Count) return false;
        p = lm[idx];
        return true;
    }
}