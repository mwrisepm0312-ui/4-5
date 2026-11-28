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
    // 오프닝이 끝나 직업 선택이 열렸는지 (나중에 다시 연결한 휴대폰에도 알려 주기 위해 서버가 기억)
    private bool jobSelectUnlocked = false;
    // 수사 시간이 끝났는지 (나중에 다시 연결한 휴대폰에도 '수사 끝'을 보여 주기 위해)
    private bool investigationEnded = false;
    // 서버가 받은 증거 (휴대폰에 '새 증거 몇 개 도착'을 알려 주기 위해)
    private readonly HashSet<string> serverEvidences = new HashSet<string>();

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
        // 인원 수·잠금 상태를 먼저 보내고, 그다음 이미 선택된 직업을 보냄 (순서 유지)
        SyncLobbyStateTargetRpc(sender, maxPlayers, jobSelectUnlocked);

        foreach (var job in selectedJobs)
        {
            UpdateJobStatusTargetRpc(sender, job.Key, true);
        }

        if (investigationEnded)
            InvestigationEndedTargetRpc(sender);
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

        int newCount = 0;
        foreach (string evidence in evidences)
        {
            if (serverEvidences.Add(evidence)) newCount++;
        }

        ReceiveEvidencesClientRpc(evidences);

        // 보낸 휴대폰에 '도착했어요' 알림
        if (sender != null)
            EvidenceArrivedTargetRpc(sender, newCount, serverEvidences.Count);
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

#if !MOBILE_CLIENT
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
#if MOBILE_CLIENT
        DataManager.Instance.MaxPlayers = count;

        // 이미 직업 선택 화면이면 다시 불러오지 않고 카드 배치만 바꿈 (화면 깜빡임 방지)
        var jobSelect = MobileJobSelect.Instance;
        if (jobSelect != null)
            jobSelect.ApplyMaxPlayers(count);
        else
            SceneTransition.Load("JobSelectScene");
#endif
    }

    [TargetRpc]
    void UpdateJobStatusTargetRpc(
        NetworkConnection conn, string jobName, bool isTaken)
    {
#if MOBILE_CLIENT
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
#if MOBILE_CLIENT
        MobileJobSelect.Instance?.OnJobRejected(jobName);
#endif
    }

    [TargetRpc]
    void ConfirmJobClientRpc(
        NetworkConnection conn, string jobName)
    {
        Debug.Log($"직업 확인: {jobName}");
#if MOBILE_CLIENT
        MobileJobSelect.Instance?.OnJobConfirmed(jobName);
#endif
    }

    [ObserversRpc]
    void UpdateJobStatusClientRpc(
        string jobName, bool isTaken)
    {
        Debug.Log($"직업 상태 업데이트: {jobName} = {isTaken}");
#if MOBILE_CLIENT
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
#if MOBILE_CLIENT
        SceneTransition.Load("JobSelectScene");
#endif
    }

    // ──────────────────────────────────────
    // 직업 선택 잠금 해제 RPC (오프닝 완료 시)
    // ──────────────────────────────────────
    [ServerRpc(RequireOwnership = false)]
    public void UnlockJobSelectServerRpc(
        NetworkConnection sender = null)
    {
        jobSelectUnlocked = true;
        UnlockJobSelectClientRpc();
    }

    [ObserversRpc]
    void UnlockJobSelectClientRpc()
    {
#if MOBILE_CLIENT
        MobileJobSelect.Instance?.UnlockJobSelect();
#endif
    }

    // ──────────────────────────────────────
    // 게임 종료 후 모바일 로비씬 이동
    // 서버에서 직접 호출 (ServerRpc 체인 없이)
    // ──────────────────────────────────────
    public void SendClientToLobbyFromServer()
    {
        // 서버에서 직접 ObserversRpc 호출
        SendClientToLobbyClientRpc();
    }

    [ObserversRpc]
    void SendClientToLobbyClientRpc()
    {
        Debug.Log("[GameManager] SendClientToLobbyClientRpc 수신");
#if MOBILE_CLIENT
        if (DataManager.Instance != null) DataManager.Instance.RoundEnded = true;   // 정상 종료 (끊김 아님)
        UnityEngine.SceneManagement.SceneManager.LoadScene("ClientMobile");
#endif
    }

    // 기존 호환용 (사용 안 해도 됨)
    [ServerRpc(RequireOwnership = false)]
    public void LoadClientSceneServerRpc(NetworkConnection sender = null)
    {
        LoadClientSceneClientRpc();
    }

    [ObserversRpc]
    void LoadClientSceneClientRpc()
    {
#if MOBILE_CLIENT
        if (DataManager.Instance != null) DataManager.Instance.RoundEnded = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene("ClientMobile");
#endif
    }

    // ──────────────────────────────────────
    // 직업 선택 화면 상태 맞추기 (서버 → 요청한 휴대폰 1대)
    // 수사 도중 다시 연결한 휴대폰이 '오프닝 진행 중' 화면에 갇히지 않도록
    // 새 RPC는 기존 RPC 순서가 바뀌지 않게 항상 맨 아래에 추가
    // ──────────────────────────────────────
    [TargetRpc]
    void SyncLobbyStateTargetRpc(
        NetworkConnection conn, int count, bool unlocked)
    {
#if MOBILE_CLIENT
        var jobSelect = MobileJobSelect.Instance;
        if (jobSelect == null) return;

        if (count >= 2 && count <= 4 && DataManager.Instance.MaxPlayers != count)
        {
            DataManager.Instance.MaxPlayers = count;
            jobSelect.ApplyMaxPlayers(count);
        }

        if (unlocked)
            jobSelect.UnlockJobSelect();
#endif
    }

    // ──────────────────────────────────────
    // 수사 시간 끝 알림 (PC → 모든 휴대폰)
    // PC 로비에서 용의자 선택으로 넘어갈 때 서버에서 직접 호출
    // ──────────────────────────────────────
    public void NotifyInvestigationEndFromServer()
    {
        if (!IsServerStarted) return;
        investigationEnded = true;
        InvestigationEndedClientRpc();
    }

    [ObserversRpc]
    void InvestigationEndedClientRpc()
    {
#if MOBILE_CLIENT
        OnInvestigationEndedMobile();
#endif
    }

    // 수사가 끝난 뒤 다시 연결한 휴대폰 1대에게
    [TargetRpc]
    void InvestigationEndedTargetRpc(NetworkConnection conn)
    {
#if MOBILE_CLIENT
        OnInvestigationEndedMobile();
#endif
    }

    // ──────────────────────────────────────
    // 증거 도착 알림 (PC → 보낸 휴대폰 1대)
    // ──────────────────────────────────────
    [TargetRpc]
    void EvidenceArrivedTargetRpc(
        NetworkConnection conn, int newCount, int totalCount)
    {
#if MOBILE_CLIENT
        string message = newCount > 0
            ? $"PC에 증거가 도착했어요!\n새 증거 {newCount}개"
            : "PC에 이미 있는 증거예요.";
        MobileSession.ShowToast(message);

        var lobby = FindFirstObjectByType<MobileLobbyUI>();
        if (lobby != null) lobby.OnEvidenceArrived(newCount);
#endif
    }

#if MOBILE_CLIENT
    void OnInvestigationEndedMobile()
    {
        // 아직 보내지 않은 증거가 있으면 자동으로 PC에 보냄 (빠뜨리지 않도록)
        var dm = DataManager.Instance;
        if (dm != null && dm.CollectedEvidences.Count > 0)
            ShareEvidencesServerRpc(dm.CollectedEvidences.ToArray());

        MobileSession.ShowBlocking("수사 끝!", "PC 화면을 봐 주세요");
    }
#endif
}