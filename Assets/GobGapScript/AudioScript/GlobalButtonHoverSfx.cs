using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GlobalButtonHoverSfx : MonoBehaviour
{

    [Header("Exclude By Layer")]
    [SerializeField] private LayerMask excludeLayers;

    [SerializeField] private SfxId hoverSfx = SfxId.UiHover;

    [Header("Optional: If button has its own UIHoverSfx, don't double-play")]
    [SerializeField] private bool skipIfButtonHasLocalHover = true;

    private readonly List<RaycastResult> _results = new List<RaycastResult>();
    private PointerEventData _ped;

    private Button _currentHoveredButton;

    private void Awake()
    {
        if (EventSystem.current == null)
            Debug.LogWarning("[GlobalButtonHoverSfx] No EventSystem in scene.");

        _ped = new PointerEventData(EventSystem.current);
    }

    private void Update()
    {
        if (EventSystem.current == null)
            return;

        _ped.position = Input.mousePosition;
        _results.Clear();
        EventSystem.current.RaycastAll(_ped, _results);

        Button hovered = null;

        for (int i = 0; i < _results.Count; i++)
        {
            var go = _results[i].gameObject;
            if (go == null) continue;

            var btn = go.GetComponentInParent<Button>();
            if (btn == null) continue;
            if (!btn.interactable) continue;

            hovered = btn;
            break;
        }

        if (hovered != _currentHoveredButton)
        {
            if (hovered != null)
            {
                if (!(skipIfButtonHasLocalHover && hovered.GetComponent<UIHoverSfx>() != null))
                {
                    if (!IsInExcludedLayer(hovered.gameObject))
                        {
                            AudioManager.SFX(hoverSfx);
                        }
                }
            }

            _currentHoveredButton = hovered;
        }
    }

    private bool IsInExcludedLayer(GameObject go)
    {
        Transform t = go.transform;

        while (t != null)
        {
            if ((excludeLayers.value & (1 << t.gameObject.layer)) != 0)
                return true;

            t = t.parent;
        }

        return false;
    }
}