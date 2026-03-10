using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Pose Sequence Config", fileName = "PoseSequenceConfig")]
public class PoseSequenceConfig : ScriptableObject
{
    [Header("Counts")]
    public int routineCount = 15;
    public int bossCount = 10;

    [Header("Timing (seconds)")]
    public float warmupReadySeconds = 3f;

    public float routineDetectSeconds = 20f; // ✅ routine 20 วิ
    public float bossDetectSeconds = 10f;    // ✅ boss 10 วิ

    public float holdSeconds = 10f;
    public float bossIntroDelaySeconds = 1.5f;
    public float victoryDelaySeconds = 2f;

    [Header("Scoring")]
    public int perfectDamage = 1000;
    public int goodDamage = 500;

    [Header("Instructions (optional)")]
    [Tooltip("ถ้าไม่ใส่ จะใช้ข้อความ default")]
    public string[] routineInstructions; // size >= routineCount (optional)
    public string[] bossInstructions;    // size >= bossCount (optional)

    public string GetRoutineInstruction(int index1Based)
    {
        int idx = index1Based - 1;
        if (routineInstructions != null && idx >= 0 && idx < routineInstructions.Length && !string.IsNullOrEmpty(routineInstructions[idx]))
            return routineInstructions[idx];
        return $"คำอธิบายท่าที่ {index1Based}";
    }

    public string GetBossInstruction(int index1Based)
    {
        int idx = index1Based - 1;
        if (bossInstructions != null && idx >= 0 && idx < bossInstructions.Length && !string.IsNullOrEmpty(bossInstructions[idx]))
            return bossInstructions[idx];
        return $"คำอธิบายท่าที่ {index1Based} (บอส)";
    }
}
