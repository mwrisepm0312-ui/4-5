using DG.Tweening;
using FishNet;
using FishNet.Connection;
using FishNet.Transporting.Tugboat;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WaitingSceneUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI ipText;
    public TextMeshProUGUI connectedText;
    public TextMeshProUGUI statusText;

    private int connectedCount = 0;
    private int maxPlayers = 0;

    void Start()
    {
        ipText.text = $"IP: {GetLocalIP()}";

        var tugboat = InstanceFinder.NetworkManager.GetComponent<Tugboat>();
        if (tugboat != null)
            maxPlayers = tugboat.GetMaximumClients() - 1;

        if (InstanceFinder.ServerManager != null)
        {
            connectedCount = Mathf.Max(0, InstanceFinder.ServerManager.Clients.Count - 1);
            InstanceFinder.ServerManager.OnRemoteConnectionState += OnClientConnected;
        }

        UpdateUI();
    }

    void OnClientConnected(NetworkConnection conn,
        FishNet.Transporting.RemoteConnectionStateArgs args)
    {
        if (conn.ClientId == 0) return;

        if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Started)
        {
            connectedCount++;
            UpdateUI();
            DOTween.Restart("countPunch");

            StartCoroutine(LoadJobSelectScene(conn));

            if (connectedCount >= maxPlayers)
                StartCoroutine(LoadOpening());
        }
        else if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Stopped)
        {
            connectedCount = Mathf.Max(0, connectedCount - 1);
            UpdateUI();

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

    IEnumerator LoadOpening()
    {
        DOTween.Pause("statusFade");        // ← Fade Loop 멈추기
        statusText.DOFade(1f, 0.2f);        // ← 알파 원복
        statusText.text = "모든 인원 연결 완료! 오프닝 시작...";
        yield return new WaitForSeconds(1.0f);

        var gm2 = FindFirstObjectByType<GameManager>();
        gm2?.LoadJobSelectSceneServerRpc();

        SceneManager.LoadScene("OpeningScene");
    }

    void UpdateUI()
    {
        connectedText.text = $"연결된 인원: {connectedCount} / {maxPlayers}";
        statusText.text = connectedCount < maxPlayers
            ? "모바일 연결을 기다리는 중..."
            : "모든 인원 연결 완료!";
    }

    string GetLocalIP()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                    && ip.ToString().StartsWith("192.168"))
                    return ip.ToString();
            }
            return "IP 없음";
        }
        catch { return "IP 없음"; }
    }

    void OnDestroy()
    {
        DOTween.Kill("statusFade");  // ← 추가
        DOTween.Kill("countPunch");  // ← 추가

        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnRemoteConnectionState -= OnClientConnected;
    }
}