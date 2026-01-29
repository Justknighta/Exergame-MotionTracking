using UnityEngine;
using System;

using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;

// << สำคัญ: runner ของคุณอยู่ใน namespace นี้ >>
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class SideNeckStretchDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PoseLandmarkerRunner runner; // ลาก Solution (ที่มี runner) มาวาง หรือปล่อยว่างให้ auto หา

    [Header("Thresholds (deg)")]
    public float enterAngleDeg = 18f;
    public float exitAngleDeg  = 12f;
    public float holdSeconds   = 0.6f;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.15f;

    private PoseLandmarkerResult _result;
    private bool _hasResult;

    private float _filteredAngle;
    private float _holdTimer;
    private bool _inPose;
    private bool _firedOnce;

    void Awake()
    {
        if (runner == null) runner = GetComponent<PoseLandmarkerRunner>();
        if (runner == null) runner = FindObjectOfType<PoseLandmarkerRunner>();

        if (runner == null)
        {
            Debug.LogError("[SideNeckStretchDetector] ไม่พบ PoseLandmarkerRunner ในซีน (ลองลากใส่ช่อง runner ใน Inspector)");
            enabled = false;
            return;
        }

        // event ชื่อ OnPoseResult ตามไฟล์ที่คุณส่งมา
        runner.OnPoseResult += OnPoseResult;
    }

    void OnDestroy()
    {
        if (runner != null)
            runner.OnPoseResult -= OnPoseResult;
    }

    private void OnPoseResult(PoseLandmarkerResult result)
    {
        _result = result;
        _hasResult = true;
    }

    void Update()
    {
        if (!_hasResult || _result.poseLandmarks == null || _result.poseLandmarks.Count == 0)
        {
            ResetState();
            return;
        }

        // poseLandmarks[0] เพราะ NumPoses = 1 (ใน runner คุณตั้งไว้แล้ว) :contentReference[oaicite:3]{index=3}
        var lm = _result.poseLandmarks[0].landmarks;

        // ดึงจุดหลัก: ไหล่ + หู (นิ่งกว่าแขน/ข้อมือ)
        const int LEFT_EAR = 7;
        const int RIGHT_EAR = 8;
        const int LEFT_SHOULDER = 11;
        const int RIGHT_SHOULDER = 12;

        Vector3 ls = ToVec(lm[LEFT_SHOULDER]);
        Vector3 rs = ToVec(lm[RIGHT_SHOULDER]);
        Vector3 le = ToVec(lm[LEFT_EAR]);
        Vector3 re = ToVec(lm[RIGHT_EAR]);

        Vector3 shoulderMid = (ls + rs) * 0.5f;
        Vector3 earMid = (le + re) * 0.5f;
        Vector3 headVec = earMid - shoulderMid;

        // มุมเอียงซ้าย/ขวา (ดูในระนาบ XY)
        float rawAngle = Mathf.Atan2(headVec.x, Mathf.Abs(headVec.y) + 1e-5f) * Mathf.Rad2Deg;

        // กรองสั่น (EMA)
        _filteredAngle = Mathf.Lerp(_filteredAngle, rawAngle, smoothing);

        float absA = Mathf.Abs(_filteredAngle);

        if (!_inPose)
        {
            if (absA >= enterAngleDeg)
            {
                _inPose = true;
                _holdTimer = 0f;
                _firedOnce = false;
            }
        }
        else
        {
            if (absA <= exitAngleDeg)
            {
                ResetState();
                return;
            }

            if (!_firedOnce)
            {
                _holdTimer += Time.deltaTime;
                if (_holdTimer >= holdSeconds)
                {
                    _firedOnce = true;
                    Debug.Log("✅ Side Neck Stretch OK (hold ผ่าน)");
                }
            }
        }
    }

    private void ResetState()
    {
        _inPose = false;
        _holdTimer = 0f;
        _firedOnce = false;
    }

    private static Vector3 ToVec(NormalizedLandmark p) => new Vector3(p.x, p.y, p.z);
}
