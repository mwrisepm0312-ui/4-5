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

public class MobileClientUI : MonoBehaviour
{
    [Header("FishNet")]
    public NetworkManager networkManager;

    [Header("UI")]
    public TextMeshProUGUI statusText;
    public Button connectButton;

    [Header("UDP Server Discovery")]
    public int broadcastPort = 8888;

    [Header("Scene")]
    public string nextSceneName = "JobSelectScene";

    private UdpClient udpClient;
    private Thread listenThread;

    private volatile bool isSearching = false;
    private volatile bool stopListening = false;


    private string foundIP = null;
    private int foundPort = 7770;

    private bool connecting = false;
    private bool sceneLoaded = false;

    void Start()
    {
        if (networkManager == null)
        {
            Debug.LogError("[Mobile] NetworkManager가 연결되지 않았습니다.");
            return;
        }

        statusText.text = "연결 버튼을 눌러주세요";
        connectButton.onClick.AddListener(OnConnectButtonClicked);

        networkManager.ClientManager.OnClientConnectionState
            += OnClientConnectionState;

        Debug.Log("[Mobile] MobileClientUI 초기화 완료");
    }

    void OnConnectButtonClicked()
    {
        if (isSearching || connecting) return;

        Debug.Log("[Mobile] 서버 탐색 시작");

        isSearching = true;
        stopListening = false;

        connectButton.interactable = false;
        statusText.text = "서버 탐색 중...";

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
            isSearching = false;
            udpClient?.Close();
            udpClient = null;
        }
    }

    void Update()
    {
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
        statusText.text = $"서버 발견: {serverIP}:{serverPort}\n연결 중...";

        Debug.Log($"[Mobile] FishNet 연결 시도: {serverIP}:{serverPort}");

        Tugboat tugboat = networkManager.GetComponent<Tugboat>();
        if (tugboat == null)
        {
            Debug.LogError("[Mobile] Tugboat 컴포넌트를 찾을 수 없습니다.");
            statusText.text = "Tugboat를 찾을 수 없습니다.";
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
            statusText.text = "연결 시작에 실패했습니다.";
            connecting = false;
            connectButton.interactable = true;
        }

    }

    void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        Debug.Log($"[Mobile] ConnectionState = {args.ConnectionState}");

        switch (args.ConnectionState)
        {
            case LocalConnectionState.Starting:
                statusText.text = "서버 연결 시작...";
                break;

            case LocalConnectionState.Started:
                statusText.text = "연결 완료!";
                Debug.Log("[Mobile] 연결 성공 → 씬 이동");
                if (!sceneLoaded)
                {
                    sceneLoaded = true;
                    StartCoroutine(LoadNextScene());
                }
                break;

            case LocalConnectionState.Stopped:
                if (!sceneLoaded)
                {
                    statusText.text = "서버 연결 실패";
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
        SceneManager.LoadScene(nextSceneName);
    }

    void OnDestroy()
    {
        stopListening = true;

        if (networkManager != null)
            networkManager.ClientManager.OnClientConnectionState
                -= OnClientConnectionState;

        udpClient?.Close();
        foundIP = null;
    }
}