using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class TutorialVideoController : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Button playPauseButton;
    [SerializeField] private GameObject playIcon;
    [SerializeField] private GameObject pauseIcon;

    private bool isDraggingSlider = false;
    private bool isPrepared = false;

    private void Awake()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.prepareCompleted += OnVideoPrepared;
        }

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = 0f;
        }

        UpdatePlayPauseIcon();
    }

    private void Update()
    {
        if (videoPlayer == null || progressSlider == null)
            return;

        if (!isPrepared)
            return;

        if (videoPlayer.isPlaying && !isDraggingSlider && videoPlayer.length > 0)
        {
            progressSlider.value = (float)(videoPlayer.time / videoPlayer.length);
        }
    }

    public void PrepareVideoPage()
    {
        if (videoPlayer == null)
            return;

        isPrepared = false;
        videoPlayer.Prepare();
        UpdatePlayPauseIcon();
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        isPrepared = true;
        progressSlider.value = 0f;

        // จะ autoplay เลยก็ได้
        videoPlayer.Play();
        UpdatePlayPauseIcon();
    }

    public void TogglePlayPause()
    {
        if (videoPlayer == null || !isPrepared)
            return;

        if (videoPlayer.isPlaying)
            videoPlayer.Pause();
        else
            videoPlayer.Play();

        UpdatePlayPauseIcon();
    }

    public void OnSliderPointerDown()
    {
        isDraggingSlider = true;
    }

    public void OnSliderPointerUp()
    {
        isDraggingSlider = false;
        SeekToSliderValue();
    }

    public void OnSliderValueChanged()
    {
        if (isDraggingSlider)
        {
            SeekToSliderValue();
        }
    }

    private void SeekToSliderValue()
    {
        if (videoPlayer == null || !isPrepared || progressSlider == null)
            return;

        if (videoPlayer.length <= 0)
            return;

        double targetTime = progressSlider.value * videoPlayer.length;
        videoPlayer.time = targetTime;
    }

    public void StopAndReset()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.Stop();
        isPrepared = false;

        if (progressSlider != null)
            progressSlider.value = 0f;

        UpdatePlayPauseIcon();
    }

    private void UpdatePlayPauseIcon()
    {
        bool isPlaying = videoPlayer != null && videoPlayer.isPlaying;

        if (playIcon != null)
            playIcon.SetActive(!isPlaying);

        if (pauseIcon != null)
            pauseIcon.SetActive(isPlaying);
    }
}