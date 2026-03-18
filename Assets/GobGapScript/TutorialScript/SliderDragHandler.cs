using UnityEngine;
using UnityEngine.EventSystems;

public class SliderDragHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private TutorialVideoController videoController;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (videoController != null)
            videoController.OnSliderPointerDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (videoController != null)
            videoController.OnSliderPointerUp();
    }
}