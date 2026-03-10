using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameSessionResult
{
    public static int FinalScore;
    public static int PerfectCount;
    public static int GoodCount;
    public static bool RewardGranted;

    public static void SetResult(int finalScore, int perfectCount, int goodCount)
    {
        FinalScore = finalScore;
        PerfectCount = perfectCount;
        GoodCount = goodCount;
    }

    public static void Clear()
    {
        FinalScore = 0;
        PerfectCount = 0;
        GoodCount = 0;
        RewardGranted = false;
    }
}


