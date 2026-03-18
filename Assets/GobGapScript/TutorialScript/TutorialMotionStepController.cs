using UnityEngine;
using TMPro;

public class TutorialMotionStepController : MonoBehaviour
{
    private enum MotionTutorialState
    {
        CameraCheck,
        RaiseHandConfirm
    }

    [Header("State Objects")]
    [SerializeField] private GameObject cameraCheckStateObject;   // หน้า 4.1
    [SerializeField] private GameObject raiseHandStateObject;     // หน้า 4.2

    [Header("Texts")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text countdownText;

    [TextArea]
    [SerializeField] private string cameraCheckMessage =
        "ขยับตัวให้พอดีกับกรอบเส้นประ เพื<voffset=0.3em>่</voffset>อให้เราจับท่าทางคุณได้แม่นยำ";

    [TextArea]
    [SerializeField] private string raiseHandMessage =
        "ชูมือขวาค้างไว้ 5 วินาที เพื<voffset=0.3em>่</voffset>อเริ<voffset=0.3em>่</voffset>มเกม";

    [Header("Countdown")]
    [SerializeField] private float requiredHoldSeconds = 5f;

    [Header("Scene Transition")]
    [SerializeField] private TutorialSceneController sceneController;

    private MotionTutorialState currentState = MotionTutorialState.CameraCheck;
    private float holdTimer = 0f;
    private bool isRightHandRaised = false;
    private bool hasStartedGame = false;

    private void Start()
    {
        ResetStep();
    }

    private void Update()
    {
        if (hasStartedGame)
            return;

        if (currentState != MotionTutorialState.RaiseHandConfirm)
            return;

        if (isRightHandRaised)
        {
            holdTimer += Time.deltaTime;
            UpdateCountdownText();

            if (holdTimer >= requiredHoldSeconds)
            {
                StartGameplay();
            }
        }
        else
        {
            if (holdTimer > 0f)
            {
                holdTimer = 0f;
                UpdateCountdownText();
            }
        }
    }

    public void ResetStep()
    {
        currentState = MotionTutorialState.CameraCheck;
        holdTimer = 0f;
        isRightHandRaised = false;
        hasStartedGame = false;
        ApplyVisualState();
    }

    // เรียกเมื่อ motion track ตรวจว่า "ผู้เล่นอยู่ใน area / อยู่ในกรอบ"
    public void SetPlayerInCameraArea(bool inArea)
    {
        if (hasStartedGame)
            return;

        if (inArea)
        {
            currentState = MotionTutorialState.RaiseHandConfirm;
        }
        else
        {
            currentState = MotionTutorialState.CameraCheck;
            holdTimer = 0f;
            isRightHandRaised = false;
        }

        ApplyVisualState();
    }

    // เรียกเมื่อ motion track ตรวจว่า "ยกมือขวาถูกต้อง"
    public void SetRightHandRaised(bool raised)
    {
        if (hasStartedGame)
            return;

        if (currentState != MotionTutorialState.RaiseHandConfirm)
            return;

        isRightHandRaised = raised;

        if (!raised)
        {
            holdTimer = 0f;
            UpdateCountdownText();
        }
    }

    private void ApplyVisualState()
    {
        if (cameraCheckStateObject != null)
            cameraCheckStateObject.SetActive(currentState == MotionTutorialState.CameraCheck);

        if (raiseHandStateObject != null)
            raiseHandStateObject.SetActive(currentState == MotionTutorialState.RaiseHandConfirm);

        if (instructionText != null)
        {
            instructionText.text = currentState == MotionTutorialState.CameraCheck
                ? cameraCheckMessage
                : raiseHandMessage;
        }

        UpdateCountdownText();
    }

    private void UpdateCountdownText()
    {
        if (countdownText == null)
            return;

        if (currentState != MotionTutorialState.RaiseHandConfirm)
        {
            countdownText.text = "";
            return;
        }

        if (!isRightHandRaised)
        {
            countdownText.text = "";
            return;
        }

        float remaining = Mathf.Max(0f, requiredHoldSeconds - holdTimer);
        countdownText.text = $"{Mathf.CeilToInt(remaining)}";
    }

    private void StartGameplay()
    {
        if (hasStartedGame)
            return;

        hasStartedGame = true;

        if (sceneController != null)
            sceneController.ContinueToGameplay();
        else
            Debug.LogWarning("TutorialMotionStepController: sceneController is null.");
    }

    // -------------------------
    // Debug buttons สำหรับ mock test
    // -------------------------

    public void DebugSetInAreaTrue()
    {
        SetPlayerInCameraArea(true);
    }

    public void DebugSetInAreaFalse()
    {
        SetPlayerInCameraArea(false);
    }

    public void DebugRightHandDown()
    {
        SetRightHandRaised(false);
    }

    public void DebugRightHandUp()
    {
        SetRightHandRaised(true);
    }
}