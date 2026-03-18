using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

public static class LeaderboardService
{
    private const string PREFIX = "leaderboard";

    // ตาม requirement: 5 levels, 2 modes (Medium/Pro) -> 10 ไฟล์ต่อ dev/real
    public const int MaxLevels = 5;

    private static bool IsDevBuild()
    {
#if UNITY_EDITOR
        return true;
#else
        return false;
#endif
    }

    // ----------------------------
    // Filename / Path
    // ----------------------------
    public static string GetFileName(int level, GameContext.Mode mode, bool isDev)
    {
        // จำกัดเฉพาะ Medium/Pro ตาม 20 รูปแบบที่คุณต้องการ
        if (mode != GameContext.Mode.Medium && mode != GameContext.Mode.Pro)
        {
            Debug.LogWarning($"[LeaderboardService] Mode '{mode}' not supported for leaderboard split. Using Pro as fallback.");
            mode = GameContext.Mode.Pro;
        }

        level = Mathf.Clamp(level, 1, MaxLevels);

        string devPart = isDev ? "_dev" : "";
        // ตัวอย่าง: leaderboard_dev_Lv1_Pro.json
        return $"{PREFIX}{devPart}_Lv{level}_{mode}.json";
    }

    public static string GetSavePath(int level, GameContext.Mode mode, bool isDev)
    {
        string fileName = GetFileName(level, mode, isDev);
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    // “จาก Context”
    public static string GetSavePathFromContext()
    {
        return GetSavePath(GameContext.SelectedLevel, GameContext.SelectedMode, IsDevBuild());
    }

    // ----------------------------
    // Load / Save
    // ----------------------------
    public static LeaderboardData Load(int level, GameContext.Mode mode, bool isDev)
    {
        try
        {
            var path = GetSavePath(level, mode, isDev);

            if (!File.Exists(path))
                return new LeaderboardData();

            var json = File.ReadAllText(path);
            if (string.IsNullOrEmpty(json))
                return new LeaderboardData();

            var data = JsonUtility.FromJson<LeaderboardData>(json);
            if (data == null) data = new LeaderboardData();
            if (data.entries == null) data.entries = new System.Collections.Generic.List<LeaderboardEntry>();

            // กัน nextInsertId หาย / เป็น 0
            if (data.nextInsertId <= 0)
            {
                long max = 0;
                for (int i = 0; i < data.entries.Count; i++)
                    if (data.entries[i] != null && data.entries[i].insertId > max) max = data.entries[i].insertId;
                data.nextInsertId = max + 1;
            }

            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LeaderboardService] Load failed: {e.Message}");
            return new LeaderboardData();
        }
    }

    public static void Save(int level, GameContext.Mode mode, bool isDev, LeaderboardData data)
    {
        try
        {
            if (data == null) data = new LeaderboardData();
            if (data.entries == null) data.entries = new System.Collections.Generic.List<LeaderboardEntry>();

            var path = GetSavePath(level, mode, isDev);
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LeaderboardService] Save failed: {e.Message}");
        }
    }

    // ----------------------------
    // Context helpers (ตามที่ขอ)
    // ----------------------------
    public static LeaderboardData LoadFromContext()
    {
        return Load(GameContext.SelectedLevel, GameContext.SelectedMode, IsDevBuild());
    }

    public static int InsertAndSortFromContext(
        LeaderboardData data,
        string playerName,
        int score,
        int maxEntries,
        out long insertedInsertId
    )
    {
        int level = GameContext.SelectedLevel;
        var mode = GameContext.SelectedMode;
        bool isDev = IsDevBuild();

        int insertedIndex = InsertAndSort(data, playerName, score, maxEntries, out insertedInsertId);

        // สำคัญ: Save ไปไฟล์ของ (level/mode/dev) นี้เท่านั้น
        Save(level, mode, isDev, data);
        return insertedIndex;
    }

    // ----------------------------
    // Core Insert/Sort (ไม่ผูก context)
    // ----------------------------
    public static int InsertAndSort(
        LeaderboardData data,
        string playerName,
        int score,
        int maxEntries,
        out long insertedInsertId
    )
    {
        if (data == null) data = new LeaderboardData();
        if (data.entries == null) data.entries = new System.Collections.Generic.List<LeaderboardEntry>();

        var entry = new LeaderboardEntry
        {
            name = playerName,
            score = score,
            insertId = data.nextInsertId
        };

        insertedInsertId = entry.insertId;
        data.nextInsertId++;

        data.entries.Add(entry);

        data.entries.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            // score desc
            int cmp = b.score.CompareTo(a.score);
            if (cmp != 0) return cmp;

            // insertId desc (newer first)
            return b.insertId.CompareTo(a.insertId);
        });

        if (maxEntries > 0 && data.entries.Count > maxEntries)
            data.entries.RemoveRange(maxEntries, data.entries.Count - maxEntries);

        int insertedIndex = -1;
        for (int i = 0; i < data.entries.Count; i++)
        {
            if (data.entries[i] != null && data.entries[i].insertId == insertedInsertId)
            {
                insertedIndex = i;
                break;
            }
        }

        return insertedIndex;
    }

    // ----------------------------
    // Dev clear helpers
    // ----------------------------
    public static void ClearAllDevFiles()
    {
        bool isDev = true;

        for (int lv = 1; lv <= MaxLevels; lv++)
        {
            DeleteIfExists(GetSavePath(lv, GameContext.Mode.Medium, isDev));
            DeleteIfExists(GetSavePath(lv, GameContext.Mode.Pro, isDev));
        }
    }

    private static void DeleteIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[LeaderboardService] Deleted: {path}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LeaderboardService] Delete failed: {e.Message}");
        }
    }
}