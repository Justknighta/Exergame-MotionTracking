using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class RotationBridge : MonoBehaviour
{
    [Header("🔌 เชื่อมต่อ")]
    public PoseLandmarkerRunner runner;

    [Header("💀 กระดูก Avatar (แขน)")]
    public Transform leftUpperArm;  
    public Transform leftForeArm;   
    public Transform rightUpperArm; 
    public Transform rightForeArm;

    [Header("💀 กระดูก Avatar (ตัวและหัว)")]
    public Transform spineBone; // B-spine
    public Transform headBone;  // B-head

    [Header("🪞 โหมดกระจก (Mirror)")]
    public bool useMirrorEffect = true; 

    [Header("🎚️ ตั้งค่าแกน (Bone Settings)")]
    public Vector3 boneAxis = new Vector3(0, 1, 0);

    [Header("🔧 จูนทิศทาง (Invert Settings) ⭐ ใหม่!")]
    [Tooltip("ถ้าหัวเอียงผิดด้าน ให้ติ๊กอันนี้")]
    public bool invertHead = true; // ผมเปิดไว้ให้เลย เพราะของคุณน่าจะต้องใช้
    [Tooltip("ถ้าตัวเอียงผิดด้าน ให้ติ๊กอันนี้")]
    public bool invertSpine = false; 

    [Header("🔧 จูนองศา (Offset)")]
    public Vector3 fixArmRotation = Vector3.zero; 
    public Vector3 fixSpineRotation = Vector3.zero;
    public Vector3 fixHeadRotation = Vector3.zero;

    [Header("⚙️ ความไว")]
    public float smooth = 25f;
    public float bodySensitivity = 1.5f; 

    private bool autoInvertX = false; 
    private PoseLandmarkerResult latestResult;
    private bool hasNewResult = false;
    private Quaternion initialSpineRot;
    private Quaternion initialHeadRot;

    void Start()
    {
        if (runner != null) runner.OnPoseResult += OnResultReceived;

        if (spineBone) initialSpineRot = spineBone.rotation;
        if (headBone) initialHeadRot = headBone.rotation;
    }

    void OnDestroy()
    {
        if (runner != null) runner.OnPoseResult -= OnResultReceived;
    }

    private void OnResultReceived(PoseLandmarkerResult result)
    {
        latestResult = result;
        hasNewResult = true;
    }

    void LateUpdate()
    {
        if (!hasNewResult || latestResult.poseLandmarks == null || latestResult.poseLandmarks.Count == 0) return;
        var landmarks = latestResult.poseLandmarks[0].landmarks;
        autoInvertX = !useMirrorEffect;

        // 1. แขน (Arms)
        if (useMirrorEffect)
        {
            if (leftUpperArm) ProcessArm(leftUpperArm, leftForeArm, landmarks[11], landmarks[13], landmarks[15], false);
            if (rightUpperArm) ProcessArm(rightUpperArm, rightForeArm, landmarks[12], landmarks[14], landmarks[16], true);
        }
        else
        {
            if (leftUpperArm) ProcessArm(leftUpperArm, leftForeArm, landmarks[12], landmarks[14], landmarks[16], false);
            if (rightUpperArm) ProcessArm(rightUpperArm, rightForeArm, landmarks[11], landmarks[13], landmarks[15], true);
        }

        // 2. ลำตัว (Spine)
        if (spineBone)
        {
            var leftSh = landmarks[11]; var rightSh = landmarks[12];
            float slopeY = (leftSh.y - rightSh.y);
            float slopeX = (leftSh.x - rightSh.x);
            float leanAngle = Mathf.Atan2(slopeY, slopeX) * Mathf.Rad2Deg;
            
            // Logic กลับด้านตัว
            if (useMirrorEffect) leanAngle = -leanAngle;
            if (invertSpine) leanAngle = -leanAngle; // ⭐ กลับด้านอีกรอบถ้าติ๊ก

            Quaternion targetSpine = initialSpineRot * Quaternion.Euler(fixSpineRotation.x, fixSpineRotation.y, (leanAngle * bodySensitivity) + fixSpineRotation.z);
            spineBone.rotation = Quaternion.Slerp(spineBone.rotation, targetSpine, Time.deltaTime * smooth);
        }

        // 3. หัว (Head)
        if (headBone)
        {
            var leftEar = landmarks[7]; var rightEar = landmarks[8];
            float headSlopeY = (leftEar.y - rightEar.y);
            float headSlopeX = (leftEar.x - rightEar.x);
            float headTilt = Mathf.Atan2(headSlopeY, headSlopeX) * Mathf.Rad2Deg;
            
            // Logic กลับด้านหัว
            if (useMirrorEffect) headTilt = -headTilt;
            if (invertHead) headTilt = -headTilt; // ⭐ กลับด้านอีกรอบถ้าติ๊ก

            Quaternion targetHead = initialHeadRot * Quaternion.Euler(fixHeadRotation.x, fixHeadRotation.y, headTilt + fixHeadRotation.z);
            headBone.rotation = Quaternion.Slerp(headBone.rotation, targetHead, Time.deltaTime * smooth);
        }
        
        hasNewResult = false;
    }

    void ProcessArm(Transform upper, Transform lower, 
                     Mediapipe.Tasks.Components.Containers.NormalizedLandmark s, 
                     Mediapipe.Tasks.Components.Containers.NormalizedLandmark e, 
                     Mediapipe.Tasks.Components.Containers.NormalizedLandmark w,
                     bool isRightSide)
    {
        Vector3 dirUp = GetDir(s, e);
        Vector3 dirLow = GetDir(e, w);
        RotateArmBone(upper, dirUp, isRightSide);
        RotateArmBone(lower, dirLow, isRightSide);
    }

    void RotateArmBone(Transform bone, Vector3 direction, bool isRightSide)
    {
        Quaternion baseRot = Quaternion.FromToRotation(boneAxis, direction);
        float invZ = isRightSide ? -1f : 1f;
        float invY = isRightSide ? -1f : 1f;
        Quaternion offsetRot = Quaternion.Euler(fixArmRotation.x, fixArmRotation.y * invY, fixArmRotation.z * invZ);
        bone.rotation = Quaternion.Slerp(bone.rotation, baseRot * offsetRot, Time.deltaTime * smooth);
    }

    Vector3 GetDir(Mediapipe.Tasks.Components.Containers.NormalizedLandmark from, Mediapipe.Tasks.Components.Containers.NormalizedLandmark to)
    {
        float x = (to.x - from.x) * (autoInvertX ? -1 : 1); 
        float y = -(to.y - from.y);
        float z = (to.z - from.z);
        return new Vector3(x, y, z).normalized;
    }
}