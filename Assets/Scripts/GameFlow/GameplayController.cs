using System;
using System.Collections;
using System.Collections.Generic;
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

    [Header("Scene Refs")]
    [SerializeField] private LeftWindowUIController leftUI;
    [SerializeField] private RightVideoController rightVideo;
    [SerializeField] private HUDController hud;
    [SerializeField] private TimerService timer;

    [Header("Config")]
    [SerializeField] private PoseSequenceConfig config;

    [Header("Scene Names")]
    [SerializeField] private string gameOverSceneName = "GameOver";
    [SerializeField] private string scoreSceneName = "Score"; // ชนะแล้วไป Score
    [SerializeField] private string nextSceneName = "Home";   // เผื่อใช้ภายหลัง

    [Header("Delays")]
    [SerializeField] private float postWinPoseDelaySeconds = 1.0f;

    [Header("Warmup UI")]
    [SerializeField] private GameObject warmupOverlayRoot;
    [SerializeField] private TMPro.TMP_Text warmupCountdownText;
    [SerializeField] private float warmupReadySeconds = 3f; // fallback

    private int _hearts = 5;
    private int _score = 0;

    // เก็บจำนวน Perfect/Good สำหรับส่งไป Score
    private int _perfectCount = 0;
    private int _goodCount = 0;

    private int _routineIndex = 1;
    private int _bossIndex = 1;

    // HP บอส (เริ่ม = จำนวนท่า bossCount)
    private int _bossHP = 0;

    private State _state;
    private Grade _pendingGrade;
    private bool _paused;

    private int _runId = 0;
    private int _activeRunId = 0;

    // ล็อค progress กันโดน timer tick ทับ (Perfect ค้าง / TimeUp ค้าง 0)
    private bool _lockProgress = false;

    // Warmup coroutine
    private Coroutine _warmupRoutine;

    // ✅ For Holding countdown tick (avoid duplicate)
    private int _lastHoldSecLeft = -1;

    // ========================= MODE HELPERS =========================
    // Medium: ไม่ลบหัวใจ + ไม่มี GameOver
    private bool IsMediumMode()
    {
        // ✅ ไม่ผูกกับ enum ชื่อ GameMode อีกต่อไป
        return GameContext.SelectedMode != null
            && GameContext.SelectedMode.ToString().Equals("Medium", System.StringComparison.OrdinalIgnoreCase);
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

        // กันข้อมูลรอบเก่าค้าง
        GameSessionResult.Clear();

        // ✅ NEW: Medium ซ่อนหัวใจ
        hud?.SetHeartsVisible(!IsMediumMode());

        hud?.SetHearts(_hearts);
        hud?.SetScore(_score);
        EnterState(State.WarmupIntro);
    }

    // ---------- Public buttons ----------
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
        timer?.Cancel();

        // ถ้าออกจาก warmup ระหว่างทาง ให้หยุด coroutine และปิด overlay กันค้าง
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
        // เปิด Overlay
        if (warmupOverlayRoot != null) warmupOverlayRoot.SetActive(true);

        // R เล่น idle
        rightVideo?.Play(RightVideoController.VideoState.IdleLoop, true);

        // L เล่น idle pose
        leftUI?.UnfreezePose();
        leftUI?.PlayRoutineIdlePose(1);

        // 1) Are You Ready?
        if (warmupCountdownText != null)
            warmupCountdownText.text = "Are You Ready?";

        float ready = (config != null) ? config.warmupReadySeconds : warmupReadySeconds;

        yield return WaitRealtimePausable(ready, runId);
        if (IsRunStale(runId)) yield break;

        // 2) 3 2 1
        for (int i = 3; i >= 1; i--)
        {
            if (warmupCountdownText != null)
                warmupCountdownText.text = i.ToString();

            yield return WaitRealtimePausable(1f, runId);
            if (IsRunStale(runId)) yield break;
        }

        // 3) Go!
        if (warmupCountdownText != null)
            warmupCountdownText.text = "Go!";

        yield return WaitRealtimePausable(0.5f, runId);
        if (IsRunStale(runId)) yield break;

        // ปิด Overlay
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

        // Countdown bar: เริ่มเต็มแล้วค่อยๆ หด
        _lockProgress = false;
        leftUI?.SetProgress01(1f);

        int index = isBoss ? _bossIndex : _routineIndex;

        float detectSeconds = 10f;
        if (config != null)
            detectSeconds = isBoss ? config.bossDetectSeconds : config.routineDetectSeconds;

        if (isBoss)
        {
            leftUI?.PlayBossPose(index);
            leftUI?.SetInstruction(config.GetBossInstruction(index));
            rightVideo?.Play(RightVideoController.VideoState.IdleBossLoop);
        }
        else
        {
            leftUI?.PlayRoutinePose(index);
            leftUI?.SetInstruction(config.GetRoutineInstruction(index));

            rightVideo?.SetRoutineVariantByIndex(index);
            rightVideo?.Play(RightVideoController.VideoState.IdleLoop);
        }

        timer.StartTimer(detectSeconds,
            p01 =>
            {
                if (IsRunStale() || _lockProgress) return;
                leftUI?.SetProgress01(p01); // p01 = remaining01 (1->0)
            },
            () =>
            {
                if (IsRunStale()) return;
                OnDetectTimerCompleted(isBoss);
            });
    }

    private void OnDetectTimerCompleted(bool isBoss)
    {
        ApplyHeartPenaltyOrGameOver(); // Medium จะไม่โดนลบหัวใจแล้ว
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
            rightVideo?.SetRoutineVariantByIndex(_routineIndex);
            rightVideo?.Play(RightVideoController.VideoState.LoseLoop, true);
        }

        // Time’s up: แถบแดงต้องหมด และค้าง
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
        leftUI?.SetInstruction("ค้างไว้ 10 วินาทีนะ");

        if (isBoss)
        {
            rightVideo?.Play(RightVideoController.VideoState.PreWinBossPose, true);
        }
        else
        {
            rightVideo?.SetRoutineVariantByIndex(_routineIndex);
            rightVideo?.Play(RightVideoController.VideoState.PreWinPose, true);
        }

        // ✅ Reset tick state so it won't carry over from previous pose
        _lastHoldSecLeft = -1;

        timer.StartTimer(holdSeconds,
            p01 =>
            {
                if (IsRunStale()) return;

                int secLeft = Mathf.CeilToInt(p01 * holdSeconds); // 10..0
                leftUI?.ShowHoldingCountdown(secLeft);

                // ✅ Tick only when second changes
                if (secLeft != _lastHoldSecLeft)
                {
                    _lastHoldSecLeft = secLeft;

                    // เล่นตอน 10..1 (ไม่เล่นตอน 0)
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

        // ✅ Holding success sound
        AudioManager.SFX(SfxId.TimerSuccess);

        // กลับไป idle pose ฝั่งซ้าย
        leftUI?.PlayRoutineIdlePose(1);
        leftUI?.SetInstruction("เก่งมาก เอามือลงได้ เตรียมทำท่าต่อไปนะ");

        if (isBoss)
        {
            rightVideo?.Play(RightVideoController.VideoState.WinBossPose, true);
        }
        else
        {
            rightVideo?.SetRoutineVariantByIndex(_routineIndex);
            rightVideo?.Play(RightVideoController.VideoState.WinPose, true);
        }

        int dmg = _pendingGrade == Grade.Perfect
            ? config.perfectDamage
            : config.goodDamage;

        if (_pendingGrade == Grade.Perfect) _perfectCount++;
        else _goodCount++;

        _score += dmg;
        hud?.SetScore(_score);

        hud?.ShowResultFeedback(_pendingGrade, dmg, postWinPoseDelaySeconds);

        // ✅ Voice compliment follows UI feedback
        AudioManager.SFX(SfxId.ComplimentVoice);

        float delay = postWinPoseDelaySeconds;

        _runId++;
        _activeRunId = _runId;

        timer.StartTimer(delay, null, () =>
        {
            if (IsRunStale()) return;

            if (!isBoss)
            {
                _routineIndex++;
                if (_routineIndex <= config.routineCount)
                    EnterState(State.Routine_Detecting);
                else
                    EnterState(State.Boss_Intro);
            }
            else
            {
                _bossHP--;
                hud?.SetBossHP(_bossHP);

                if (_bossHP <= 0)
                {
                    EnterState(State.Victory);
                    return;
                }

                _bossIndex++;
                if (_bossIndex <= config.bossCount)
                    EnterState(State.Boss_Detecting);
                else
                    EnterState(State.Victory);
            }
        });
    }

    // ========================= BOSS INTRO =========================

    private void StartBossIntro()
    {
        _runId++;
        _activeRunId = _runId;

        _bossHP = (config != null) ? config.bossCount : 10;

        rightVideo?.Play(RightVideoController.VideoState.BossIntro, true);
        leftUI?.SetInstruction("บอสมาแล้ว!");

        timer.StartTimer(config.bossIntroDelaySeconds, null, () =>
        {
            if (!IsRunStale())
            {
                _bossIndex = 1;
                hud?.ShowBossHP(_bossHP);
                EnterState(State.Boss_Detecting);
            }
        });
    }

    // ========================= VICTORY =========================

    private void StartVictory()
    {
        _runId++;
        _activeRunId = _runId;

        hud?.HideBossHP();
        rightVideo?.Play(RightVideoController.VideoState.KOBoss, true);

        GameSessionResult.SetResult(_score, _perfectCount, _goodCount);

        timer.StartTimer(config.victoryDelaySeconds, null, () =>
        {
            if (!IsRunStale())
                SceneManager.LoadScene(scoreSceneName);
        });
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

                // ✅ Track success sound (Pose detected)
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

                // ✅ Track success sound (Pose detected)
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

        // ✅ Heart pop when losing a heart
        AudioManager.SFX(SfxId.HeartPop);

        if (_hearts <= 0)
        {
            timer?.Cancel();
            SceneManager.LoadScene(gameOverSceneName);
        }
    }
}