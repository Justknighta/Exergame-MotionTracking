using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ===============================
// Progress Data (Saved to JSON)
// ===============================
[Serializable]
public class ProgressData
{
    public int coins = 0;

    // เก็บด่านที่ปลดล็อกแล้ว (1..5)
    public List<int> unlockedLevels = new List<int>();

    // เผื่อขยายอนาคต
    public int version = 1;

    public bool IsLevelUnlocked(int level)
    {
        return unlockedLevels != null && unlockedLevels.Contains(level);
    }

    public void EnsureLevelUnlocked(int level)
    {
        if (unlockedLevels == null) unlockedLevels = new List<int>();
        if (!unlockedLevels.Contains(level))
            unlockedLevels.Add(level);
    }
}

// ===============================
// Progress Service (Static)
// ===============================
public static class ProgressService
{
    // -------- Config --------
    public const int MaxLevels = 5;
    public const int DefaultFreeLevel = 1;

    // Dev/Test: เหรียญเริ่มต้น
    private const int DevStartingCoins = 10000;

    // 파일 이름แยก dev/real
    private const string RealFileName = "progress_real.json";
    private const string DevFileName  = "progress_dev.json";

    // -------- Runtime cache --------
    private static ProgressData _cache;
    private static bool _loaded;

    // ======= Public API =======

    /// <summary>โหลด progress (cache) ถ้ายังไม่โหลด</summary>
    public static ProgressData Load()
    {
        if (_loaded && _cache != null) return _cache;

        string path = GetSavePath();
        ProgressData data = null;

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                data = JsonUtility.FromJson<ProgressData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProgressService] Failed to read progress JSON. Will create new. Error: {e}");
            }
        }

        if (data == null)
        {
            data = CreateDefaultProgress();
            // สร้างครั้งแรกแล้ว save ไว้เลย
            Save(data);
        }

        Normalize(data);

        _cache = data;
        _loaded = true;
        return _cache;
    }

    /// <summary>บันทึก progress (และอัปเดต cache)</summary>
    public static void Save(ProgressData data)
    {
        if (data == null) return;

        Normalize(data);

        string path = GetSavePath();
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(path, json);

            _cache = data;
            _loaded = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ProgressService] Failed to write progress JSON: {e}");
        }
    }

    public static int GetCoins()
    {
        return Load().coins;
    }

    public static void AddCoins(int amount)
    {
        if (amount <= 0) return;
        var d = Load();
        d.coins += amount;
        Save(d);
    }

    /// <summary>
    /// พยายามหักเหรียญ ถ้าไม่พอคืน false (ไม่เปลี่ยนค่า)
    /// </summary>
    public static bool TrySpendCoins(int amount)
    {
        if (amount <= 0) return true;

        var d = Load();
        if (d.coins < amount) return false;

        d.coins -= amount;
        Save(d);
        return true;
    }

    public static bool IsLevelUnlocked(int level)
    {
        if (level <= 0 || level > MaxLevels) return false;
        return Load().IsLevelUnlocked(level);
    }

    /// <summary>
    /// ปลดล็อกด่านแบบ “ซื้อด้วยเหรียญ”
    /// - ถ้าปลดแล้ว -> true
    /// - ถ้าเหรียญไม่พอ -> false
    /// - ด่าน 1 ฟรีอยู่แล้ว
    /// </summary>
    public static bool TryUnlockLevel(int level, out int missingCoins)
    {
        missingCoins = 0;

        if (level <= 0 || level > MaxLevels) return false;

        var d = Load();

        if (d.IsLevelUnlocked(level))
            return true;

        int cost = GetUnlockCost(level);
        if (cost <= 0)
        {
            d.EnsureLevelUnlocked(level);
            Save(d);
            return true;
        }

        if (d.coins < cost)
        {
            missingCoins = cost - d.coins;
            return false;
        }

        d.coins -= cost;
        d.EnsureLevelUnlocked(level);
        Save(d);
        return true;
    }

    public static int GetUnlockCost(int level)
    {
        // ตามที่คุณกำหนด:
        // L2=1000, L3=2000, L4=2500, L5=5000 (L1 ฟรี)
        return level switch
        {
            1 => 0,
            2 => 1000,
            3 => 2000,
            4 => 2500,
            5 => 5000,
            _ => int.MaxValue
        };
    }

    /// <summary>
    /// สำหรับหน้า Level/Shop: โหลดแล้วคืน progress object (สะดวกเอาไป bind UI)
    /// </summary>
    public static ProgressData GetSnapshot()
    {
        // คืน clone แบบเบา ๆ กันคนแก้ค่าแล้วลืม Save (optional)
        // แต่ตอนนี้คืนตัวจริงไปก่อนจะใช้ง่ายกว่า
        return Load();
    }

    /// <summary>
    /// ล้าง cache (เผื่ออยาก reload ใหม่ใน runtime)
    /// </summary>
    public static void ClearCache()
    {
        _cache = null;
        _loaded = false;
    }

#if UNITY_EDITOR
    // ======= Editor Debug Menu =======

    [MenuItem("Debug/Progress/Clear DEV Progress (Delete JSON)")]
    public static void Editor_ClearDevProgressFile()
    {
        string path = GetSavePath_EditorForceDev();
        if (File.Exists(path))
            File.Delete(path);

        ClearCache();
        Debug.Log($"[ProgressService] DEV progress cleared: {path}");
    }

    [MenuItem("Debug/Progress/Open Save Folder")]
    public static void Editor_OpenSaveFolder()
    {
        string folder = Application.persistentDataPath;
        EditorUtility.RevealInFinder(folder);
    }
#endif

    // ======= Internal helpers =======

    private static void Normalize(ProgressData d)
    {
        if (d == null) return;

        if (d.unlockedLevels == null)
            d.unlockedLevels = new List<int>();

        // ด่าน 1 ต้องปลดเสมอ
        if (!d.unlockedLevels.Contains(DefaultFreeLevel))
            d.unlockedLevels.Add(DefaultFreeLevel);

        // กันข้อมูลแปลก ๆ
        d.unlockedLevels.RemoveAll(x => x <= 0 || x > MaxLevels);

        // ไม่ให้เหรียญติดลบ
        if (d.coins < 0) d.coins = 0;
    }

    private static ProgressData CreateDefaultProgress()
    {
        var d = new ProgressData();

        // ด่าน 1 ฟรี
        d.EnsureLevelUnlocked(DefaultFreeLevel);

#if UNITY_EDITOR
        // DEV mode: ตั้งเหรียญเริ่มต้นไว้เทส unlock
        d.coins = DevStartingCoins;
#else
        d.coins = 0;
#endif

        return d;
    }

    private static string GetSavePath()
    {
        string fileName =
#if UNITY_EDITOR
            DevFileName;
#else
            RealFileName;
#endif
        return Path.Combine(Application.persistentDataPath, fileName);
    }

#if UNITY_EDITOR
    private static string GetSavePath_EditorForceDev()
    {
        return Path.Combine(Application.persistentDataPath, DevFileName);
    }
#endif
}