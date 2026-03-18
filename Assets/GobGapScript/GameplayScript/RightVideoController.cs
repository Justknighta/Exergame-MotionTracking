using UnityEngine;
using UnityEngine.Video;

public class RightVideoController : MonoBehaviour
{
    public enum VideoState
    {
        // Routine
        IdleLoop,
        LoseLoop,
        PreWinPose,
        WinPose,

        // Routine Transitions
        TransitionToCoin1,
        TransitionToMonster1,
        TransitionBG,
        TransitionToCoin2,
        TransitionToMonster2,

        // Boss
        BossIntro,
        IdleBossLoop,
        LoseBossLoop,
        PreWinBossPose,
        WinBossPose,
        KOBoss
    }

    public enum RoutineSet
    {
        Coin1,
        Monster1,
        Coin2,
        Monster2
    }

    [Header("Refs")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Routine - Coin1")]
    [SerializeField] private VideoClip idleLoopCoin1;
    [SerializeField] private VideoClip loseLoopCoin1;
    [SerializeField] private VideoClip preWinPoseCoin1;
    [SerializeField] private VideoClip winPoseCoin1;

    [Header("Routine - Monster1")]
    [SerializeField] private VideoClip idleLoopMonster1;
    [SerializeField] private VideoClip loseLoopMonster1;
    [SerializeField] private VideoClip preWinPoseLoopMonster1;
    [SerializeField] private VideoClip winPoseMonster1;

    [Header("Routine - Coin2")]
    [SerializeField] private VideoClip idleLoopCoin2;
    [SerializeField] private VideoClip loseLoopCoin2;
    [SerializeField] private VideoClip preWinPoseCoin2;
    [SerializeField] private VideoClip winPoseCoin2;

    [Header("Routine - Monster2")]
    [SerializeField] private VideoClip idleLoopMonster2;
    [SerializeField] private VideoClip loseLoopMonster2;
    [SerializeField] private VideoClip preWinPoseLoopMonster2;
    [SerializeField] private VideoClip winPoseMonster2;

    [Header("Transitions")]
    [SerializeField] private VideoClip transitionToCoin1;
    [SerializeField] private VideoClip transitionToMonster1;
    [SerializeField] private VideoClip transitionBG;
    [SerializeField] private VideoClip transitionToCoin2;
    [SerializeField] private VideoClip transitionToMonster2;

    [Header("Boss Clips")]
    [SerializeField] private VideoClip bossIntro;
    [SerializeField] private VideoClip idleBossLoop;
    [SerializeField] private VideoClip loseBossLoop;
    [SerializeField] private VideoClip preWinBossPose;
    [SerializeField] private VideoClip winBossPose;
    [SerializeField] private VideoClip koBoss;

    private VideoState? _current;
    private RoutineSet _currentRoutineSet = RoutineSet.Coin1;

    public void SetRoutineSet(RoutineSet routineSet)
    {
        _currentRoutineSet = routineSet;
    }

    public void Play(VideoState state, bool forceRestart = false, bool? overrideLoop = null)
    {
        if (videoPlayer == null) return;

        VideoClip clip = GetClip(state);
        if (clip == null)
        {
            Debug.LogWarning($"[RightVideoController] Missing clip for state: {state}");
            return;
        }

        bool shouldLoop = overrideLoop ?? GetDefaultLoopForState(state);

        bool sameState = _current.HasValue && _current.Value == state;
        bool sameClip = videoPlayer.clip == clip;

        if (!forceRestart && sameState && sameClip && videoPlayer.isPlaying && videoPlayer.isLooping == shouldLoop)
            return;

        _current = state;
        videoPlayer.isLooping = shouldLoop;

        if (forceRestart || !sameClip)
        {
            videoPlayer.Stop();
            videoPlayer.clip = clip;
            videoPlayer.time = 0;
        }
        else if (videoPlayer.clip == null)
        {
            videoPlayer.clip = clip;
            videoPlayer.time = 0;
        }

        videoPlayer.Play();
    }

    public void Pause()
    {
        if (videoPlayer == null) return;
        if (videoPlayer.isPlaying) videoPlayer.Pause();
    }

    public void Resume()
    {
        if (videoPlayer == null) return;
        if (videoPlayer.clip == null) return;

        if (!videoPlayer.isPlaying)
            videoPlayer.Play();
    }

    public void Stop()
    {
        if (videoPlayer == null) return;
        videoPlayer.Stop();
        _current = null;
    }

    public float GetClipLength(VideoState state)
    {
        VideoClip clip = GetClip(state);
        return clip != null ? (float)clip.length : 0f;
    }

    private bool GetDefaultLoopForState(VideoState state)
    {
        switch (state)
        {
            case VideoState.IdleLoop:
            case VideoState.LoseLoop:
            case VideoState.PreWinPose:
            case VideoState.IdleBossLoop:
            case VideoState.LoseBossLoop:
            case VideoState.PreWinBossPose:
                return true;

            default:
                return false;
        }
    }

    private VideoClip GetClip(VideoState state)
    {
        switch (state)
        {
            // Routine
            case VideoState.IdleLoop:
                return GetRoutineIdleClip();

            case VideoState.LoseLoop:
                return GetRoutineLoseClip();

            case VideoState.PreWinPose:
                return GetRoutinePreWinClip();

            case VideoState.WinPose:
                return GetRoutineWinClip();

            // Transitions
            case VideoState.TransitionToCoin1:
                return transitionToCoin1;

            case VideoState.TransitionToMonster1:
                return transitionToMonster1;

            case VideoState.TransitionBG:
                return transitionBG;

            case VideoState.TransitionToCoin2:
                return transitionToCoin2;

            case VideoState.TransitionToMonster2:
                return transitionToMonster2;

            // Boss
            case VideoState.BossIntro:
                return bossIntro;

            case VideoState.IdleBossLoop:
                return idleBossLoop;

            case VideoState.LoseBossLoop:
                return loseBossLoop;

            case VideoState.PreWinBossPose:
                return preWinBossPose;

            case VideoState.WinBossPose:
                return winBossPose;

            case VideoState.KOBoss:
                return koBoss;

            default:
                return null;
        }
    }

    private VideoClip GetRoutineIdleClip()
    {
        switch (_currentRoutineSet)
        {
            case RoutineSet.Coin1: return idleLoopCoin1;
            case RoutineSet.Monster1: return idleLoopMonster1;
            case RoutineSet.Coin2: return idleLoopCoin2;
            case RoutineSet.Monster2: return idleLoopMonster2;
            default: return idleLoopCoin1;
        }
    }

    private VideoClip GetRoutineLoseClip()
    {
        switch (_currentRoutineSet)
        {
            case RoutineSet.Coin1: return loseLoopCoin1;
            case RoutineSet.Monster1: return loseLoopMonster1;
            case RoutineSet.Coin2: return loseLoopCoin2;
            case RoutineSet.Monster2: return loseLoopMonster2;
            default: return loseLoopCoin1;
        }
    }

    private VideoClip GetRoutinePreWinClip()
    {
        switch (_currentRoutineSet)
        {
            case RoutineSet.Coin1: return preWinPoseCoin1;
            case RoutineSet.Monster1: return preWinPoseLoopMonster1;
            case RoutineSet.Coin2: return preWinPoseCoin2;
            case RoutineSet.Monster2: return preWinPoseLoopMonster2;
            default: return preWinPoseCoin1;
        }
    }

    private VideoClip GetRoutineWinClip()
    {
        switch (_currentRoutineSet)
        {
            case RoutineSet.Coin1: return winPoseCoin1;
            case RoutineSet.Monster1: return winPoseMonster1;
            case RoutineSet.Coin2: return winPoseCoin2;
            case RoutineSet.Monster2: return winPoseMonster2;
            default: return winPoseCoin1;
        }
    }

    public void SetRoutineVariantByIndex(int poseIndex1Based)
    {
        // compatibility กับโค้ดเก่า (โดยเฉพาะ GameplayEasyController)
        // odd/even เดิม:
        // คี่  -> set 1
        // คู่  -> set 2
        //
        // ในโครงใหม่จะ map เป็น:
        // set 1 -> Coin1
        // set 2 -> Coin2
        //
        // ถ้าใน Easy scene ยังใช้แค่ 2 ชุดอยู่ วิธีนี้จะทำให้รันต่อได้ก่อน
        _currentRoutineSet = (poseIndex1Based % 2 == 0)
            ? RoutineSet.Coin2
            : RoutineSet.Coin1;
    }
}
