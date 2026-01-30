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

    [Header("💀 กระดูก Avatar (หัวไหล่ + มือ)")]
    public Transform leftShoulder;   // B-shoulder.L (หรือ clavicle)
    public Transform rightShoulder;  // B-shoulder.R
    public Transform leftHand;       // B-hand.L
    public Transform rightHand;      // B-hand.R

    [Header("⚙️ Hand Settings")]
    public Vector3 handAxis = new Vector3(0, 1, 0);   // แกน local ของมือ (คล้าย boneAxis แต่แยก)
    public Vector3 fixHandRotation = Vector3.zero;
    public float shoulderWeight = 0.5f; // หัวไหล่ขยับ “น้อยกว่า” แขนบน (กันสั่น)


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
    public float smooth = 40f;
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
    bool TryGetLm(
        System.Collections.Generic.IList<Mediapipe.Tasks.Components.Containers.NormalizedLandmark> lm,
        int idx,
        out Mediapipe.Tasks.Components.Containers.NormalizedLandmark p)
    {
        p = default;
        if (lm == null) return false;
        if (idx < 0 || idx >= lm.Count) return false;
        p = lm[idx];
        return true;
    }


    void LateUpdate()
    {
        if (!hasNewResult || latestResult.poseLandmarks == null || latestResult.poseLandmarks.Count == 0) return;
        var landmarks = latestResult.poseLandmarks[0].landmarks;
        autoInvertX = !useMirrorEffect;

        // 1. แขน (Arms)
        // 1. แขน (Arms)
        if (useMirrorEffect)
        {
            if (leftUpperArm &&
                TryGetLm(landmarks, 11, out var ls) &&
                TryGetLm(landmarks, 13, out var le) &&
                TryGetLm(landmarks, 15, out var lw) &&
                TryGetLm(landmarks, 19, out var li))
            {
                ProcessArm(leftUpperArm, leftForeArm, leftHand, ls, le, lw, li, false);
            }

            if (rightUpperArm &&
                TryGetLm(landmarks, 12, out var rs) &&
                TryGetLm(landmarks, 14, out var re) &&
                TryGetLm(landmarks, 16, out var rw) &&
                TryGetLm(landmarks, 20, out var ri))
            {
                ProcessArm(rightUpperArm, rightForeArm, rightHand, rs, re, rw, ri, true);
            }
        }



        // 2. ลำตัว (Spine)
        if (spineBone)
        if (TryGetLm(landmarks, 11, out var leftSh) && TryGetLm(landmarks, 12, out var rightSh))
        {
            float slopeY = (leftSh.y - rightSh.y);
            float slopeX = (leftSh.x - rightSh.x);
            float leanAngle = Mathf.Atan2(slopeY, slopeX) * Mathf.Rad2Deg;

            if (useMirrorEffect) leanAngle = -leanAngle;
            if (invertSpine) leanAngle = -leanAngle;

            Quaternion targetSpine =
                initialSpineRot *
                Quaternion.Euler(
                    fixSpineRotation.x,
                    fixSpineRotation.y,
                    (leanAngle * bodySensitivity) + fixSpineRotation.z);

            spineBone.rotation =
                Quaternion.Slerp(spineBone.rotation, targetSpine, Time.deltaTime * smooth);
        }


        // 3. หัว (Head)
        if (TryGetLm(landmarks, 7, out var leftEar) && TryGetLm(landmarks, 8, out var rightEar))
        {
            float headSlopeY = (leftEar.y - rightEar.y);
            float headSlopeX = (leftEar.x - rightEar.x);
            float headTilt = Mathf.Atan2(headSlopeY, headSlopeX) * Mathf.Rad2Deg;

            if (useMirrorEffect) headTilt = -headTilt;
            if (invertHead) headTilt = -headTilt;

            Quaternion targetHead =
                initialHeadRot *
                Quaternion.Euler(
                    fixHeadRotation.x,
                    fixHeadRotation.y,
                    headTilt + fixHeadRotation.z);

            headBone.rotation =
                Quaternion.Slerp(headBone.rotation, targetHead, Time.deltaTime * smooth);
        }

        
        hasNewResult = false;
    }

    void ProcessArm(
        Transform upper, Transform lower, Transform hand,
        Mediapipe.Tasks.Components.Containers.NormalizedLandmark s,
        Mediapipe.Tasks.Components.Containers.NormalizedLandmark e,
        Mediapipe.Tasks.Components.Containers.NormalizedLandmark w,
        Mediapipe.Tasks.Components.Containers.NormalizedLandmark index,
        bool isRightSide)
    {
        Vector3 dirUp  = GetDir(s, e);
        Vector3 dirLow = GetDir(e, w);

        RotateArmBone(upper, dirUp, isRightSide);
        RotateArmBone(lower, dirLow, isRightSide);

        // ✅ หมุนมือด้วยทิศ wrist -> index
        if (hand != null)
        {
            Vector3 dirHand = GetDir(w, index);
            RotateHandBone(hand, dirHand, isRightSide);
        }
    }


    void RotateArmBone(Transform bone, Vector3 direction, bool isRightSide)
    {
        Quaternion baseRot = Quaternion.FromToRotation(boneAxis, direction);
        float invZ = isRightSide ? -1f : 1f;
        float invY = isRightSide ? -1f : 1f;
        Quaternion offsetRot = Quaternion.Euler(fixArmRotation.x, fixArmRotation.y * invY, fixArmRotation.z * invZ);
        bone.rotation = Quaternion.Slerp(bone.rotation, baseRot * offsetRot, Time.deltaTime * smooth);
    }
    void RotateHandBone(Transform bone, Vector3 direction, bool isRightSide)
    {
        Quaternion baseRot = Quaternion.FromToRotation(handAxis, direction);
        float invZ = isRightSide ? -1f : 1f;
        float invY = isRightSide ? -1f : 1f;
        Quaternion offsetRot = Quaternion.Euler(fixHandRotation.x, fixHandRotation.y * invY, fixHandRotation.z * invZ);

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