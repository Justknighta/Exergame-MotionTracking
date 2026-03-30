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
    public float routineDetectSeconds = 20f;
    public float bossDetectSeconds = 10f;
    public float holdSeconds = 10f;
    public float bossIntroDelaySeconds = 1.5f;
    public float victoryDelaySeconds = 2f;

    [Header("Scoring")]
    public int perfectDamage = 1000;
    public int goodDamage = 500;

    [Header("Pose IDs")]
    public int[] routinePoseIDs; // size >= routineCount
    public int[] bossPoseIDs;    // size >= bossCount

    [Header("Instructions (optional)")]
    [Tooltip("ถ้าไม่ใส่ จะใช้ข้อความ default")]
    public string[] routineInstructions;
    public string[] bossInstructions;

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

    public int GetRoutinePoseID(int index1Based)
    {
        int idx = index1Based - 1;
        if (routinePoseIDs != null && idx >= 0 && idx < routinePoseIDs.Length)
            return routinePoseIDs[idx];
        return -1;
    }

    public int GetBossPoseID(int index1Based)
    {
        int idx = index1Based - 1;
        if (bossPoseIDs != null && idx >= 0 && idx < bossPoseIDs.Length)
            return bossPoseIDs[idx];
        return -1;
    }
}