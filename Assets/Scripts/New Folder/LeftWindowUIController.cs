using UnityEngine;

public class LeftWindowUIController : MonoBehaviour
{
    public void FreezePose() { }
    public void UnfreezePose() { }
    public void ClearHoldingCountdown() { }

    public void PlayRoutineIdlePose(int index) { }
    public void PlayRoutinePose(int index) { }
    public void PlayBossPose(int index) { }

    public void SetInstruction(string text) { }
    public void ShowHoldingCountdown(int secLeft) { }

    public void SetProgress01(float value) { }
}