using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayEasyController : MonoBehaviour
{
    private enum State
    {
        WarmupIntro,

        Routine_Delay,
        Routine_Holding,

        Boss_Intro,
        Boss_Delay,
        Boss_Holding,

        Victory
    }

    [Header("Scene Refs")]
    [SerializeField] private LeftWindowUIController leftUI;
    [SerializeField] private RightVideoController rightVideo;
    [SerializeField] private HUDController hud;
    [SerializeField] private TimerService timer;

    [Header("Config")]
    [SerializeField] private PoseSequenceConfigEasy config;

    [Header("Scene Names")]
    [SerializeField] private string scoreEasySceneName = "ScoreEasy";

    [Header("Easy Timing Overrides")]
    [Tooltip("ถ้าเปิด จะใช้ค่าคงที่แทน config.delaySeconds")]
    [SerializeField] private bool useFixedDelaySeconds = true;
    [SerializeField] private float fixedDelaySeconds = 10f;

    [Header("Delays / Fallbacks")]
    [SerializeField] private float postWinPoseDelaySeconds = 1.0f;
    [SerializeField] private float missingClipFallbackSeconds = 0.05f;

    [Header("Warmup UI")]
    [SerializeField] private GameObject warmupOverlayRoot;
    [SerializeField] private TMPro.TMP_Text warmupCountdownText;
    [SerializeField] private float warmupReadySecondsFallback = 3f;

    private int _routineIndex = 1;
    private int _bossIndex = 1;
    private int _bossProgress = 0;

    private State _state;
    private bool _paused;

    private int _runId = 0;
    private int _activeRunId = 0;

    private Coroutine _warmupRoutine;
    private Coroutine _stateSequenceRoutine;

    private int _lastHoldSecLeft = -1;

    private void Awake()
    {
        if (timer == null)
        {
            timer = GetComponent<TimerService>();
            if (timer == null) timer = gameObject.AddComponent<TimerService>();
        }
    }

    private void Start()
    {
        Debug.Log($"[GameplayEasy] Level = {GameContext.SelectedLevel} | Mode = {GameContext.SelectedMode}");

        GameSessionResult.Clear();

        // Easy: ไม่ใช้หัวใจ/คะแนน
        hud?.SetHeartsVisible(false);
        hud?.SetScore(0);
        hud?.HideAllResultPopups();
        hud?.HideBossPower();

        _routineIndex = 1;
        _bossIndex = 1;
        _bossProgress = 0;

        EnterState(State.WarmupIntro);
    }

    // ---------- Public buttons ----------
    public void PauseGame()
    {
        if (_paused) return;
        _paused = true;

        timer?.Pause();
        rightVideo?.Pause();
        leftUI?.FreezePose();
    }

    public void ResumeGame()
    {
        if (!_paused) return;
        _paused = false;

        timer?.Resume();
        rightVideo?.Resume();
        leftUI?.UnfreezePose();
    }

    // ========================= STATE MACHINE =========================

    private void EnterState(State newState)
    {
        ExitState(_state);
        _state = newState;

        switch (_state)
        {
            case State.WarmupIntro:
                StartWarmup();
                break;

            case State.Routine_Delay:
                StartDelay(isBoss: false);
                break;

            case State.Routine_Holding:
                StartHolding(isBoss: false);
                break;

            case State.Boss_Intro:
                StartBossIntro();
                break;

            case State.Boss_Delay:
                StartDelay(isBoss: true);
                break;

            case State.Boss_Holding:
                StartHolding(isBoss: true);
                break;

            case State.Victory:
                StartVictory();
                break;
        }
    }

    private void ExitState(State state)
    {
        timer?.Cancel();
        leftUI?.ClearHoldingCountdown();

        if (_stateSequenceRoutine != null)
        {
            StopCoroutine(_stateSequenceRoutine);
            _stateSequenceRoutine = null;
        }

        if (state == State.WarmupIntro)
        {
            if (_warmupRoutine != null)
            {
                StopCoroutine(_warmupRoutine);
                _warmupRoutine = null;
            }

            if (warmupOverlayRoot != null) warmupOverlayRoot.SetActive(false);
            if (warmupCountdownText != null) warmupCountdownText.text = "";
        }
    }

    private bool IsRunStale(int runId) => runId != _activeRunId;
    private bool IsRunStale() => _activeRunId != _runId;

    // ========================= WARMUP =========================

    private void StartWarmup()
    {
        _runId++;
        _activeRunId = _runId;

        if (_warmupRoutine != null) StopCoroutine(_warmupRoutine);
        _warmupRoutine = StartCoroutine(WarmupSequence(_activeRunId));
    }

    private IEnumerator WaitRealtimePausable(float seconds, int runId)
    {
        float t = 0f;

        while (t < seconds)
        {
            if (IsRunStale(runId))
                yield break;

            if (!_paused)
                t += Time.unscaledDeltaTime;

            yield return null;
        }
    }

    private IEnumerator WarmupSequence(int runId)
    {
        if (warmupOverlayRoot != null) warmupOverlayRoot.SetActive(true);

        // Warmup ใช้ Coin1 idle เป็นค่าเริ่มต้น
        rightVideo?.SetRoutineSet(RightVideoController.RoutineSet.Coin1);
        rightVideo?.Play(RightVideoController.VideoState.IdleLoop, true);

        leftUI?.UnfreezePose();
        leftUI?.PlayRoutineIdlePose(1);

        if (warmupCountdownText != null)
            warmupCountdownText.text = "Are You Ready?";

        float ready = (config != null) ? config.warmupReadySeconds : warmupReadySecondsFallback;

        yield return WaitRealtimePausable(ready, runId);
        if (IsRunStale(runId)) yield break;

        for (int i = 3; i >= 1; i--)
        {
            if (warmupCountdownText != null)
                warmupCountdownText.text = i.ToString();

            yield return WaitRealtimePausable(1f, runId);
            if (IsRunStale(runId)) yield break;
        }

        if (warmupCountdownText != null)
            warmupCountdownText.text = "Go!";

        yield return WaitRealtimePausable(0.5f, runId);
        if (IsRunStale(runId)) yield break;

        if (warmupOverlayRoot != null) warmupOverlayRoot.SetActive(false);
        if (warmupCountdownText != null) warmupCountdownText.text = "";

        EnterState(State.Routine_Delay);
    }

    // ========================= DELAY (แทน Detect) =========================

    private float GetDelaySeconds(bool isBoss)
    {
        if (useFixedDelaySeconds)
            return Mathf.Max(0.1f, fixedDelaySeconds);

        if (config == null) return 10f;
        return Mathf.Max(0.1f, config.delaySeconds);
    }

    private void StartDelay(bool isBoss)
    {
        _runId++;
        _activeRunId = _runId;

        leftUI?.UnfreezePose();
        leftUI?.SetProgress01(1f);

        int index = isBoss ? _bossIndex : _routineIndex;

        if (isBoss)
        {
            leftUI?.PlayBossPose(index);
            leftUI?.SetInstruction(config != null ? config.GetBossInstruction(index) : $"คำอธิบายท่าที่ {index} (บอส)");
            rightVideo?.Play(RightVideoController.VideoState.IdleBossLoop, true);
        }
        else
        {
            leftUI?.PlayRoutinePose(index);
            leftUI?.SetInstruction(config != null ? config.GetRoutineInstruction(index) : $"คำอธิบายท่าที่ {index}");

            if (rightVideo != null)
            {
                rightVideo.SetRoutineSet(GetRoutineSetForRound(index));
                rightVideo.Play(RightVideoController.VideoState.IdleLoop, true);
            }
        }

        float delaySeconds = GetDelaySeconds(isBoss);

        timer.StartTimer(delaySeconds,
            p01 =>
            {
                if (IsRunStale()) return;
                leftUI?.SetProgress01(p01);
            },
            () =>
            {
                if (IsRunStale()) return;

                // Easy ไม่มี detect จริง: ครบเวลาแล้วถือว่าผ่าน
                AudioManager.SFX(SfxId.PoseSuccess);

                EnterState(isBoss ? State.Boss_Holding : State.Routine_Holding);
            });
    }

    // ========================= HOLDING =========================

    private void StartHolding(bool isBoss)
    {
        _runId++;
        _activeRunId = _runId;

        float holdSeconds = (config != null) ? config.holdSeconds : 10f;

        leftUI?.FreezePose();
        leftUI?.SetInstruction($"ค้างไว้ {Mathf.RoundToInt(holdSeconds)} วินาทีนะ");

        if (isBoss)
        {
            // Flow ใหม่: boss ไม่มี pre-win แล้ว ใช้ WinBossPose ตั้งแต่ตอน Holding
            rightVideo?.Play(RightVideoController.VideoState.WinBossPose, true, true);
        }
        else
        {
            if (rightVideo != null)
            {
                rightVideo.SetRoutineSet(GetRoutineSetForRound(_routineIndex));
                rightVideo.Play(RightVideoController.VideoState.PreWinPose, true);
            }
        }

        _lastHoldSecLeft = -1;

        timer.StartTimer(holdSeconds,
            p01 =>
            {
                if (IsRunStale()) return;

                int secLeft = Mathf.CeilToInt(p01 * holdSeconds);
                leftUI?.ShowHoldingCountdown(secLeft);

                if (secLeft != _lastHoldSecLeft)
                {
                    _lastHoldSecLeft = secLeft;
                    if (secLeft > 0)
                        AudioManager.SFX(SfxId.TimerTick);
                }
            },
            () =>
            {
                if (IsRunStale()) return;
                OnHoldCompleted(isBoss);
            });
    }

    private void OnHoldCompleted(bool isBoss)
    {
        leftUI?.ClearHoldingCountdown();

        AudioManager.SFX(SfxId.TimerSuccess);

        leftUI?.PlayRoutineIdlePose(1);
        leftUI?.SetInstruction("เก่งมาก เอามือลงได้ เตรียมทำท่าต่อไปนะ");

        hud?.ShowPerfectOnly(postWinPoseDelaySeconds);
        AudioManager.SFX(SfxId.ComplimentVoice);

        if (!isBoss)
        {
            if (rightVideo != null)
            {
                rightVideo.SetRoutineSet(GetRoutineSetForRound(_routineIndex));
                rightVideo.Play(RightVideoController.VideoState.WinPose, true);
            }

            _runId++;
            _activeRunId = _runId;
            int runId = _activeRunId;
            int completedRoutineRound = _routineIndex;

            _stateSequenceRoutine = StartCoroutine(RoutinePostSuccessSequence(runId, completedRoutineRound));
        }
        else
        {
            _bossProgress++;
            hud?.SetBossPower(_bossProgress);

            if (_bossProgress >= (config != null ? config.bossCount : 8))
            {
                EnterState(State.Victory);
                return;
            }

            _bossIndex++;
            if (_bossIndex <= (config != null ? config.bossCount : 8))
                EnterState(State.Boss_Delay);
            else
                EnterState(State.Victory);
        }
    }

    private IEnumerator RoutinePostSuccessSequence(int runId, int completedRoutineRound)
    {
        float winPoseLength = GetClipLengthOrFallback(
            RightVideoController.VideoState.WinPose,
            postWinPoseDelaySeconds);

        yield return WaitRealtimePausable(winPoseLength, runId);
        if (IsRunStale(runId)) yield break;

        RightVideoController.VideoState[] transitions = GetTransitionsAfterRoutineRound(completedRoutineRound);
        if (transitions != null)
        {
            for (int i = 0; i < transitions.Length; i++)
            {
                if (IsRunStale(runId)) yield break;

                RightVideoController.VideoState transitionState = transitions[i];
                rightVideo?.Play(transitionState, true);

                float transitionLength = GetClipLengthOrFallback(
                    transitionState,
                    missingClipFallbackSeconds);

                yield return WaitRealtimePausable(transitionLength, runId);
                if (IsRunStale(runId)) yield break;
            }
        }

        _routineIndex++;
        int routineCount = (config != null) ? config.routineCount : 12;

        if (_routineIndex <= routineCount)
            EnterState(State.Routine_Delay);
        else
            EnterState(State.Boss_Intro);
    }

    // ========================= BOSS INTRO =========================

    private void StartBossIntro()
    {
        _runId++;
        _activeRunId = _runId;

        _bossProgress = 0;
        _bossIndex = 1;

        hud?.HideBossPower();
        rightVideo?.Play(RightVideoController.VideoState.BossIntro, true);
        leftUI?.SetInstruction("บอสมาแล้ว!");

        int runId = _activeRunId;
        _stateSequenceRoutine = StartCoroutine(BossIntroSequence(runId));
    }

    private IEnumerator BossIntroSequence(int runId)
    {
        float wait = GetClipLengthOrFallback(
            RightVideoController.VideoState.BossIntro,
            config != null ? config.bossIntroDelaySeconds : 1.5f);

        yield return WaitRealtimePausable(wait, runId);
        if (IsRunStale(runId)) yield break;

        hud?.ShowBossPower(_bossProgress);
        EnterState(State.Boss_Delay);
    }

    // ========================= VICTORY =========================

    private void StartVictory()
    {
        _runId++;
        _activeRunId = _runId;

        hud?.HideBossPower();
        rightVideo?.Play(RightVideoController.VideoState.KOBoss, true);

        // Easy: ไม่คิดคะแนน
        GameSessionResult.SetResult(0, 0, 0);

        int runId = _activeRunId;
        _stateSequenceRoutine = StartCoroutine(VictorySequence(runId));
    }

    private IEnumerator VictorySequence(int runId)
    {
        float wait = GetClipLengthOrFallback(
            RightVideoController.VideoState.KOBoss,
            config != null ? config.victoryDelaySeconds : 2f);

        yield return WaitRealtimePausable(wait, runId);
        if (IsRunStale(runId)) yield break;

        SceneManager.LoadScene(scoreEasySceneName);
    }

    // ========================= ROUTINE VISUAL FLOW =========================

    private RightVideoController.RoutineSet GetRoutineSetForRound(int round1Based)
    {
        if (round1Based <= 2) return RightVideoController.RoutineSet.Coin1;
        if (round1Based <= 4) return RightVideoController.RoutineSet.Monster1;
        if (round1Based <= 6) return RightVideoController.RoutineSet.Coin1;
        if (round1Based <= 8) return RightVideoController.RoutineSet.Coin2;
        if (round1Based <= 10) return RightVideoController.RoutineSet.Monster2;
        return RightVideoController.RoutineSet.Coin2;
    }

    private RightVideoController.VideoState[] GetTransitionsAfterRoutineRound(int round1Based)
    {
        switch (round1Based)
        {
            case 1:
                return new[] { RightVideoController.VideoState.TransitionToCoin1 };

            case 2:
                return new[] { RightVideoController.VideoState.TransitionToMonster1 };

            case 3:
                return new[] { RightVideoController.VideoState.TransitionToMonster1 };

            case 4:
                return new[] { RightVideoController.VideoState.TransitionToCoin1 };

            case 5:
                return new[] { RightVideoController.VideoState.TransitionToCoin1 };

            case 6:
                return new[]
                {
                    RightVideoController.VideoState.TransitionBG,
                    RightVideoController.VideoState.TransitionToCoin2
                };

            case 7:
                return new[] { RightVideoController.VideoState.TransitionToCoin2 };

            case 8:
                return new[] { RightVideoController.VideoState.TransitionToMonster2 };

            case 9:
                return new[] { RightVideoController.VideoState.TransitionToMonster2 };

            case 10:
                return new[] { RightVideoController.VideoState.TransitionToCoin2 };

            case 11:
                return new[] { RightVideoController.VideoState.TransitionToCoin2 };

            default:
                return null;
        }
    }

    private float GetClipLengthOrFallback(RightVideoController.VideoState state, float fallbackSeconds)
    {
        if (rightVideo == null)
            return Mathf.Max(missingClipFallbackSeconds, fallbackSeconds);

        float len = rightVideo.GetClipLength(state);
        if (len > 0f)
            return len;

        return Mathf.Max(missingClipFallbackSeconds, fallbackSeconds);
    }
}