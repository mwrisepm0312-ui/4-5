using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;

// ──────────────────────────────────────
// 모바일 공통 관리 (씬에 따로 놓지 않아도 앱이 켜질 때 자동으로 만들어짐)
//  1) 게임 중 휴대폰 화면이 꺼지지 않게 (M-63)
//  2) 수사 도중 연결이 끊기면 안내 후 연결 화면으로 이동, 모은 증거는 유지 (M-62)
//  3) 화면 안내: 아래쪽 말풍선(ShowToast) / 화면 전체 안내(ShowBlocking)
//  4) 60fps로 실행 (M-68)
// ──────────────────────────────────────
public class MobileSession : MonoBehaviour
{
    private const string ConnectSceneName = "ClientMobile";

    private static MobileSession instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        // 게임 중에는 화면이 저절로 꺼지지 않게 (꺼지면 연결이 끊길 수 있음)
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // 안드로이드 기본값은 30fps라 애니메이션이 뚝뚝 끊겨 보임 → 60fps (M-68)
        Application.targetFrameRate = 60;

        if (instance != null) return;
        var go = new GameObject("MobileSession");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MobileSession>();
    }

    static MobileSession Get()
    {
        if (instance == null) Init();
        return instance;
    }

    // 아래쪽 말풍선 (잠깐 보였다 사라짐, 터치를 막지 않음)
    public static void ShowToast(string message, float seconds = 2.5f) => Get().Toast(message, seconds);

    // 화면 전체 안내 (터치를 막고 계속 보임, 연결 화면으로 가면 사라짐)
    public static void ShowBlocking(string title, string message) => Get().Blocking(title, message);

    public static void HideBlocking()
    {
        if (instance != null) instance.HideBlockingInternal();
    }

    // ──────────────────────────────────────
    // 연결 끊김 감시
    // ──────────────────────────────────────
    private NetworkManager watchedManager;
    private bool returning = false;
    private bool quitting = false;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Unwatch();
    }

    void OnApplicationQuit()
    {
        quitting = true;
    }

    void Update()
    {
        // 씬이 바뀌어도 살아 있는 NetworkManager의 연결 상태를 계속 지켜봄
        NetworkManager nm = InstanceFinder.NetworkManager;
        if (nm != watchedManager)
        {
            Unwatch();
            watchedManager = nm;
            if (watchedManager != null)
                watchedManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        }
    }

    void Unwatch()
    {
        if (ReferenceEquals(watchedManager, null)) return;
        try { watchedManager.ClientManager.OnClientConnectionState -= OnClientConnectionState; }
        catch { /* 이미 사라진 경우 */ }
        watchedManager = null;
    }

    void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState != LocalConnectionState.Stopped) return;
        if (quitting || returning) return;

        // 연결 화면에서는 MobileClientUI가 직접 처리
        if (SceneManager.GetActiveScene().name == ConnectSceneName) return;

        // 한 판이 정상적으로 끝나 서버가 닫힌 경우는 끊김이 아님
        var dm = DataManager.Instance;
        if (dm != null && dm.RoundEnded) return;

        StartCoroutine(ReturnToConnectScene());
    }

    IEnumerator ReturnToConnectScene()
    {
        returning = true;

        var dm = DataManager.Instance;
        if (dm != null) dm.ReconnectPending = true;   // 연결 화면에서 모은 증거를 지우지 않도록

        HideBlockingInternal();
        Toast("연결이 끊겼어요.\n연결 화면으로 돌아갈게요.", 2f);

        yield return new WaitForSecondsRealtime(2f);

        if (SceneManager.GetActiveScene().name != ConnectSceneName)
            SceneTransition.Load(ConnectSceneName);

        returning = false;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CacheFont();

        // 새 판(연결 화면)으로 오면 '수사 끝' 안내는 닫음
        if (scene.name == ConnectSceneName)
            HideBlockingInternal();
    }

    // ──────────────────────────────────────
    // 안내 화면 만들기 (씬과 상관없이 맨 위에 그려지는 캔버스)
    // ──────────────────────────────────────
    private Canvas overlayCanvas;
    private TMP_FontAsset font;

    private CanvasGroup toastGroup;
    private TextMeshProUGUI toastText;
    private Sequence toastSeq;

    private GameObject blockingRoot;
    private TextMeshProUGUI blockingTitle;
    private TextMeshProUGUI blockingMessage;

    // TMP 기본 글꼴에는 한글이 없어서, 씬에서 쓰는 메이플스토리 글꼴을 찾아 씀
    void CacheFont()
    {
        if (font != null) return;

        TMP_FontAsset fallback = null;
        foreach (var t in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.font == null) continue;
            if (t.font.name.Contains("Maplestory")) { font = t.font; break; }
            if (fallback == null) fallback = t.font;
        }
        if (font == null) font = fallback;

        if (font != null)
        {
            if (toastText != null) toastText.font = font;
            if (blockingTitle != null) blockingTitle.font = font;
            if (blockingMessage != null) blockingMessage.font = font;
        }
    }

    void EnsureCanvas()
    {
        if (overlayCanvas != null) return;

        var go = new GameObject("MobileNoticeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);

        overlayCanvas = go.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 30000;   // 게임 화면보다 위, 화면 전환 효과(32000)보다는 아래

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);   // 다른 씬과 같은 기준
        scaler.matchWidthOrHeight = 0.5f;
    }

    TextMeshProUGUI MakeText(RectTransform parent, string name, float size, float minSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);

        var text = go.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.enableAutoSizing = true;   // 길어져도 판 밖으로 나가지 않게
        text.fontSizeMin = minSize;
        text.fontSizeMax = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    static readonly Color32 PanelNavy = new Color32(0x1A, 0x27, 0x51, 0xF2);   // 팝업과 같은 남색
    static readonly Color32 Gold = new Color32(0xFB, 0xC8, 0x1F, 0xFF);

    void Toast(string message, float seconds)
    {
        CacheFont();
        EnsureCanvas();

        if (toastGroup == null)
        {
            var panel = new GameObject("Toast", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rt = (RectTransform)panel.transform;
            rt.SetParent(overlayCanvas.transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 150f);
            rt.sizeDelta = new Vector2(1100f, 220f);

            var bg = panel.GetComponent<Image>();
            bg.color = PanelNavy;
            bg.raycastTarget = false;

            toastGroup = panel.GetComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
            toastGroup.interactable = false;

            toastText = MakeText(rt, "Text", 56f, 36f);
            var trt = (RectTransform)toastText.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(30f, 20f);
            trt.offsetMax = new Vector2(-30f, -20f);
        }

        toastText.text = message;
        toastGroup.transform.SetAsLastSibling();   // 전체 안내보다 위에

        toastSeq?.Kill();
        toastGroup.alpha = 0f;
        toastGroup.transform.localScale = Vector3.one * 0.9f;
        toastSeq = DOTween.Sequence()
            .SetUpdate(true)
            .Append(toastGroup.DOFade(1f, 0.2f))
            .Join(toastGroup.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack))
            .AppendInterval(seconds)
            .Append(toastGroup.DOFade(0f, 0.4f));
    }

    void Blocking(string title, string message)
    {
        CacheFont();
        EnsureCanvas();

        if (blockingRoot == null)
        {
            // 화면 전체를 어둡게 덮어 뒤쪽 터치를 막음
            blockingRoot = new GameObject("BlockingNotice", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)blockingRoot.transform;
            rt.SetParent(overlayCanvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var dim = blockingRoot.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.75f);
            dim.raycastTarget = true;

            // 가운데 금색 테두리 + 남색 판
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            var brt = (RectTransform)border.transform;
            brt.SetParent(rt, false);
            brt.sizeDelta = new Vector2(1212f, 512f);
            border.GetComponent<Image>().color = Gold;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var prt = (RectTransform)panel.transform;
            prt.SetParent(rt, false);
            prt.sizeDelta = new Vector2(1200f, 500f);
            panel.GetComponent<Image>().color = PanelNavy;

            blockingTitle = MakeText(prt, "Title", 120f, 80f);
            blockingTitle.color = Gold;
            var trt = (RectTransform)blockingTitle.transform;
            trt.sizeDelta = new Vector2(1100f, 160f);
            trt.anchoredPosition = new Vector2(0f, 90f);

            blockingMessage = MakeText(prt, "Message", 72f, 48f);
            var mrt = (RectTransform)blockingMessage.transform;
            mrt.sizeDelta = new Vector2(1100f, 180f);
            mrt.anchoredPosition = new Vector2(0f, -90f);
        }

        blockingTitle.text = title;
        blockingMessage.text = message;

        if (!blockingRoot.activeSelf)
        {
            blockingRoot.SetActive(true);
            blockingRoot.transform.localScale = Vector3.one * 0.95f;
            blockingRoot.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
        }
        if (toastGroup != null) toastGroup.transform.SetAsLastSibling();
    }

    void HideBlockingInternal()
    {
        if (blockingRoot != null && blockingRoot.activeSelf)
        {
            DOTween.Kill(blockingRoot.transform);
            blockingRoot.SetActive(false);
        }
    }

    void OnDestroy()
    {
        toastSeq?.Kill();
        if (instance == this) instance = null;
    }
}
