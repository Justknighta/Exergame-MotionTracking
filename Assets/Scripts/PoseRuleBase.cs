using UnityEngine;

public abstract class PoseRuleBase : MonoBehaviour
{
    [Header("Pose Identity")]
    [SerializeField] private int poseID;
    [SerializeField] private string displayName;

    public int PoseID => poseID;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? PoseName : displayName;

    public virtual string PoseName => gameObject.name;
    public virtual float DurationSec => 60f;
    public virtual int PassBonusScore => 100;

    public virtual void OnSessionStart() { }
    public virtual void OnSessionEnd() { }

    public abstract bool EvaluateThisFrame(out bool valid);

    public virtual string GetDebugText() => "";
}