using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverSfx : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private SfxId hoverSfx = SfxId.UiHover;

    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.SFX(hoverSfx);
    }
}