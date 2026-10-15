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

            while (isRunning)
            {
                udpClient.Send(data, data.Length, endPoint);
                Thread.Sleep(1000);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Server] BroadcastLoop 오류: {e.Message}");
        }
    }

    string GetSubnetBroadcast()
    {
        try
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ip = addr.Address.ToString();
                        if (ip.StartsWith("192.168"))
                        {
                            // 예: 192.168.0.2 + 마스크 255.255.255.0 → 192.168.0.255
                            byte[] ipBytes = addr.Address.GetAddressBytes();
                            byte[] maskBytes = addr.IPv4Mask.GetAddressBytes();
                            byte[] broadcastBytes = new byte[4];
                            for (int i = 0; i < 4; i++)
                                broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                            return new IPAddress(broadcastBytes).ToString();
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Server] GetSubnetBroadcast 오류: {e.Message}");
        }
        return "192.168.0.255"; // 폴백
    }

    void OnDestroy()
    {
        isRunning = false;
        udpClient?.Close();
    }
}