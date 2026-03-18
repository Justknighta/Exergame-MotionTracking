using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class TimerService : MonoBehaviour
{
    private float _duration;
    private float _remaining;
    private bool _running;
    private bool _paused;

    private Action<float> _onTick01; // 0..1 progress
    private Action _onCompleted;

    public bool IsRunning => _running;
    public bool IsPaused => _paused;
    public float Remaining => _remaining;
    public float Duration => _duration;

    public void StartTimer(float durationSeconds, Action<float> onTick01, Action onCompleted)
    {
        if (durationSeconds <= 0f) durationSeconds = 0.01f;

        _duration = durationSeconds;
        _remaining = durationSeconds;
        _onTick01 = onTick01;
        _onCompleted = onCompleted;

        _running = true;
        _paused = false;

        // initial tick = 0 progress (or 1 depending on your preference)
        _onTick01?.Invoke(0f);
    }

    public void Cancel()
    {
        _running = false;
        _paused = false;
        _onTick01 = null;
        _onCompleted = null;
        _duration = 0f;
        _remaining = 0f;
    }

    public void Pause()
    {
        if (!_running) return;
        _paused = true;
    }

    public void Resume()
    {
        if (!_running) return;
        _paused = false;
    }

    private void Update()
    {
        if (!_running || _paused) return;

        _remaining -= Time.unscaledDeltaTime; // ใช้ unscaled เพื่อให้ pause แบบ logic ได้
        if (_remaining < 0f) _remaining = 0f;

        float progress01 = (_remaining / _duration); // 0 -> 1
        _onTick01?.Invoke(progress01);

        if (_remaining <= 0f)
        {
            // complete once
            _running = false;
            var completed = _onCompleted;
            _onCompleted = null;
            completed?.Invoke();
        }
    }
}

