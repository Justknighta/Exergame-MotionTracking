using UnityEngine;

using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class LevatorScapulaStretchRule : PoseRuleBase
{
    public enum TargetDir { DownLeft, DownRight, Either }

    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner;

    [Header("Target")]
    public TargetDir target = TargetDir.Either;

    [Tooltip("ถ้ากล้องเป็น mirror แล้วซ้าย/ขวาสลับ ให้ติ๊กอันนี้")]
    public bool mirrorX = false;

    [Header("Thresholds (ปรับให้ง่าย/ยาก)")]
    [Tooltip("ต้องก้มลงขั้นต่ำกี่องศา (ค่ามาก = ต้องก้มมากขึ้น/ยากขึ้น)")]
    public float minDownPitchDeg = 25f;   // แนะนำ 20-35

    [Tooltip("ต้องหัน/เฉียงซ้าย-ขวาขั้นต่ำเป็นสัดส่วนของความกว้างไหล่ (ค่ามาก = ยากขึ้น)")]
    public float minYawRatio = 0.10f;     // แนะนำ 0.08-0.15

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.40f;

    public override string PoseName => "Levator Scapulae Stretch";
    public override float DurationSec => 30f;
    public override int PassBonusScore => 100;

    private PoseLandmarkerResult _result;
    private bool _hasResult;
    private readonly object _lock = new object();

    private float _rawPitch, _fPitch;
    private float _rawYaw, _fYaw; // yaw เป็น ratio (ติดลบ=ซ้าย, บวก=ขวา)

    public override void OnSessionStart()
    {
        _rawPitch = _fPitch = 0f;
        _rawYaw = _fYaw = 0f;
    }

    private void Awake()
    {
        if (runner == null) runner = GetComponent<PoseLandmarkerRunner>();
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();

        if (runner == null)
        {
            Debug.LogError("[LevatorScapulaStretchRule] ไม่พบ PoseLandmarkerRunner (ลากใส่ช่อง runner ใน Inspector)");
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

        NormalizedLandmark noseP = default, lsP = default, rsP = default;
        bool ok = false;

        lock (_lock)
        {
            if (_hasResult && _result.poseLandmarks != null && _result.poseLandmarks.Count > 0)
            {
                var lm = _result.poseLandmarks[0].landmarks;
                if (lm != null
                    && TryGet(lm, 0, out noseP)   // Nose
                    && TryGet(lm, 11, out lsP)   // Left shoulder
                    && TryGet(lm, 12, out rsP))  // Right shoulder
                {
                    ok = true;
                }
            }
        }

        if (!ok) return false;
        valid = true;

        Vector2 nose = new Vector2(noseP.x, noseP.y);
        Vector2 ls = new Vector2(lsP.x, lsP.y);
        Vector2 rs = new Vector2(rsP.x, rsP.y);

        // กึ่งกลางไหล่
        Vector2 mid = (ls + rs) * 0.5f;

        // ความกว้างไหล่ (กันหาร 0)
        float shoulderWidth = Mathf.Max(1e-4f, Mathf.Abs(rs.x - ls.x));

        // 1) Yaw ratio: ตำแหน่ง nose เบี่ยงจากกึ่งกลางกี่ส่วนของความกว้างไหล่
        float yaw = (nose.x - mid.x) / shoulderWidth;
        if (mirrorX) yaw = -yaw;

        // 2) Pitch: ใช้เวกเตอร์ midShoulder -> nose เทียบกับ "แนวขึ้น" (0,-1)
        // ถ้าก้มลงมากขึ้น nose จะต่ำลง => มุมจากแนวขึ้นจะมากขึ้น
        Vector2 v = nose - mid;
        float pitchDeg = Vector2.Angle(new Vector2(0f, -1f), v.normalized);

        _rawYaw = yaw;
        _rawPitch = pitchDeg;

        _fYaw = Mathf.Lerp(_fYaw, _rawYaw, smoothing);
        _fPitch = Mathf.Lerp(_fPitch, _rawPitch, smoothing);

        bool pitchOK = _fPitch >= minDownPitchDeg;
        bool yawMagOK = Mathf.Abs(_fYaw) >= minYawRatio;

        bool dirOK = true;
        switch (target)
        {
            case TargetDir.DownLeft:
                dirOK = _fYaw <= -minYawRatio;
                break;
            case TargetDir.DownRight:
                dirOK = _fYaw >= +minYawRatio;
                break;
            case TargetDir.Either:
                dirOK = yawMagOK;
                break;
        }

        return pitchOK && yawMagOK && dirOK;
    }

    public override string GetDebugText()
    {
        return $"Levator pitchDeg: {_fPitch:F1} >= {minDownPitchDeg:F0} | yawRatio: {_fYaw:F2} (|.|>={minYawRatio:F2}) | target={target}";
    }

    private static bool TryGet(System.Collections.Generic.IList<NormalizedLandmark> lm, int idx, out NormalizedLandmark p)
    {
        p = default;
        if (lm == null || idx < 0 || idx >= lm.Count) return false;
        p = lm[idx];
        return true;
    }
}