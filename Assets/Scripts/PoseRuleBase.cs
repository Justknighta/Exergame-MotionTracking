using UnityEngine;

public abstract class PoseRuleBase : MonoBehaviour
{
    // ✅ ค่าประจำท่า (ให้แต่ละท่า override ได้)
    public virtual string PoseName => gameObject.name;
    public virtual float DurationSec => 60f;
    public virtual int PassBonusScore => 100;

    public virtual void OnSessionStart() {}
    public virtual void OnSessionEnd() {}

    // return: correct?  | out valid: pose data valid?
    public abstract bool EvaluateThisFrame(out bool valid);

    public virtual string GetDebugText() => "";
}