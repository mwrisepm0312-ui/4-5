using FishNet;
using FishNet.Object;
using FishNet.Connection;
using UnityEngine;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    private Dictionary<string, int> selectedJobs
        = new Dictionary<string, int>();
    private Dictionary<int, string> clientJobs
        = new Dictionary<int, string>();
    private int maxPlayers = 0;

    // PC에서 수신한 증거 목록
    public static List<string> ReceivedEvidences = new List<string>();

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SelectJobServerRpc(
        string jobName,
        NetworkConnection sender = null)
    {
        if (selectedJobs.ContainsKey(jobName))
        {
            RejectJobClientRpc(sender, jobName);
            return;
        }

        selectedJobs[jobName] = sender.ClientId;
        clientJobs[sender.ClientId] = jobName;
        Debug.Log($"직업 선택됨: {jobName}");

        UpdateJobStatusClientRpc(jobName, true);
        ConfirmJobClientRpc(sender, jobName);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestJobStatusServerRpc(
        NetworkConnection sender = null)
    {
        foreach (var job in selectedJobs)
        {
            UpdateJobStatusTargetRpc(sender, job.Key, true);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMaxPlayersAndLoadSceneForConnServerRpc(
        int count, int targetClientId,
        NetworkConnection sender = null)
    {
        maxPlayers = count;

        if (InstanceFinder.ServerManager.Clients
            .TryGetValue(targetClientId,
                out NetworkConnection conn))
        {
            SetMaxPlayersAndLoadSceneTargetRpc(conn, count);
        }
    }

    // ──────────────────────────────────────
    // 증거 송신 RPC (모바일 → PC)
    // ──────────────────────────────────────
    [ServerRpc(RequireOwnership = false)]
    public void ShareEvidencesServerRpc(
        string[] evidences,
        NetworkConnection sender = null)
    {
        Debug.Log($"증거 수신: {evidences.Length}개");
        ReceiveEvidencesClientRpc(evidences);
    }

    [ObserversRpc]
    void ReceiveEvidencesClientRpc(string[] evidences)
    {
        foreach (string evidence in evidences)
        {
            if (!ReceivedEvidences.Contains(evidence))
                ReceivedEvidences.Add(evidence);
        }
        Debug.Log($"증거 수신 완료: {ReceivedEvidences.Count}개");

#if !UNITY_ANDROID
        // PC에서만 SuspectScene 업데이트
        var suspectScene = FindFirstObjectByType<SuspectSceneUI>();
        suspectScene?.OnEvidencesReceived(ReceivedEvidences);
#endif
    }

    // ──────────────────────────────────────
    // PC 단독 테스트용
    // ──────────────────────────────────────
    public static void AddEvidenceDirectly(string evidence)
    {
        if (!ReceivedEvidences.Contains(evidence))
            ReceivedEvidences.Add(evidence);
        Debug.Log($"증거 직접 추가: {evidence}");
    }

    [TargetRpc]
    void SetMaxPlayersAndLoadSceneTargetRpc(
        NetworkConnection conn, int count)
    {
#if UNITY_ANDROID
        DataManager.Instance.MaxPlayers = count;
        UnityEngine.SceneManagement.SceneManager
            .LoadScene("JobSelectScene");
#endif
    }

    [TargetRpc]
    void UpdateJobStatusTargetRpc(
        NetworkConnection conn, string jobName, bool isTaken)
    {
#if UNITY_ANDROID
        MobileJobSelect.Instance?.OnJobStatusUpdated(jobName, isTaken);
#endif
    }

    public void OnClientDisconnected(int clientId)
    {
        if (clientJobs.ContainsKey(clientId))
        {
            string jobName = clientJobs[clientId];
            selectedJobs.Remove(jobName);
            clientJobs.Remove(clientId);
            Debug.Log($"직업 해제됨: {jobName}");
            UpdateJobStatusClientRpc(jobName, false);
        }
    }

    [TargetRpc]
    void RejectJobClientRpc(
        NetworkConnection conn, string jobName)
    {
        Debug.Log($"직업 거부: {jobName}");
#if UNITY_ANDROID
        MobileJobSelect.Instance?.OnJobRejected(jobName);
#endif
    }

    [TargetRpc]
    void ConfirmJobClientRpc(
        NetworkConnection conn, string jobName)
    {
        Debug.Log($"직업 확인: {jobName}");
#if UNITY_ANDROID
        MobileJobSelect.Instance?.OnJobConfirmed(jobName);
#endif
    }

    [ObserversRpc]
    void UpdateJobStatusClientRpc(
        string jobName, bool isTaken)
    {
        Debug.Log($"직업 상태 업데이트: {jobName} = {isTaken}");
#if UNITY_ANDROID
        MobileJobSelect.Instance?.OnJobStatusUpdated(jobName, isTaken);
#endif
    }

    // ──────────────────────────────────────
    // 모바일 JobSelectScene 이동 RPC
    // ──────────────────────────────────────
    [ServerRpc(RequireOwnership = false)]
    public void LoadJobSelectSceneServerRpc(
        NetworkConnection sender = null)
    {
        LoadJobSelectSceneClientRpc();
    }

    [ObserversRpc]
    void LoadJobSelectSceneClientRpc()
    {
#if UNITY_ANDROID
        UnityEngine.SceneManagement.SceneManager
            .LoadScene("JobSelectScene");
#endif
    }

    // ──────────────────────────────────────
    // 직업 선택 잠금 해제 RPC (오프닝 완료 시)
    // ──────────────────────────────────────
    [ServerRpc(RequireOwnership = false)]
    public void UnlockJobSelectServerRpc(
        NetworkConnection sender = null)
    {
        UnlockJobSelectClientRpc();
    }

    [ObserversRpc]
    void UnlockJobSelectClientRpc()
    {
#if UNITY_ANDROID
        MobileJobSelect.Instance?.UnlockJobSelect();
#endif
    }
}