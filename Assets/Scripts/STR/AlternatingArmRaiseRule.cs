using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class AlternatingArmDirectionRule : PoseRuleBase
{
    public enum Direction
    {
        RightUp,  // ยกขวา + ซ้ายลง
        LeftUp,   // ยกซ้าย + ขวาลง
        Either    // ผ่านได้ทั้งสองแบบ (ไว้เทส)
    }

    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Direction")]
    public Direction direction = Direction.RightUp;

    [Header("Up/Down Thresholds (Wrist vs Shoulder)")]
    [Tooltip("ถือว่า 'ยกแขน' เมื่อ wrist สูงกว่า shoulder (y น้อยกว่า) อย่างน้อยเท่านี้")]
    public float upMargin = 0.05f;

    [Tooltip("ถือว่า 'ปล่อยลง' เมื่อ wrist ต่ำกว่า shoulder (y มากกว่า) อย่างน้อยเท่านี้")]
    public float downMargin = 0.03f;

    [Header("Optional: Require opposite arm down")]
    [Tooltip("ถ้าเปิด = ต้องให้แขนอีกข้าง 'ลง' จริง ไม่ใช่แค่ไม่ขึ้น")]
    public bool requireOppositeDown = true;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.40f;

    public override string PoseName => $"Arm Raise - {direction}";
    public override float DurationSec => 20f;
    public override int PassBonusScore => 100;

    private PoseLandmarkerResult _result;
    private bool _hasResult;
    private readonly object _lock = new object();

    // filtered: (shoulderY - wristY) > 0 แปลว่า wrist สูงกว่า shoulder
    private float _fLeftUp, _fRightUp;
    private float _rawLeftUp, _rawRightUp;

    public override void OnSessionStart()
    {
        _fLeftUp = _fRightUp = 0f;
        _rawLeftUp = _rawRightUp = 0f;
    }

    private void Awake()
    {
        if (runner == null) runner = GetComponent<PoseLandmarkerRunner>();
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();

        if (runner == null)
        {
            Debug.LogError("[AlternatingArmDirectionRule] ไม่พบ PoseLandmarkerRunner (ลากใส่ช่อง runner ใน Inspector)");
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

        NormalizedLandmark ls = default, rs = default, lw = default, rw = default;
        bool ok = false;

        lock (_lock)
        {
            if (_hasResult && _result.poseLandmarks != null && _result.poseLandmarks.Count > 0)
            {
                var lm = _result.poseLandmarks[0].landmarks;
                if (lm != null
                    && TryGet(lm, 11, out ls) // left shoulder
                    && TryGet(lm, 12, out rs) // right shoulder
                    && TryGet(lm, 15, out lw) // left wrist
                    && TryGet(lm, 16, out rw)) // right wrist
                {
                    ok = true;
                }
            }
        }

        if (!ok) return false;
        valid = true;

        // Mediapipe: y น้อย = สูง
        _rawLeftUp = ls.y - lw.y;   // >0 = left wrist สูงกว่า left shoulder
        _rawRightUp = rs.y - rw.y;  // >0 = right wrist สูงกว่า right shoulder

        _fLeftUp = Mathf.Lerp(_fLeftUp, _rawLeftUp, smoothing);
        _fRightUp = Mathf.Lerp(_fRightUp, _rawRightUp, smoothing);

        bool leftUp = _fLeftUp >= upMargin;
        bool rightUp = _fRightUp >= upMargin;

        bool leftDown = _fLeftUp <= -downMargin;
        bool rightDown = _fRightUp <= -downMargin;

        // ถ้าไม่บังคับ oppositeDown: ให้ถือว่า "ลง" เมื่อไม่ได้ยกขึ้น
        if (!requireOppositeDown)
        {
            leftDown = !leftUp;
            rightDown = !rightUp;
        }

        bool rightUpLeftDown = rightUp && leftDown;
        bool leftUpRightDown = leftUp && rightDown;

        return direction switch
        {
            Direction.RightUp => rightUpLeftDown,
            Direction.LeftUp => leftUpRightDown,
            _ => (rightUpLeftDown || leftUpRightDown),
        };
    }

    public override string GetDebugText()
    {
        return $"Dir:{direction} | LUpVal:{_fLeftUp:F2} RUpVal:{_fRightUp:F2} (up>{upMargin:F2}, down<-{downMargin:F2})";
    }

    private static bool TryGet(System.Collections.Generic.IList<NormalizedLandmark> lm, int i, out NormalizedLandmark p)
    {
        p = default;
        if (lm == null || i < 0 || i >= lm.Count) return false;
        p = lm[i];
        return true;
    }
}