using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using FishNet.Transporting.Tugboat;
using FishNet;

public class ServerBroadcaster : MonoBehaviour
{
    public int broadcastPort = 8888;
    public int fishnetPort = 7770;

    private UdpClient udpClient;
    private Thread broadcastThread;
    private bool isRunning = false;

    void Start()
    {
        isRunning = true;
        broadcastThread = new Thread(BroadcastLoop);
        broadcastThread.IsBackground = true;
        broadcastThread.Start();
    }

    void BroadcastLoop()
    {
        try
        {
            udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;

            // 255.255.255.255 대신 서브넷 브로드캐스트 주소 사용
            string broadcastIP = GetSubnetBroadcast();
            Debug.Log($"[Server] 브로드캐스트 주소: {broadcastIP}");

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(broadcastIP), broadcastPort);
            string message = $"FISHNET_SERVER:{fishnetPort}";
            byte[] data = Encoding.UTF8.GetBytes(message);

            // 공유기에 따라 서브넷 브로드캐스트가 막히는 경우를 대비해 전체 브로드캐스트도 함께 보냄
            IPEndPoint allEndPoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
            bool sendAll = broadcastIP != "255.255.255.255";

            while (isRunning)
            {
                udpClient.Send(data, data.Length, endPoint);
                if (sendAll)
                {
                    try { udpClient.Send(data, data.Length, allEndPoint); }
                    catch (SocketException) { sendAll = false; }   // 허용 안 되는 환경이면 그만 보냄
                }
                Thread.Sleep(1000);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Server] BroadcastLoop 오류: {e.Message}");
        }
    }

    // 같은 공유기 안 휴대폰에게 보내는 주소 (192.168 / 10 / 172.16~31 모두 지원)
    string GetSubnetBroadcast()
    {
        return LanAddress.GetBroadcastIP();
    }

    void OnDestroy()
    {
        isRunning = false;
        udpClient?.Close();
    }
}