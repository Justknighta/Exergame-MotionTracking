using UnityEngine;

public class RightVideoController : MonoBehaviour
{
    public enum VideoState
    {
        IdleLoop,
        IdleBossLoop,
        LoseLoop,
        LoseBossLoop,
        PreWinPose,
        PreWinBossPose,
        WinPose,
        WinBossPose,
        BossIntro,
        KOBoss
    }

    public void Play(VideoState state, bool loop = false) { }
    public void Pause() { }
    public void Resume() { }
    public void SetRoutineVariantByIndex(int index) { }
    public void SetBossVariantByIndex(int index) { }
}