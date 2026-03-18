using UnityEngine;

public class PoseLibrary : MonoBehaviour
{
    [SerializeField] private PoseRuleBase[] allRules;

    public PoseRuleBase GetByID(int id)
    {
        if (allRules == null) return null;

        foreach (var rule in allRules)
        {
            if (rule != null && rule.PoseID == id)
                return rule;
        }

        return null;
    }
}