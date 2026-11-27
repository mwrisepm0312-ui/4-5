using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using FishNet;

// ─────────────────────────────────────────────
// PC 찾기 신호 (UDP 브로드캐스트, 8888 포트)
//
// 휴대폰은 이 신호로 PC 주소를 알아내서 연결함.
// 예전에는 WaitingScene 안에서만 보내서, 오프닝부터는 신호가 멈춰
// 수사 도중 끊긴 휴대폰이 PC를 다시 찾지 못했음 (P-66).
//
// 지금은:
//  - WaitingScene에 있는 이 컴포넌트는 '씬이 바뀌어도 남는 신호기'를 한 번만 만들고 쉼
//  - 남는 신호기는 서버가 켜져 있는 동안에만 1초마다 신호를 보냄
//    (한 판이 끝나 서버가 꺼지면 멈추고, 다음 판에 서버가 켜지면 다시 보냄)
//  - 두 번째 판에 WaitingScene이 다시 열려도 신호기는 하나만 있음
// ─────────────────────────────────────────────
public class ServerBroadcaster : MonoBehaviour
{
    public int broadcastPort = 8888;
    public int fishnetPort = 7770;

    // 씬이 바뀌어도 남는 신호기 (하나만)
    private static ServerBroadcaster persistent;

    private bool isPersistent = false;
    private UdpClient udpClient;
    private Thread broadcastThread;
    private volatile bool isRunning = false;
    private volatile bool serverOn = false;   // 메인 스레드에서 서버 상태를 확인해 넣어 줌

    void Start()
    {
        if (isPersistent) return;

        // 씬에 놓인 원본: 남는 신호기가 없으면 하나 만들고, 자기는 아무것도 하지 않음
        if (persistent == null)
        {
            var go = new GameObject("PCFinderBroadcaster");
            DontDestroyOnLoad(go);
            var b = go.AddComponent<ServerBroadcaster>();
            b.broadcastPort = broadcastPort;
            b.fishnetPort = fishnetPort;
            b.isPersistent = true;
            persistent = b;
            b.Begin();
        }
        enabled = false;
    }

    void Begin()
    {
        UpdateServerState();
        isRunning = true;
        broadcastThread = new Thread(BroadcastLoop);
        broadcastThread.IsBackground = true;
        broadcastThread.Start();
    }

    void Update()
    {
        if (isPersistent) UpdateServerState();
    }

    void UpdateServerState()
    {
        var nm = InstanceFinder.NetworkManager;
        serverOn = nm != null && nm.IsServerStarted;
    }

    void BroadcastLoop()
    {
        try
        {
            udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;

            string message = $"FISHNET_SERVER:{fishnetPort}";
            byte[] data = Encoding.UTF8.GetBytes(message);
            IPEndPoint allEndPoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);

            IPEndPoint endPoint = null;
            bool sendAll = true;
            int sinceRefresh = 999;

            while (isRunning)
            {
                if (serverOn)
                {
                    // 와이파이를 바꿀 수도 있으니 5초마다 브로드캐스트 주소를 다시 구함
                    if (endPoint == null || sinceRefresh >= 5)
                    {
                        string broadcastIP = GetSubnetBroadcast();
                        if (endPoint == null || endPoint.Address.ToString() != broadcastIP)
                            Debug.Log($"[Server] 브로드캐스트 주소: {broadcastIP}");
                        endPoint = new IPEndPoint(IPAddress.Parse(broadcastIP), broadcastPort);
                        sendAll = broadcastIP != "255.255.255.255";
                        sinceRefresh = 0;
                    }

                    try { udpClient.Send(data, data.Length, endPoint); }
                    catch (SocketException e) { Debug.LogWarning($"[Server] 신호 보내기 실패: {e.Message}"); }

                    // 공유기에 따라 서브넷 브로드캐스트가 막히는 경우를 대비해 전체 브로드캐스트도 함께 보냄
                    if (sendAll)
                    {
                        try { udpClient.Send(data, data.Length, allEndPoint); }
                        catch (SocketException) { sendAll = false; }   // 허용 안 되는 환경이면 그만 보냄
                    }
                    sinceRefresh++;
                }
                Thread.Sleep(1000);
            }
        }
        catch (System.Exception e)
        {
            if (isRunning) Debug.LogError($"[Server] BroadcastLoop 오류: {e.Message}");
        }
    }

    // 같은 공유기 안 휴대폰에게 보내는 주소 (192.168 / 10 / 172.16~31 모두 지원)
    string GetSubnetBroadcast()
    {
        return LanAddress.GetBroadcastIP();
    }

    void Stop()
    {
        isRunning = false;
        udpClient?.Close();
    }

    void OnApplicationQuit()
    {
        if (isPersistent) Stop();
    }

    void OnDestroy()
    {
        if (!isPersistent) return;
        Stop();
        if (persistent == this) persistent = null;
    }
}
