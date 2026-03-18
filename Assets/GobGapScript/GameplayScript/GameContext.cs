using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameContext
{
    public enum Mode
    {
        Easy,
        Medium,
        Pro
    }

    // 1..5
    public static int SelectedLevel { get; private set; } = 1;

    public static Mode SelectedMode { get; private set; } = Mode.Pro;

    public static void SetSelection(int levelIndex, Mode mode)
    {
        if (levelIndex < 1) levelIndex = 1;
        SelectedLevel = levelIndex;
        SelectedMode = mode;
    }
}
