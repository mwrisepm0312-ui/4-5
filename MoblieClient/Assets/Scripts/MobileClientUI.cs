using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using DG.Tweening;

public class MobileClientUI : MonoBehaviour
{
    [Header("FishNet")]
    public NetworkManager networkManager;

    [Header("UI")]
    public TextMeshProUGUI statusText;
    public Button connectButton;

    [Header("게임종료 버튼")]
    public Button quitButton;

    [Header("UDP Server Discovery")]
    public int broadcastPort = 8888;

    [Header("Scene")]
    public string nextSceneName = "JobSelectScene";

    private UdpClient udpClient;
    private Thread listenThread;

    private volatile bool isSearching = false;
    private volatile bool searchFailed = false;   // 찾기가 오류로 끝나면 버튼을 다시 켜기 위해
    private float searchStartTime;
    private bool searchHintShown = false;
    private volatile bool stopListening = false;

    private string foundIP = null;
    private int foundPort = 7770;

    private bool connecting = false;
    private bool sceneLoaded = false;

    private Tween quitFloatTween;
    private float quitBaseY;

    private Tween connectFloatTween;
    private float connectBaseY;

    void Start()
    {
        // ClientMobile 씬 진입 시 상태 완전 초기화
        isSearching = false;
        stopListening = false;
        connecting = false;
        sceneLoaded = false;
        foundIP = null;

        // 모바일 증거 초기화
        if (DataManager.Instance != null)
            DataManager.Instance.CollectedEvidences.Clear();

        // 씬의 NetworkManager가 DestroyNewest로 파괴되는 경우 InstanceFinder로 대체
        networkManager = FishNet.InstanceFinder.NetworkManager;
        if (networkManager == null)
        {
            Debug.LogError("[Mobile] NetworkManager를 찾을 수 없습니다.");
            return;
        }

        statusText.text = "[연결]을 눌러 수사에 참여하세요";
        statusText.gameObject.SetActive(true);
        connectButton.interactable = true;
        connectButton.onClick.AddListener(OnConnectButtonClicked);
        quitButton.onClick.AddListener(OnQuitClicked);

        // 종료 버튼 둥실
        quitBaseY = quitButton.transform.localPosition.y;

        var quitTrigger = quitButton.gameObject.AddComponent<EventTrigger>();

        var quitDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        quitDown.callback.AddListener(_ =>
        {
            quitFloatTween?.Kill();
            quitButton.transform.localPosition = new Vector3(
                quitButton.transform.localPosition.x, quitBaseY, quitButton.transform.localPosition.z);
            quitFloatTween = quitButton.transform
                .DOLocalMoveY(quitBaseY + 20f, 0.8f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        });
        quitTrigger.triggers.Add(quitDown);

        var quitUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        quitUp.callback.AddListener(_ =>
        {
            quitFloatTween?.Kill();
            quitButton.transform
                .DOLocalMoveY(quitBaseY, 0.15f)
                .SetEase(Ease.OutSine);
        });
        quitTrigger.triggers.Add(quitUp);

        // 연결 버튼 둥실
        connectBaseY = connectButton.transform.localPosition.y;
        var connectTrigger = connectButton.gameObject.AddComponent<EventTrigger>();

        var connectDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        connectDown.callback.AddListener(_ =>
        {
            connectFloatTween?.Kill();
            connectButton.transform.localPosition = new Vector3(
                connectButton.transform.localPosition.x, connectBaseY, connectButton.transform.localPosition.z);
            connectFloatTween = connectButton.transform
                .DOLocalMoveY(connectBaseY + 20f, 0.8f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        });
        connectTrigger.triggers.Add(connectDown);

        var connectUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        connectUp.callback.AddListener(_ =>
        {
            connectFloatTween?.Kill();
            connectButton.transform
                .DOLocalMoveY(connectBaseY, 0.15f)
                .SetEase(Ease.OutSine);
        });
        connectTrigger.triggers.Add(connectUp);

        networkManager.ClientManager.OnClientConnectionState
            += OnClientConnectionState;

        Debug.Log("[Mobile] MobileClientUI 초기화 완료");
    }

    void OnQuitClicked()
    {
        stopListening = true;
        udpClient?.Close();

        if (networkManager != null && networkManager.ClientManager.Started)
            networkManager.ClientManager.StopConnection();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnConnectButtonClicked()
    {
        if (isSearching || connecting) return;

        Debug.Log("[Mobile] 서버 탐색 시작");

        isSearching = true;
        stopListening = false;

        connectButton.interactable = false;
        statusText.text = "PC를 찾는 중...";
        searchFailed = false;
        searchStartTime = Time.time;
        searchHintShown = false;

        listenThread = new Thread(ListenForServer);
        listenThread.IsBackground = true;
        listenThread.Start();
    }

    void ListenForServer()
    {
        try
        {
            udpClient = new UdpClient(broadcastPort);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, broadcastPort);

            Debug.Log($"[Mobile] UDP {broadcastPort} 수신 대기");

            while (!stopListening)
            {
                byte[] data = udpClient.Receive(ref endPoint);
                string message = Encoding.UTF8.GetString(data);

                Debug.Log($"[Mobile] UDP 수신: {message}");

                if (message.StartsWith("FISHNET_SERVER:"))
                {
                    string portString = message.Substring("FISHNET_SERVER:".Length);
                    int port = 7770;
                    if (int.TryParse(portString, out int parsedPort))
                        port = parsedPort;

                    foundPort = port;
                    foundIP = endPoint.Address.ToString();

                    Debug.Log($"[Mobile] 서버 발견! IP={foundIP}, Port={foundPort}");
                    break;
                }
            }
        }
        catch (SocketException e)
        {
            if (!stopListening)
                Debug.LogError($"[Mobile] UDP Socket 오류: {e.Message}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Mobile] UDP 오류: {e}");
        }
        finally
        {
            if (string.IsNullOrEmpty(foundIP) && !stopListening)
                searchFailed = true;
            isSearching = false;
            udpClient?.Close();
            udpClient = null;
        }
    }

    void Update()
    {
        // 10초가 지나도 PC를 못 찾으면 와이파이를 확인하라고 안내 (계속 찾는 중)
        if (isSearching && !searchHintShown && Time.time - searchStartTime > 10f)
        {
            searchHintShown = true;
            statusText.text = "와이파이를 확인해 주세요";
        }

        // 찾기가 오류로 끝났으면 다시 누를 수 있게
        if (searchFailed)
        {
            searchFailed = false;
            if (!connecting)
            {
                statusText.text = "다시 눌러 주세요";
                connectButton.interactable = true;
            }
        }

        if (!string.IsNullOrEmpty(foundIP) && !connecting)
        {
            string serverIP = foundIP;
            int serverPort = foundPort;
            foundIP = null;
            ConnectToServer(serverIP, serverPort);
        }
    }

    void ConnectToServer(string serverIP, int serverPort)
    {
        connecting = true;
        statusText.text = $"PC를 찾았어요! 연결하는 중...\n({serverIP})";

        Debug.Log($"[Mobile] FishNet 연결 시도: {serverIP}:{serverPort}");

        Tugboat tugboat = networkManager.GetComponent<Tugboat>();
        if (tugboat == null)
        {
            Debug.LogError("[Mobile] Tugboat 컴포넌트를 찾을 수 없습니다.");
            statusText.text = "네트워크 설정 오류 (Tugboat 없음)";
            connecting = false;
            connectButton.interactable = true;
            return;
        }

        tugboat.SetPort((ushort)serverPort);
        Debug.Log($"[Mobile] Tugboat Port 설정: {serverPort}");

        bool result = networkManager.ClientManager.StartConnection(serverIP);
        Debug.Log($"[Mobile] StartConnection 결과: {result}");

        if (!result)
        {
            statusText.text = "연결하지 못했어요. 다시 눌러 주세요.";
            connecting = false;
            connectButton.interactable = true;
        }
    }

    void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        Debug.Log($"[Mobile] ConnectionState = {args.ConnectionState} / sceneLoaded={sceneLoaded} / connecting={connecting}");

        switch (args.ConnectionState)
        {
            case LocalConnectionState.Starting:
                statusText.text = "연결하는 중...";
                break;

            case LocalConnectionState.Started:
                statusText.text = "연결됐어요!";
                Debug.Log("[Mobile] 연결 성공 → 씬 이동");
                if (!sceneLoaded)
                {
                    sceneLoaded = true;
                    StartCoroutine(LoadNextScene());
                }
                break;

            case LocalConnectionState.Stopping:
                Debug.Log("[Mobile] Stopping 수신 — 무시");
                break;

            case LocalConnectionState.Stopped:
                Debug.Log($"[Mobile] Stopped 수신 — sceneLoaded={sceneLoaded}, connecting={connecting}");
                // 씬 복귀 직후 오는 Stopped는 무시 (connecting=false이고 sceneLoaded=false인 초기 상태)
                if (connecting && !sceneLoaded)
                {
                    statusText.text = "연결에 실패했어요.\nPC와 같은 와이파이인지 확인해 주세요.";
                    Debug.LogError("[Mobile] FishNet 연결 실패/종료");
                    connecting = false;
                    connectButton.interactable = true;
                }
                break;
        }
    }

    System.Collections.IEnumerator LoadNextScene()
    {
        yield return null;
        Debug.Log($"[Mobile] 씬 이동: {nextSceneName}");
        SceneTransition.Load(nextSceneName);
    }

    void OnDestroy()
    {
        quitFloatTween?.Kill();
        connectFloatTween?.Kill();
        stopListening = true;

        if (networkManager != null)
            networkManager.ClientManager.OnClientConnectionState
                -= OnClientConnectionState;

        udpClient?.Close();
        foundIP = null;
    }
}