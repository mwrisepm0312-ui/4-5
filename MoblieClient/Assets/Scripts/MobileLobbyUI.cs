using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public class MobileLobbyUI : MonoBehaviour
{
    [Header("직업 표시")]
    public TextMeshProUGUI jobText;

    [Header("증거 목록 버튼")]
    public Button evidenceButton;

    [Header("증거 팝업")]
    public GameObject evidencePopup;
    public Transform evidenceContent;
    public GameObject evidenceItemTemplate;
    public Button sendButton;
    public Button closeButton;
    public TextMeshProUGUI sendStatusText;

    [Header("튜토리얼 팝업")]
    public GameObject tutorialPopup;
    public VideoPlayer videoPlayer;
    public RawImage videoDisplay;
    public Button tutorialConfirmButton;

    private RenderTexture renderTexture;

    void Start()
    {
        if (DataManager.Instance != null)
        {
            string job = DataManager.Instance.SelectedJob;
            jobText.text = $"내 직업: {job}";
        }

        evidencePopup.SetActive(false);
        evidenceItemTemplate.SetActive(false);
        if (sendStatusText != null) sendStatusText.text = "";

        evidenceButton.onClick.AddListener(OpenEvidencePopup);
        sendButton.onClick.AddListener(OnSendClicked);
        closeButton.onClick.AddListener(CloseEvidencePopup);
        tutorialConfirmButton.onClick.AddListener(CloseTutorialPopup);

        SetupVideo();

        // 처음 한 번만 튜토리얼 표시
        // (전시 모드는 NFC 대신 화면의 카드를 누르므로 'NFC 태그하는 방법' 튜토리얼을 보여 주지 않음)
        if (!DataManager.Instance.TutorialShown && !ExhibitionMode.IsOn)
        {
            ShowTutorialPopup();
        }
        else
        {
            tutorialPopup.SetActive(false);
        }
    }

    void CloseTutorialPopup()
    {
        DataManager.Instance.TutorialShown = true;
        DOTween.PlayBackwards("tutorialOpen");
        DOVirtual.DelayedCall(0.35f, () =>
        {
            tutorialPopup.SetActive(false);
            videoPlayer?.Stop();
        });
    }

    void SetupVideo()
    {
        if (videoPlayer == null || videoDisplay == null) return;
        renderTexture = new RenderTexture(1280, 720, 0);
        videoPlayer.targetTexture = renderTexture;
        videoDisplay.texture = renderTexture;
        videoPlayer.isLooping = true;
        videoPlayer.playOnAwake = false;
    }

    void ShowTutorialPopup()
    {
        tutorialPopup.SetActive(true);
        DOTween.Restart("tutorialOpen");
        videoPlayer?.Play();
    }

    

    // ──────────────────────────────────────
    void OpenEvidencePopup()
    {
        foreach (Transform child in evidenceContent)
            if (child.gameObject != evidenceItemTemplate)
                Destroy(child.gameObject);

        if (sendStatusText != null) sendStatusText.text = "";

        var evidences = DataManager.Instance.CollectedEvidences;
        if (evidences.Count == 0)
        {
            GameObject item = Instantiate(evidenceItemTemplate, evidenceContent);
            item.SetActive(true);
            item.GetComponentInChildren<TextMeshProUGUI>().text = "아직 수집한 증거가 없어요.\n미션을 해결해 증거를 모아 보세요!";
            item.GetComponent<Button>().interactable = false;
        }
        else
        {
            foreach (string evidence in evidences)
            {
                GameObject item = Instantiate(evidenceItemTemplate, evidenceContent);
                item.SetActive(true);
                item.GetComponentInChildren<TextMeshProUGUI>().text = evidence;
                item.GetComponent<Button>().interactable = false;
            }
        }
        evidencePopup.SetActive(true);

        // 내 미션을 모두 끝내야 보낼 수 있음
        UpdateSendAvailability();
    }

    // 내 직업 미션을 다 끝냈는지에 따라 [증거 보내기] 버튼을 켜고 끔
    bool UpdateSendAvailability()
    {
        var remaining = MissionRules.RemainingMissions(DataManager.Instance);
        bool ready = remaining.Count == 0;
        sendButton.interactable = ready;

        if (!ready && sendStatusText != null)
        {
            var names = new System.Collections.Generic.List<string>();
            foreach (string m in remaining) names.Add(MissionRules.DisplayName(m));
            sendStatusText.text = $"남은 미션: {string.Join(" · ", names)}";
        }
        return ready;
    }

    void OnSendClicked()
    {
        var evidences = DataManager.Instance.CollectedEvidences;
        if (evidences.Count == 0)
        {
            if (sendStatusText != null) sendStatusText.text = "보낼 증거가 아직 없어요.";
            return;
        }
        if (!UpdateSendAvailability()) return;   // 내 미션을 다 끝내야 보낼 수 있음
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null && gm.IsClientInitialized)
        {
            gm.ShareEvidencesServerRpc(evidences.ToArray());
            if (sendStatusText != null) sendStatusText.text = "PC로 보내는 중...";
            sendButton.interactable = false;

            // PC에서 '도착' 알림이 3초 안에 안 오면 다시 보낼 수 있게
            evidenceArrived = false;
            if (sendWaitRoutine != null) StopCoroutine(sendWaitRoutine);
            sendWaitRoutine = StartCoroutine(WaitForArrival());
        }
        else
        {
            if (sendStatusText != null) sendStatusText.text = "보내지 못했어요. 다시 시도해 주세요.";
        }
    }

    private bool evidenceArrived = false;
    private Coroutine sendWaitRoutine;

    System.Collections.IEnumerator WaitForArrival()
    {
        yield return new WaitForSeconds(3f);
        if (!evidenceArrived)
        {
            if (sendStatusText != null) sendStatusText.text = "보내지 못했어요. 다시 시도해 주세요.";
            if (sendButton != null) sendButton.interactable = true;
        }
        sendWaitRoutine = null;
    }

    // PC가 증거를 받으면 GameManager가 불러 줌
    public void OnEvidenceArrived(int newCount)
    {
        evidenceArrived = true;
        if (sendStatusText != null)
            sendStatusText.text = newCount > 0 ? "PC에 도착했어요!" : "이미 PC에 있는 증거예요.";
    }

    void CloseEvidencePopup() => evidencePopup.SetActive(false);

    void OnDestroy()
    {
        if (renderTexture != null) { renderTexture.Release(); renderTexture = null; }
    }
}