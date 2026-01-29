using UnityEngine;

public class WebcamProbe : MonoBehaviour
{
    WebCamTexture _tex;

    void Start()
    {
        Debug.Log("=== WebCam devices ===");
        foreach (var d in WebCamTexture.devices)
            Debug.Log($"Device: {d.name} isFront:{d.isFrontFacing}");

        if (WebCamTexture.devices.Length == 0)
        {
            Debug.LogError("NO WEBCAM DEVICE FOUND by Unity");
            return;
        }

        // ลองใช้ตัวแรกก่อน (ถ้ามีหลายตัว เดี๋ยวเราจะสลับได้)
        _tex = new WebCamTexture(WebCamTexture.devices[0].name, 640, 480, 30);
        _tex.Play();
        Debug.Log("Play() called. isPlaying=" + _tex.isPlaying);
    }

    void Update()
    {
        if (_tex == null) return;
        Debug.Log($"isPlaying={_tex.isPlaying} didUpdate={_tex.didUpdateThisFrame} size={_tex.width}x{_tex.height}");
    }

    void OnDestroy()
    {
        if (_tex != null) _tex.Stop();
    }
}
