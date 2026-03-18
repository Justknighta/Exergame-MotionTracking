using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Pose Sequence Config Easy", fileName = "PoseSequenceConfigEasy")]
public class PoseSequenceConfigEasy : ScriptableObject
{
    [Header("Counts")]
    [Tooltip("จำนวนท่า routine ก่อนเข้าบอส")]
    public int routineCount = 10;

    [Tooltip("จำนวนท่าของบอส (HP บอส)")]
    public int bossCount = 5;

    [Header("Timing (seconds)")]

    [Tooltip("เวลาหน้า Are You Ready?")]
    public float warmupReadySeconds = 3f;

    [Tooltip("เวลาทำท่าก่อนเข้า Holding (แทน Detect)")]
    public float delaySeconds = 10f;

    [Tooltip("เวลาค้างท่า")]
    public float holdSeconds = 10f;

    [Tooltip("เวลาหน้า Boss Intro")]
    public float bossIntroDelaySeconds = 1.5f;

    [Tooltip("เวลาก่อนโหลด ScoreEasy")]
    public float victoryDelaySeconds = 2f;

    [Header("Instructions (optional)")]
    [Tooltip("ถ้าไม่ใส่ จะใช้ข้อความ default")]
    public string[] routineInstructions;

    public string[] bossInstructions;

    // ==============================
    // Instruction Helpers
    // ==============================

    public string GetRoutineInstruction(int index1Based)
    {
        int idx = index1Based - 1;

        if (routineInstructions != null &&
            idx >= 0 &&
            idx < routineInstructions.Length &&
            !string.IsNullOrEmpty(routineInstructions[idx]))
        {
            return routineInstructions[idx];
        }

        return $"ทำท่าที่ {index1Based} ตามตัวอย่าง";
    }

    public string GetBossInstruction(int index1Based)
    {
        int idx = index1Based - 1;

        if (bossInstructions != null &&
            idx >= 0 &&
            idx < bossInstructions.Length &&
            !string.IsNullOrEmpty(bossInstructions[idx]))
        {
            return bossInstructions[idx];
        }

        return $"ท่าบอสที่ {index1Based}";
    }
}