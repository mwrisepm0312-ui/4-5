using FishNet.Object;
using FishNet.Connection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class JobNetworkHelper : NetworkBehaviour
{
    public static JobNetworkHelper Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SelectJobServerRpc(
        string jobName,
        NetworkConnection sender = null)
    {
        Debug.Log($"Á÷¾÷ ¼±ÅÃµÊ: {jobName}");
        ConfirmJobClientRpc(sender, jobName);
        UpdateJobStatusClientRpc(jobName, true);
    }

    [TargetRpc]
    void ConfirmJobClientRpc(
        NetworkConnection conn, string jobName)
    {
        MobileJobSelect.Instance?.OnJobConfirmed(jobName);
    }

    [TargetRpc]
    void RejectJobClientRpc(
        NetworkConnection conn, string jobName)
    {
        MobileJobSelect.Instance?.OnJobRejected(jobName);
    }

    [ObserversRpc]
    void UpdateJobStatusClientRpc(
        string jobName, bool isTaken)
    {
        MobileJobSelect.Instance?.OnJobStatusUpdated(jobName, isTaken);
    }
}