using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayController : MonoBehaviour
{
    private enum State
    {
        WarmupIntro,
        Routine_Detecting,
        Routine_TimesUpWaitingGood,
        Routine_Holding,
        Boss_Intro,
        Boss_Detecting,
        Boss_TimesUpWaitingGood,
        Boss_Holding,
        Victory
    }

    private enum Grade
    {
        Perfect,
        Good
    }
    
    [Header("Pose Runtime")]
    [SerializeField] private PoseLibrary poseLibrary;
    [SerializeField] private PoseDetectionBridge poseBridge;

    [Header("Scene Refs")]
    [SerializeField] private LeftWindowUIController leftUI;
    [SerializeField] private RightVideoController rightVideo;
    [SerializeField] private HUDController hud;
    [SerializeField] private TimerService timer;

    [Header("Config")]
    [SerializeField] private PoseSequenceConfig config;

    [Header("Scene Names")]
    [SerializeField] private string gameOverSceneName = "GameOver";
    [SerializeField] private string scoreSceneName = "Score";
    [SerializeField] private string nextSceneName = "Home";

    [Header("Delays / Fallbacks")]
    [SerializeField] private float postWinPoseDelaySeconds = 1.0f;
    [SerializeField] private float missingClipFallbackSeconds = 0.05f;

    [Header("Warmup UI")]
    [SerializeField] private GameObject warmupOverlayRoot;
    [SerializeField] private TMPro.TMP_Text warmupCountdownText;
    [SerializeField] private float warmupReadySeconds = 3f;

    private int _hearts = 5;
    private int _score = 0;

    private int _perfectCount = 0;
    private int _goodCount = 0;

    private int _routineIndex = 1;
    private int _bossIndex = 1;

    // Boss progress: 0 -> config.bossCount
    private int _bossProgress = 0;

    private State _state;
    private Grade _pendingGrade;
    private bool _paused;

    private int _runId = 0;
    private int _activeRunId = 0;

    private bool _lockProgress = false;

    private Coroutine _warmupRoutine;
    private Coroutine _stateSequenceRoutine;

    private int _lastHoldSecLeft = -1;

    private bool IsMediumMode()
    {
        return GameContext.SelectedMode != null
            && GameContext.SelectedMode.ToString().Equals("Medium", StringComparison.OrdinalIgnoreCase);
    }

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
        Debug.Log($"[Gameplay] Level = {GameContext.SelectedLevel} | Mode = {GameContext.SelectedMode}");

        GameSessionResult.Clear();

        hud?.SetHeartsVisible(!IsMediumMode());
        hud?.SetHearts(_hearts);
        hud?.SetScore(_score);

        EnterState(State.WarmupIntro);
    }

    public void TEST_Good() => OnGoodReceived();

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

    private void EnterState(State newState)
    {
        ExitState(_state);
        _state = newState;

        switch (_state)
        {
            case State.WarmupIntro:
                StartWarmup();
                break;

            case State.Routine_Detecting:
                StartDetecting(false);
                break;

            case State.Routine_TimesUpWaitingGood:
                StartTimesUpWaitingGood(false);
                break;

            case State.Routine_Holding:
                StartHolding(false);
                break;

            case State.Boss_Intro:
                StartBossIntro();
                break;

            case State.Boss_Detecting:
                StartDetecting(true);
                break;

            case State.Boss_TimesUpWaitingGood:
                StartTimesUpWaitingGood(true);
                break;

            case State.Boss_Holding:
                StartHolding(true);
                break;

            case State.Victory:
                StartVictory();
                break;
        }
    }

    private void ExitState(State state)
    {
        poseBridge?.ClearRule();
        timer?.Cancel();

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

        leftUI?.ClearHoldingCountdown();
    }

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

        float ready = (config != null) ? config.warmupReadySeconds : warmupReadySeconds;

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

        if (warmupOverlayRoot != null)
            warmupOverlayRoot.SetActive(false);

        if (warmupCountdownText != null)
            warmupCountdownText.text = "";

        EnterState(State.Routine_Detecting);
    }

    private bool IsRunStale(int runId) => runId != _activeRunId;
    private bool IsRunStale() => _activeRunId != _runId;

    // ========================= DETECT =========================

    private void StartDetecting(bool isBoss)
    {
        _runId++;
        _activeRunId = _runId;

        leftUI?.UnfreezePose();

        _lockProgress = false;
        leftUI?.SetProgress01(1f);

        int index = isBoss ? _bossIndex : _routineIndex;

        float detectSeconds = 10f;
        if (config != null)
            detectSeconds = isBoss ? config.bossDetectSeconds : config.routineDetectSeconds;

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

        timer.StartTimer(detectSeconds,
            p01 =>
            {
                if (IsRunStale() || _lockProgress) return;
                leftUI?.SetProgress01(p01);
            },
            () =>
            {
                if (IsRunStale()) return;
                OnDetectTimerCompleted(isBoss);
            });
        int poseId = isBoss
        ? (config != null ? config.GetBossPoseID(index) : -1)
        : (config != null ? config.GetRoutinePoseID(index) : -1);

    PoseRuleBase rule = null;
    if (poseLibrary != null && poseId >= 0)
        rule = poseLibrary.GetByID(poseId);

    if (poseBridge != null)
    {
        poseBridge.SetGameplay(this);
        poseBridge.SetRule(rule);
    }

    Debug.Log($"[Gameplay] Detect {(isBoss ? "Boss" : "Routine")} #{index} -> PoseID={poseId} -> Rule={(rule != null ? rule.DisplayName : "NULL")}");
    }

    private void OnDetectTimerCompleted(bool isBoss)
    {
        ApplyHeartPenaltyOrGameOver();
        if (!IsMediumMode() && _hearts <= 0) return;

        EnterState(isBoss ? State.Boss_TimesUpWaitingGood : State.Routine_TimesUpWaitingGood);
    }

    // ========================= TIME UP =========================

    private void StartTimesUpWaitingGood(bool isBoss)
    {
        _runId++;
        _activeRunId = _runId;

        if (isBoss)
        {
            rightVideo?.Play(RightVideoController.VideoState.LoseBossLoop, true);
        }
        else
        {
            if (rightVideo != null)
            {
                rightVideo.SetRoutineSet(GetRoutineSetForRound(_routineIndex));
                rightVideo.Play(RightVideoController.VideoState.LoseLoop, true);
            }
        }

        _lockProgress = true;
        leftUI?.SetProgress01(0f);

        _pendingGrade = Grade.Good;
    }

    // ========================= HOLDING =========================

    private void StartHolding(bool isBoss)
    {
        _runId++;
        _activeRunId = _runId;

        float holdSeconds = config != null ? config.holdSeconds : 10f;

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
                if (!IsRunStale()) OnHoldCompleted(isBoss);
            });
    }

    private void OnHoldCompleted(bool isBoss)
    {
        leftUI?.ClearHoldingCountdown();

        AudioManager.SFX(SfxId.TimerSuccess);

        leftUI?.PlayRoutineIdlePose(1);
        leftUI?.SetInstruction("เก่งมาก เอามือลงได้ เตรียมทำท่าต่อไปนะ");

        int dmg = _pendingGrade == Grade.Perfect
            ? config.perfectDamage
            : config.goodDamage;

        if (_pendingGrade == Grade.Perfect) _perfectCount++;
        else _goodCount++;

        _score += dmg;
        hud?.SetScore(_score);
        hud?.ShowResultFeedback(_pendingGrade, dmg, postWinPoseDelaySeconds);

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
                EnterState(State.Boss_Detecting);
            else
                EnterState(State.Victory);
        }
    }

    private IEnumerator RoutinePostSuccessSequence(int runId, int completedRoutineRound)
    {
        float winPoseLength = GetClipLengthOrFallback(RightVideoController.VideoState.WinPose, postWinPoseDelaySeconds);
        yield return WaitRealtimePausable(winPoseLength, runId);
        if (IsRunStale(runId)) yield break;

        RightVideoController.VideoState[] transitions = GetTransitionsAfterRoutineRound(completedRoutineRound);
        if (transitions != null)
        {
            for (int i = 0; i < transitions.Length; i++)
            {
                if (IsRunStale(runId)) yield break;

                var transitionState = transitions[i];
                rightVideo?.Play(transitionState, true);

                float transitionLength = GetClipLengthOrFallback(transitionState, missingClipFallbackSeconds);
                yield return WaitRealtimePausable(transitionLength, runId);
            }
        }

        if (IsRunStale(runId)) yield break;

        _routineIndex++;
        if (_routineIndex <= (config != null ? config.routineCount : 12))
            EnterState(State.Routine_Detecting);
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

        hud?.ShowBossPower(_bossProgress); // ค่อยเปิดหลัง intro จบ
        EnterState(State.Boss_Detecting);
    }

    // ========================= VICTORY =========================

    private void StartVictory()
    {
        _runId++;
        _activeRunId = _runId;

        hud?.HideBossPower();
        rightVideo?.Play(RightVideoController.VideoState.KOBoss, true);

        GameSessionResult.SetResult(_score, _perfectCount, _goodCount);

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

        SceneManager.LoadScene(scoreSceneName);
    }

    // ========================= GOOD EVENT =========================

    private void OnGoodReceived()
    {
        if (_paused) return;

        switch (_state)
        {
            case State.Routine_Detecting:
                _lockProgress = true;
                _pendingGrade = Grade.Perfect;
                AudioManager.SFX(SfxId.PoseSuccess);
                EnterState(State.Routine_Holding);
                break;

            case State.Routine_TimesUpWaitingGood:
                _pendingGrade = Grade.Good;
                EnterState(State.Routine_Holding);
                break;

            case State.Boss_Detecting:
                _lockProgress = true;
                _pendingGrade = Grade.Perfect;
                AudioManager.SFX(SfxId.PoseSuccess);
                EnterState(State.Boss_Holding);
                break;

            case State.Boss_TimesUpWaitingGood:
                _pendingGrade = Grade.Good;
                EnterState(State.Boss_Holding);
                break;
        }
    }

    // ========================= HEART / GAMEOVER =========================

    private void ApplyHeartPenaltyOrGameOver()
    {
        if (IsMediumMode())
            return;

        _hearts--;
        hud?.SetHearts(_hearts);

        AudioManager.SFX(SfxId.HeartPop);

        if (_hearts <= 0)
        {
            timer?.Cancel();
            SceneManager.LoadScene(gameOverSceneName);
        }
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