using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUIBinder : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Tooltip("เมื่อ popup เปิด ให้ reapply ค่าไปที่ mixer อีกครั้งเพื่อกันค่าไม่ sync")]
    [SerializeField] private bool forceReapplyOnEnable = true;

    private void OnEnable()
    {
        if (AudioManager.Instance == null)
            return;

        // 1) set slider ให้ตรงกับค่าปัจจุบัน (ไม่ยิง event)
        musicSlider.SetValueWithoutNotify(AudioManager.Instance.GetMusic01());
        sfxSlider.SetValueWithoutNotify(AudioManager.Instance.GetSfx01());

        // 2) บังคับ apply ค่าเดิมไปที่ mixer (กรณีเสียงเพี้ยนตอนเริ่ม)
        if (forceReapplyOnEnable)
            AudioManager.Instance.ReapplyToMixer();

        // 3) ค่อย subscribe event หลังจาก set ค่าแล้ว
        musicSlider.onValueChanged.AddListener(OnMusicChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);
    }

    private void OnDisable()
    {
        musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
    }

    private void OnMusicChanged(float value)
    {
        AudioManager.Instance?.SetMusic01(value);
    }

    private void OnSfxChanged(float value)
    {
        AudioManager.Instance?.SetSfx01(value);
    }
}