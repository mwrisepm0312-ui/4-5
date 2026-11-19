using FishNet;
using FishNet.Connection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class PCLobbyUI : MonoBehaviour
{
    [Header("연결 UI")]
    public TextMeshProUGUI connectedText;

    [Header("타이머 UI")]
    public TextMeshProUGUI timerText;
    private float remainingTime = 15 * 60f;
    private bool timerRunning = false;

    [Header("용의자 버튼")]
    public Button suspectButton;

    [Header("용의자특정씬 바로가기 버튼")]
    public Button goToSuspectSceneButton;

    [Header("용의자 팝업")]
    public GameObject suspectPopup;
    public Button closePopupButton;
    public Button prevButton;
    public Button nextButton;

    [Header("용의자 프로필 표시")]
    public Image suspectImage;
    public GameObject suspectImagePlaceholder;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI ageText;
    public TextMeshProUGUI jobText;
    public TextMeshProUGUI featureText;
    public TextMeshProUGUI pageText;

    // ──────────────────────────────────────
    // 용의자 데이터 (스토리 기준)
    // ──────────────────────────────────────
    private SuspectData[] suspects;

    private int currentIndex = 0;

    // ──────────────────────────────────────
    // 네트워크
    // ──────────────────────────────────────
    private int connectedCount = 0;
    private int maxPlayers = 0;

    void Start()
    {
        // 용의자 데이터 초기화 (이미지 포함)
        suspects = new SuspectData[]
        {
            new SuspectData(
                "수집가",
                45,
                "보석 수집가",
                "열린마음 은행 VIP 금고에 레드 다이아 반지를 맡김.\n사건 당일 은행 주변을 서성이는 모습이 CCTV에 찍힘.",
                Resources.Load<Sprite>("Suspects/suspect_1")
            ),
            new SuspectData(
                "경비원",
                38,
                "은행 경비원",
                "사건 당일 평소처럼 근무함.\n순찰 중 이상한 점을 봤다고 진술함.",
                Resources.Load<Sprite>("Suspects/suspect_2")
            ),
            new SuspectData(
                "비서",
                35,
                "주얼리씨 비서",
                "주얼리씨 밑에서 15년째 근무.\n화장실에만 있었다고 했지만\n금고 앞에서 CCTV에 찍힘.",
                Resources.Load<Sprite>("Suspects/suspect_3")
            ),
            new SuspectData(
                "청소부",
                52,
                "은행 청소부",
                "지하에서 청소 중이었다고 진술.\n수상한 가방을 들고 다니는 모습이 목격됨.",
                Resources.Load<Sprite>("Suspects/suspect_4")
            ),
        };

        // 네트워크 연결 카운트
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnRemoteConnectionState += OnClientConnected;
            connectedCount = Mathf.Max(0, InstanceFinder.ServerManager.Clients.Count - 1);
        }

        var tugboat = InstanceFinder.NetworkManager
            .GetComponent<FishNet.Transporting.Tugboat.Tugboat>();
        if (tugboat != null)
            maxPlayers = tugboat.GetMaximumClients() - 1;

        UpdateConnectedText();

        suspectPopup.SetActive(false);

        suspectButton.onClick.AddListener(OpenSuspectPopup);
        closePopupButton.onClick.AddListener(CloseSuspectPopup);
        prevButton.onClick.AddListener(ShowPrev);
        nextButton.onClick.AddListener(ShowNext);
        goToSuspectSceneButton.onClick.AddListener(GoToSuspectScene);

        timerRunning = true;
    }

    // ──────────────────────────────────────
    // 타이머
    // ──────────────────────────────────────
    void Update()
    {
        if (!timerRunning) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            timerRunning = false;
            UpdateTimerText();
            GoToSuspectScene();
            return;
        }

        UpdateTimerText();
    }

    void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
        timerText.color = remainingTime <= 180f ? Color.red : Color.white;
    }

    // ──────────────────────────────────────
    // 용의자특정씬 이동
    // ──────────────────────────────────────
    void GoToSuspectScene()
    {
        timerRunning = false;
        SceneManager.LoadScene("SuspectScene");
    }

    // ──────────────────────────────────────
    // 팝업 열기 / 닫기
    // ──────────────────────────────────────
    void OpenSuspectPopup()
    {
        currentIndex = 0;
        suspectPopup.SetActive(true);
        ShowCurrentSuspect();
    }

    void CloseSuspectPopup()
    {
        suspectPopup.SetActive(false);
    }

    // ──────────────────────────────────────
    // 페이지 넘기기
    // ──────────────────────────────────────
    void ShowPrev()
    {
        currentIndex = (currentIndex - 1 + suspects.Length) % suspects.Length;
        ShowCurrentSuspect();
    }

    void ShowNext()
    {
        currentIndex = (currentIndex + 1) % suspects.Length;
        ShowCurrentSuspect();
    }

    void ShowCurrentSuspect()
    {
        SuspectData s = suspects[currentIndex];

        nameText.text = $"이름: {s.Name}";
        ageText.text = $"나이: {s.Age}세";
        jobText.text = $"직업: {s.Job}";
        featureText.text = $"특징: {s.Feature}";
        pageText.text = $"{currentIndex + 1} / {suspects.Length}";

        if (s.Photo != null)
        {
            suspectImage.sprite = s.Photo;
            suspectImage.gameObject.SetActive(true);
            if (suspectImagePlaceholder != null)
                suspectImagePlaceholder.SetActive(false);
        }
        else
        {
            suspectImage.gameObject.SetActive(false);
            if (suspectImagePlaceholder != null)
                suspectImagePlaceholder.SetActive(true);
        }

        prevButton.gameObject.SetActive(suspects.Length > 1);
        nextButton.gameObject.SetActive(suspects.Length > 1);
    }

    // ──────────────────────────────────────
    // 네트워크 이벤트
    // ──────────────────────────────────────
    void OnClientConnected(NetworkConnection conn,
        FishNet.Transporting.RemoteConnectionStateArgs args)
    {
        if (conn.ClientId == 0) return;

        if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Started)
        {
            connectedCount++;
            UpdateConnectedText();
            StartCoroutine(LoadJobSelectScene(conn));
        }
        else if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Stopped)
        {
            connectedCount = Mathf.Max(0, connectedCount - 1);
            UpdateConnectedText();

            var gm = FindFirstObjectByType<GameManager>();
            gm?.OnClientDisconnected(conn.ClientId);
        }
    }

    IEnumerator LoadJobSelectScene(NetworkConnection conn)
    {
        yield return new WaitForSeconds(0.5f);
        var gm = FindFirstObjectByType<GameManager>();
        gm?.SetMaxPlayersAndLoadSceneForConnServerRpc(maxPlayers, conn.ClientId);
    }

    void UpdateConnectedText()
    {
        connectedText.text = $"연결된 휴대폰  {connectedCount} / {maxPlayers}";
    }

    void OnDestroy()
    {
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnRemoteConnectionState -= OnClientConnected;
    }
}