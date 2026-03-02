using UnityEngine;

using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class ShoulderStretchRule : PoseRuleBase
{
    public enum Side { LeftArm, RightArm, Either }   // เพิ่ม Either ให้ทนสลับ

    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Target")]
    public Side targetSide = Side.Either;

    [Tooltip("ถ้าภาพเป็น mirror ให้ติ๊ก (แนะนำลองติ๊กถ้าซ้ายขวาสลับ)")]
    public bool mirrorX = false;

    [Header("Thresholds")]
    [Tooltip("ศอกต้องอยู่ใกล้ระดับไหล่ (normalized y tolerance)")]
    public float elbowHeightTolerance = 0.10f;

    [Tooltip("แขนต้องพาดข้ามลำตัวขั้นต่ำ (ratio ของ shoulder width)")]
    public float minCrossRatio = 0.18f;

    [Tooltip("มืออีกข้างต้องอยู่ใกล้ข้อศอก (normalized dist)")]
    public float maxHandToElbowDist = 0.14f;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.40f;

    public override string PoseName => "Shoulder Stretch (Robust)";
    public override float DurationSec => 15f;
    public override int PassBonusScore => 100;

    private PoseLandmarkerResult _result;
    private bool _hasResult;
    private readonly object _lock = new object();

    // debug
    private float _bestScore;     // ยิ่งมากยิ่งดี
    private string _bestWhich;    // "L" หรือ "R"
    private float _bestCross;
    private float _bestHandDist;

    private void Awake()
    {
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();
        if (runner == null)
        {
            Debug.LogError("[ShoulderStretchRule] PoseLandmarkerRunner not found");
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

        Vector2 vLS = new Vector2(ls.x, ls.y);
        Vector2 vRS = new Vector2(rs.x, rs.y);
        float shoulderWidth = Mathf.Max(1e-4f, Mathf.Abs(vRS.x - vLS.x));

        // ประเมิน 2 เคส แล้วเลือกเคสที่ดีที่สุด
        float scoreL = Score_LeftArmPulled(ls, rs, le, re, lw, rw, shoulderWidth, out float crossL, out float handDistL);
        float scoreR = Score_RightArmPulled(ls, rs, le, re, lw, rw, shoulderWidth, out float crossR, out float handDistR);

        // บังคับตาม targetSide ถ้าต้องการ
        if (targetSide == Side.LeftArm) scoreR = -999f;
        if (targetSide == Side.RightArm) scoreL = -999f;

        if (scoreL >= scoreR)
        {
            _bestScore = scoreL; _bestWhich = "L";
            _bestCross = crossL; _bestHandDist = handDistL;
        }
        else
        {
            _bestScore = scoreR; _bestWhich = "R";
            _bestCross = crossR; _bestHandDist = handDistR;
        }

        // ผ่านเมื่อ score > 0 (แปลว่าผ่านทุกเงื่อนไขหลัก)
        return _bestScore > 0f;
    }

    // เคส: ดึงแขนซ้าย (Right wrist ใกล้ Left elbow) + ศอกซ้ายข้ามลำตัว
    private float Score_LeftArmPulled(
        NormalizedLandmark ls, NormalizedLandmark rs,
        NormalizedLandmark le, NormalizedLandmark re,
        NormalizedLandmark lw, NormalizedLandmark rw,
        float shoulderWidth,
        out float cross, out float handDist)
    {
        cross = 0f; handDist = 999f;

        // 1) ศอกซ้ายระดับไหล่
        bool elbowHeightOK = Mathf.Abs(le.y - ls.y) <= elbowHeightTolerance;
        if (!elbowHeightOK) return -1f;

        // 2) ศอกซ้ายข้ามลำตัวไปฝั่งขวา
        cross = (le.x - ls.x) / shoulderWidth;
        if (mirrorX) cross = -cross;
        bool crossOK = cross >= minCrossRatio;
        if (!crossOK) return -1f;

        // 3) มือขวาใกล้ศอกซ้าย (จริง ๆ คือ rw ใกล้ le)
        handDist = Vector2.Distance(new Vector2(rw.x, rw.y), new Vector2(le.x, le.y));
        bool handOK = handDist <= maxHandToElbowDist;
        if (!handOK) return -1f;

        // score ให้สูงขึ้นเมื่อทำได้ “ดี”
        // ใกล้ศอกมากขึ้น = score มากขึ้น
        float handScore = 1f - Mathf.InverseLerp(maxHandToElbowDist, 0f, handDist);
        float crossScore = Mathf.InverseLerp(minCrossRatio, minCrossRatio * 2f, cross);
        return 0.1f + handScore + 0.3f * crossScore;
    }

    // เคส: ดึงแขนขวา (Left wrist ใกล้ Right elbow) + ศอกขวาข้ามลำตัว
    private float Score_RightArmPulled(
        NormalizedLandmark ls, NormalizedLandmark rs,
        NormalizedLandmark le, NormalizedLandmark re,
        NormalizedLandmark lw, NormalizedLandmark rw,
        float shoulderWidth,
        out float cross, out float handDist)
    {
        cross = 0f; handDist = 999f;

        bool elbowHeightOK = Mathf.Abs(re.y - rs.y) <= elbowHeightTolerance;
        if (!elbowHeightOK) return -1f;

        // ขวาข้ามไปซ้าย
        cross = (rs.x - re.x) / shoulderWidth;
        if (mirrorX) cross = -cross;
        bool crossOK = cross >= minCrossRatio;
        if (!crossOK) return -1f;

        // มือซ้ายใกล้ศอกขวา (lw ใกล้ re)
        handDist = Vector2.Distance(new Vector2(lw.x, lw.y), new Vector2(re.x, re.y));
        bool handOK = handDist <= maxHandToElbowDist;
        if (!handOK) return -1f;

        float handScore = 1f - Mathf.InverseLerp(maxHandToElbowDist, 0f, handDist);
        float crossScore = Mathf.InverseLerp(minCrossRatio, minCrossRatio * 2f, cross);
        return 0.1f + handScore + 0.3f * crossScore;
    }

    public override string GetDebugText()
    {
        return $"ShoulderStretch best={_bestWhich} score={_bestScore:F2} | cross={_bestCross:F2} (>= {minCrossRatio:F2}) | handDist={_bestHandDist:F2} (<= {maxHandToElbowDist:F2})";
    }
}