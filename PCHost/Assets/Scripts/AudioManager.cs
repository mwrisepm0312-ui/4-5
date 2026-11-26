using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("BGM 클립")]
    public AudioClip lobbyBGM;
    public AudioClip successBGM;
    public AudioClip failBGM;

    [Header("볼륨")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    [Header("음소거 버튼")]
    public Button muteButton;
    public Sprite soundOnSprite;   // 🔊 이미지
    public Sprite soundOffSprite;  // 🔇 이미지

    private AudioSource bgmSource;
    private bool isMuted = false;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.playOnAwake = false;
    }

    void Start()
    {
        if (muteButton != null)
            muteButton.onClick.AddListener(ToggleMute);

        UpdateMuteButtonIcon();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "PC_LobbyScene":
                PlayBGM(lobbyBGM);
                break;
            case "ArrestScene":
                PlayBGM(successBGM);
                break;
            case "FailScene":
                PlayBGM(failBGM);
                break;
            default:
                StopBGM();
                break;
        }
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.volume = isMuted ? 0f : bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        bgmSource.volume = isMuted ? 0f : bgmVolume;
        UpdateMuteButtonIcon();
    }

    void UpdateMuteButtonIcon()
    {
        if (muteButton == null) return;
        Image btnImage = muteButton.GetComponent<Image>();
        if (btnImage == null) return;

        if (isMuted && soundOffSprite != null)
            btnImage.sprite = soundOffSprite;
        else if (!isMuted && soundOnSprite != null)
            btnImage.sprite = soundOnSprite;
    }

    public void SetVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (!isMuted) bgmSource.volume = bgmVolume;
    }
}