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
        if (!DataManager.Instance.TutorialShown)
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
            item.GetComponentInChildren<TextMeshProUGUI>().text = "아직 수집한 증거가 없습니다.";
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
    }

    void OnSendClicked()
    {
        var evidences = DataManager.Instance.CollectedEvidences;
        if (evidences.Count == 0)
        {
            if (sendStatusText != null) sendStatusText.text = "수집한 증거가 없습니다!";
            return;
        }
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.ShareEvidencesServerRpc(evidences.ToArray());
            if (sendStatusText != null) sendStatusText.text = "PC로 송신 완료!";
            sendButton.interactable = false;
        }
        else
        {
            if (sendStatusText != null) sendStatusText.text = "연결 오류! 다시 시도해주세요.";
        }
    }

    void CloseEvidencePopup() => evidencePopup.SetActive(false);

    void OnDestroy()
    {
        if (renderTexture != null) { renderTexture.Release(); renderTexture = null; }
    }
}