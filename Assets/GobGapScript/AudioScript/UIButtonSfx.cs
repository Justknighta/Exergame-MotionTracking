using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSfx : MonoBehaviour
{
    [SerializeField] private SfxId sfxId = SfxId.UiClick;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (_button != null)
            _button.onClick.AddListener(PlaySfx);
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(PlaySfx);
    }

    private void PlaySfx()
    {
        AudioManager.SFX(sfxId);
    }
}