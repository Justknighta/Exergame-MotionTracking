using UnityEngine;

public class PoseLibraryDebug : MonoBehaviour
{
    [SerializeField] private PoseLibrary library;
    [SerializeField] private int testID = 16;

    private void Start()
    {
        if (library == null)
        {
            Debug.LogWarning("PoseLibrary is null");
            return;
        }

        var rule = library.GetByID(testID);

        if (rule != null)
            Debug.Log("Found Pose: " + rule.DisplayName + " (" + rule.PoseName + ")");
        else
            Debug.LogWarning("Pose ID not found: " + testID);
    }
}