using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GlobalButtonOnlySfx : MonoBehaviour
{
    [Header("Exclude By Layer")]
    [SerializeField] private LayerMask excludeLayers;

    [SerializeField] private SfxId clickSfx = SfxId.UiClick;

    [Header("Optional: If a button already has UIButtonSfx, don't double-play")]
    [SerializeField] private bool skipIfButtonHasUIButtonSfx = true;

    private readonly List<RaycastResult> _results = new List<RaycastResult>();
    private PointerEventData _ped;

    private void Awake()
    {
        if (EventSystem.current == null)
            Debug.LogWarning("[GlobalButtonOnlySfx] No EventSystem in scene. UI click SFX won't work.");

        _ped = new PointerEventData(EventSystem.current);
    }

    private void Update()
    {
        // Mouse click (Desktop)
        if (Input.GetMouseButtonDown(0))
        {
            TryPlayForPointer(Input.mousePosition);
        }

        // Touch (Mobile) - optional, safe to keep
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TryPlayForPointer(Input.GetTouch(0).position);
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

    private void TryPlayForPointer(Vector2 screenPos)
    {
        if (EventSystem.current == null) return;

        _ped.position = screenPos;
        _results.Clear();
        EventSystem.current.RaycastAll(_ped, _results);

        if (_results.Count == 0) return;

        // Find first raycast hit that is a Button (or under a Button)
        for (int i = 0; i < _results.Count; i++)
        {
            var go = _results[i].gameObject;
            if (go == null) continue;

            var btn = go.GetComponentInParent<Button>();
            if (btn == null) continue;

            if (!btn.interactable) return; // ไม่เล่นเสียงถ้าปุ่มกดไม่ได้

            if (skipIfButtonHasUIButtonSfx && btn.GetComponent<UIButtonSfx>() != null)
                return; // กันเสียงซ้ำ ถ้าปุ่มนั้นมี UIButtonSfx อยู่แล้ว

            if (!IsInExcludedLayer(btn.gameObject))
            {
                bool excluded = IsInExcludedLayer(btn.gameObject);
        Debug.Log($"[UI SFX] Click {btn.name} | btnLayer={LayerMask.LayerToName(btn.gameObject.layer)} | excluded={excluded} | mask={excludeLayers.value}");
                AudioManager.SFX(clickSfx);
            }
            return;
        }
    }
}