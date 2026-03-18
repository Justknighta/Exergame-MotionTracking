#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class LeaderboardDebugMenu
{
    [MenuItem("Tools/Leaderboard/Clear DEV Leaderboards (All Levels/Mode)")]
    public static void ClearDevLeaderboards_All()
    {
        LeaderboardService.ClearAllDevFiles();
        Debug.Log("[LeaderboardDebug] Cleared ALL DEV leaderboard files (Lv1-5, Medium/Pro).");
    }

    [MenuItem("Tools/Leaderboard/Reveal Save Folder")]
    public static void RevealSaveFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
#endif