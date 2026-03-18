using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Exposed Parameter Names")]
    [SerializeField] private string musicParam = "MusicVol";
    [SerializeField] private string sfxParam = "SfxVol";

    [Header("BGM Source")]
    [SerializeField] private AudioSource bgmSource;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip coreTheme;
    [SerializeField] private AudioClip resultTheme;
    [SerializeField] private AudioClip gameOverTheme;

    [SerializeField] private AudioClip[] modeThemes;      // size = 5
    [SerializeField] private AudioClip[] gameplayThemes;  // size = 5

    [Header("SFX Source")]
    [SerializeField] private AudioSource sfxSource;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip uiClick;
    [SerializeField] private AudioClip uiHover;
    [SerializeField] private AudioClip timerTick;
    [SerializeField] private AudioClip timerSuccess;
    [SerializeField] private AudioClip poseSuccess;
    [SerializeField] private AudioClip heartPop;
    [SerializeField] private AudioClip leaderboardWow;

    [Header("Compliment Voice Clips (Random)")]
    [SerializeField] private AudioClip[] complimentVoices;

    [Header("SFX Options")]
    [SerializeField] private bool preventSameComplimentTwice = true;

    private int _lastComplimentIndex = -1;

    private AudioClip currentBgmClip;

    private const string PREF_MUSIC = "audio_music_01";
    private const string PREF_SFX = "audio_sfx_01";

    private float music01 = 0.5f;
    private float sfx01 = 0.5f;

    private const float MUTE_DB = -80f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        LoadPrefs();
        ApplyAll();
    }

    private void Start()
    {
        // กันกรณีมี object อื่น set mixer ทับหลัง Awake/OnEnable
        ApplyAll();
    }

    // =========================
    // PUBLIC API
    // =========================
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateBgmForScene(scene.name);
    }

    public void SetMusic01(float value)
    {
        music01 = Mathf.Clamp01(value);
        ApplyMusic();
        SavePrefs();
    }

    public void SetSfx01(float value)
    {
        sfx01 = Mathf.Clamp01(value);
        ApplySfx();
        SavePrefs();
    }

    public float GetMusic01() => music01;
    public float GetSfx01() => sfx01;

    // =========================
    // APPLY TO MIXER
    // =========================

    private void ApplyAll()
    {
        ApplyMusic();
        ApplySfx();
    }

    private void ApplyMusic()
    {
        float db = Convert01ToDb(music01);
        mainMixer.SetFloat(musicParam, db);
    }

    private void ApplySfx()
    {
        float db = Convert01ToDb(sfx01);
        mainMixer.SetFloat(sfxParam, db);
    }

    private float Convert01ToDb(float value)
    {
        if (value <= 0.0001f)
            return MUTE_DB;

        return Mathf.Log10(value) * 20f;
    }

    // =========================
    // PREFS
    // =========================

    private void LoadPrefs()
    {
        music01 = PlayerPrefs.GetFloat(PREF_MUSIC, 0.5f);
        sfx01 = PlayerPrefs.GetFloat(PREF_SFX, 0.5f);
    }

    private void SavePrefs()
    {
        PlayerPrefs.SetFloat(PREF_MUSIC, music01);
        PlayerPrefs.SetFloat(PREF_SFX, sfx01);
        PlayerPrefs.Save();
    }

    public void ReapplyToMixer()
    {
        ApplyAll();
    }

    private void UpdateBgmForScene(string sceneName)
    {
        AudioClip targetClip = null;

        // ---------------- CORE ----------------
        if (sceneName == "Home" ||
            sceneName == "Level" ||
            sceneName == "Tutorial" ||
            sceneName == "Shop")
        {
            targetClip = coreTheme;
        }

        // ---------------- RESULT ----------------
        else if (sceneName == "Score" ||
                sceneName == "ScoreEasy")
        {
            targetClip = resultTheme;
        }

        // ---------------- GAME OVER ----------------
        else if (sceneName == "GameOver")
        {
            targetClip = gameOverTheme;
        }

        // ---------------- MODE LvX ----------------
        else if (sceneName.StartsWith("ModeLv"))
        {
            int level = ExtractLevel(sceneName);
            targetClip = GetArrayClip(modeThemes, level);
        }

        // ---------------- TUTORIAL ----------------
        else if (sceneName.StartsWith("Tutorial_"))
        {
            int level = GameContext.SelectedLevel;
            targetClip = GetArrayClip(modeThemes, level);
        }

        // ---------------- GAMEPLAY ----------------
        else if (sceneName.StartsWith("GameplayLv") ||
                sceneName.StartsWith("GameplayEasyLv"))
        {
            int level = ExtractLevel(sceneName);
            targetClip = GetArrayClip(gameplayThemes, level);
        }

        PlayBgm(targetClip);
    }
    
    private void PlayBgm(AudioClip clip)
    {
        if (clip == null)
            return;

        if (bgmSource == null)
        {
            Debug.LogWarning("[AudioManager] BGM Source missing!");
            return;
        }

        if (currentBgmClip == clip)
            return;

        currentBgmClip = clip;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    private int ExtractLevel(string sceneName)
    {
        string digits = System.Text.RegularExpressions.Regex.Match(sceneName, @"\d+").Value;

        if (int.TryParse(digits, out int level))
            return level;

        return 1;
    }

    private AudioClip GetArrayClip(AudioClip[] array, int level1Based)
    {
        if (array == null || array.Length == 0)
            return null;

        int index = Mathf.Clamp(level1Based - 1, 0, array.Length - 1);
        return array[index];
    }

    public static void SFX(SfxId id)
    {
        Instance?.PlaySfx(id);
    }

    public void PlaySfx(SfxId id)
    {
        if (sfxSource == null)
        {
            Debug.LogWarning("[AudioManager] SFX Source missing!");
            return;
        }

        AudioClip clip = id switch
        {
            SfxId.UiClick => uiClick,
            SfxId.UiHover => uiHover,
            SfxId.TimerTick => timerTick,
            SfxId.TimerSuccess => timerSuccess,
            SfxId.PoseSuccess => poseSuccess,
            SfxId.HeartPop => heartPop,
            SfxId.LeaderboardWow => leaderboardWow,
            SfxId.ComplimentVoice => PickRandomCompliment(),
            _ => null
        };

        if (clip == null) return;

        sfxSource.PlayOneShot(clip);
    }

    private AudioClip PickRandomCompliment()
    {
        if (complimentVoices == null || complimentVoices.Length == 0)
            return null;

        if (complimentVoices.Length == 1)
        {
            _lastComplimentIndex = 0;
            return complimentVoices[0];
        }

        int idx = Random.Range(0, complimentVoices.Length);

        if (preventSameComplimentTwice && idx == _lastComplimentIndex)
            idx = (idx + 1) % complimentVoices.Length;

        _lastComplimentIndex = idx;
        return complimentVoices[idx];
    }
}