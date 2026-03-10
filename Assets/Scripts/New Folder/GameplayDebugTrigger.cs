using UnityEngine;

public class GameplayDebugTrigger : MonoBehaviour
{
    [SerializeField] private GameplayController gameplay;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("[DebugTrigger] G pressed");

            if (gameplay != null)
            {
                Debug.Log("[DebugTrigger] gameplay found, calling TEST_Good()");
                gameplay.TEST_Good();
            }
            else
            {
                Debug.LogWarning("[DebugTrigger] gameplay is NULL");
            }
        }
    }
}