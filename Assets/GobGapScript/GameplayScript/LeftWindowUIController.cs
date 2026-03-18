using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeftWindowUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator poseAnimator;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private Image progressFill; // Image type = Filled
    [SerializeField] private TMP_Text holdingCountdownText;

    [Header("Animator State Names")]
    [SerializeField] private string routineStatePrefix = "Pose_";
    [SerializeField] private string bossStatePrefix = "BossPose_";

    // ✅ NEW: ท่านิ่ง (Idle Pose)
    [SerializeField] private string routineIdleStatePrefix = "PoseIdle";

    public void SetInstruction(string text)
    {
        if (instructionText != null)
            instructionText.text = text;
    }

    public void SetProgress01(float p01)
    {
        if (progressFill != null)
            progressFill.fillAmount = Mathf.Clamp01(p01);
    }

    public void ShowHoldingCountdown(int secondsRemaining)
    {
        if (holdingCountdownText != null)
            holdingCountdownText.text = secondsRemaining.ToString();
    }

    public void ClearHoldingCountdown()
    {
        if (holdingCountdownText != null)
            holdingCountdownText.text = "";
    }

    // ========================= POSE PLAY =========================

    public void PlayRoutinePose(int index1Based)
    {
        PlayPoseState(routineStatePrefix, index1Based);
    }

    public void PlayBossPose(int index1Based)
    {
        PlayPoseState(bossStatePrefix, index1Based);
    }

    // ✅ NEW: เล่นท่านิ่ง (Idle)
    public void PlayRoutineIdlePose(int index1Based = 1)
    {
        PlayPoseState(routineIdleStatePrefix, index1Based);
    }

    private void PlayPoseState(string prefix, int index1Based)
    {
        if (poseAnimator == null) return;

        string stateName = prefix + index1Based.ToString("00");
        poseAnimator.speed = 1f;
        poseAnimator.Play(stateName, 0, 0f);
    }

    // ========================= FREEZE =========================

    public void FreezePose()
    {
        if (poseAnimator != null)
            poseAnimator.speed = 0f;
    }

    public void UnfreezePose()
    {
        if (poseAnimator != null)
            poseAnimator.speed = 1f;
    }
}
