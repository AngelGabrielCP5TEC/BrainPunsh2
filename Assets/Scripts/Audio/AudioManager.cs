using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Persistent music director. Singleton; survives scene loads.
//   • Auto-plays the right track for MainMenu / RING based on scene name
//   • PlayWinMusic / PlayLoseMusic called explicitly by MatchEndOverlay
//   • Crossfades between tracks; uses unscaled time so fades work while paused
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null) _instance = FindObjectOfType<AudioManager>();
            if (_instance == null)
            {
                var go = new GameObject("AudioManager (auto)");
                _instance = go.AddComponent<AudioManager>();
            }
            return _instance;
        }
    }

    [Header("Music Library")]
    [SerializeField] private AudioClip _menuMusic;
    [SerializeField] private AudioClip _ringMusic;
    [SerializeField] private AudioClip _winMusic;
    [SerializeField] private AudioClip _loseMusic;

    [Header("SFX Library")]
    [SerializeField] private AudioClip _blockSfx;
    [SerializeField] private AudioClip _roundStartSfx;
    [SerializeField] private AudioClip _roundEndBellSfx;

    [Header("Mixer / Volume")]
    [SerializeField] [Range(0f, 1f)] private float _musicVolume     = 0.6f;
    [SerializeField] [Range(0f, 1f)] private float _sfxVolume       = 0.9f;
    [SerializeField] [Range(0f, 1f)] private float _matchEndDuckVol = 0.0f; // "quiet everything" duck on win/lose
    [SerializeField] private float _defaultFadeDuration = 1.0f;

    [Header("Scene → Track Mapping")]
    [SerializeField] private string _menuSceneName = "MainMenu";
    [SerializeField] private string _ringSceneName = "RING";

    private AudioSource _musicSource;
    private AudioSource _sfxSource;
    private Coroutine _fadeCo;

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        _musicSource = GetComponent<AudioSource>();
        if (_musicSource == null) _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.playOnAwake = false;
        _musicSource.loop = true;
        _musicSource.volume = 0f;

        // Dedicated SFX source (separate so PlayOneShot doesn't interrupt music)
        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.loop = false;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // Handle the scene we booted into (sceneLoaded doesn't fire for it)
        ApplyTrackForScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        if (_instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyTrackForScene(scene.name);

    private void ApplyTrackForScene(string sceneName)
    {
        if (sceneName == _menuSceneName) PlayMenuMusic();
        else if (sceneName == _ringSceneName) PlayRingMusic();
        // Win/Lose music is triggered explicitly by MatchEndOverlay, not by scene change
    }

    // ── Public API ────────────────────────────────────────────────────
    public void PlayMenuMusic() => CrossfadeTo(_menuMusic, loop: true);
    public void PlayRingMusic() => CrossfadeTo(_ringMusic, loop: true);
    public void PlayWinMusic()  => CrossfadeTo(_winMusic, loop: true);   // looped per spec
    public void PlayLoseMusic() => CrossfadeTo(_loseMusic, loop: false); // plays once per spec
    public void StopMusic(float fadeDuration = -1f) => CrossfadeTo(null, false, fadeDuration);

    public void PlayBlock()         => PlaySfx(_blockSfx);
    public void PlayRoundStart()    => PlaySfx(_roundStartSfx);
    public void PlayRoundEndBell()  => PlaySfx(_roundEndBellSfx);

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clip, _sfxVolume * volumeScale);
    }

    public void CrossfadeTo(AudioClip clip, bool loop, float fadeDuration = -1f)
    {
        if (fadeDuration < 0f) fadeDuration = _defaultFadeDuration;
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CrossfadeRoutine(clip, loop, fadeDuration));
    }

    private IEnumerator CrossfadeRoutine(AudioClip clip, bool loop, float duration)
    {
        // Fade out current
        if (_musicSource.isPlaying)
        {
            float startVol = _musicSource.volume;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                _musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
                yield return null;
            }
            _musicSource.volume = 0f;
            _musicSource.Stop();
        }

        if (clip == null) { _fadeCo = null; yield break; }

        // Fade in new
        _musicSource.clip = clip;
        _musicSource.loop = loop;
        _musicSource.volume = 0f;
        _musicSource.Play();
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            _musicSource.volume = Mathf.Lerp(0f, _musicVolume, t / duration);
            yield return null;
        }
        _musicSource.volume = _musicVolume;
        _fadeCo = null;
    }
}
