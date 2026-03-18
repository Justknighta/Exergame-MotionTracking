using UnityEngine;

public class PoseDetectionBridge : MonoBehaviour
{
    [SerializeField] private GameplayController gameplay;
    [SerializeField] private PoseRuleBase currentRule;
    [SerializeField] private float confirmHoldSeconds = 0.4f;

    private float okTimer = 0f;
    private bool alreadySent = false;

    public void SetGameplay(GameplayController controller)
    {
        gameplay = controller;
    }

    public void SetRule(PoseRuleBase rule)
    {
        currentRule = rule;
        okTimer = 0f;
        alreadySent = false;

        if (currentRule != null)
            currentRule.OnSessionStart();

        Debug.Log("[PoseBridge] SetRule = " + (rule != null ? $"{rule.DisplayName} (ID {rule.PoseID})" : "NULL"));
    }

    public void ClearRule()
    {
        currentRule = null;
        okTimer = 0f;
        alreadySent = false;
    }

    private void Update()
    {
        if (gameplay == null || currentRule == null || alreadySent)
            return;

        bool valid;
        bool matched = currentRule.EvaluateThisFrame(out valid);

        if (!valid)
        {
            okTimer = 0f;
            return;
        }

        if (matched)
        {
            okTimer += Time.deltaTime;

            if (okTimer >= confirmHoldSeconds)
            {
                Debug.Log("[PoseBridge] matched -> TEST_Good()");
                gameplay.TEST_Good();
                alreadySent = true;
            }
        }
        else
        {
            okTimer = 0f;
        }
    }
}