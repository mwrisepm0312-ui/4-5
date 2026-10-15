using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SuspectSceneUI : MonoBehaviour
{
    // ──────────────────────────────────────
    // 정답 설정
    // ──────────────────────────────────────
    [Header("정답 설정")]
    public string correctSuspect = "비서";
    public List<string> correctEvidences = new List<string>
    {
        "금고 지문",
        "비서 금고 앞 포착",
        "비서 복도 서성임",
        "비서 서류가방 수상 목격",
        "비서 레드 다이아 반지 시세 검색 기록"
    };

    // ──────────────────────────────────────
    // 용의자 카드 버튼 4개
    // ──────────────────────────────────────
    [Header("용의자 카드 버튼")]
    public Button selectBtn1;
    public Button selectBtn2;
    public Button selectBtn3;
    public Button selectBtn4;

    // ──────────────────────────────────────
    // 용의자 팝업
    // ──────────────────────────────────────
    [Header("용의자 팝업")]
    public GameObject suspectPopup;
    public Button closePopupButton;

    [Header("팝업 - 프로필")]
    public Image suspectPhoto;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI jobText;

    [Header("팝업 - 증거 슬롯 Image")]
    public Image evidenceSlot1;
    public Image evidenceSlot2;
    public Image evidenceSlot3;
    public Image evidenceSlot4;
    public Image evidenceSlot5;

    [Header("팝업 - 슬롯 텍스트")]
    public TextMeshProUGUI slotText1;
    public TextMeshProUGUI slotText2;
    public TextMeshProUGUI slotText3;
    public TextMeshProUGUI slotText4;
    public TextMeshProUGUI slotText5;

    [Header("팝업 - + 버튼")]
    public Button plusBtn1;
    public Button plusBtn2;
    public Button plusBtn3;
    public Button plusBtn4;
    public Button plusBtn5;

    [Header("팝업 - 검거 버튼")]
    public Button arrestButton;

    // ──────────────────────────────────────
    // 증거 목록 팝업 (스크롤뷰)
    // ──────────────────────────────────────
    [Header("증거 목록 팝업")]
    public GameObject evidenceListPopup;
    public Transform evidenceContent;
    public GameObject evidenceBtnTemplate;

    [Header("수신된 증거 팝업")]
    public Button evidenceListButton;
    public GameObject receivedEvidencePopup;
    public Transform receivedEvidenceContent;
    public GameObject receivedEvidenceTemplate;
    public Button closeReceivedEvidenceButton;

    // ──────────────────────────────────────
    // 용의자 데이터
    // ──────────────────────────────────────
    private string[] suspectNames = { "수집가", "경비원", "비서", "청소부" };
    private string[] suspectJobs = { "보석 수집가", "은행 경비원", "주얼리씨 비서", "은행 청소부" };


    // ──────────────────────────────────────
    // 상태 변수
    // ──────────────────────────────────────
    private string selectedSuspect = "";
    private string[] selectedEvidences = new string[5];
    private int currentPlusSlot = -1;

    // 수집된 증거 목록 (GameManager.ReceivedEvidences에서 받아옴)
    public List<string> collectedEvidences = new List<string>();

    // ──────────────────────────────────────
    // Start
    // ──────────────────────────────────────
    void Start()
    {
        suspectPopup.SetActive(false);
        evidenceListPopup.SetActive(false);

        // 용의자 카드 버튼
        selectBtn1.onClick.AddListener(() => OpenSuspectPopup(0));
        selectBtn2.onClick.AddListener(() => OpenSuspectPopup(1));
        selectBtn3.onClick.AddListener(() => OpenSuspectPopup(2));
        selectBtn4.onClick.AddListener(() => OpenSuspectPopup(3));

        // 닫기 버튼
        closePopupButton.onClick.AddListener(ClosePopup);

        // + 버튼
        plusBtn1.onClick.AddListener(() => OpenEvidenceList(0));
        plusBtn2.onClick.AddListener(() => OpenEvidenceList(1));
        plusBtn3.onClick.AddListener(() => OpenEvidenceList(2));
        plusBtn4.onClick.AddListener(() => OpenEvidenceList(3));
        plusBtn5.onClick.AddListener(() => OpenEvidenceList(4));

        // 검거 버튼
        arrestButton.onClick.AddListener(OnArrestClicked);

        // 템플릿 숨기기
        evidenceBtnTemplate.SetActive(false);

        // 수신된 증거 팝업 초기화
        if (receivedEvidencePopup != null)
            receivedEvidencePopup.SetActive(false);
        if (receivedEvidenceTemplate != null)
            receivedEvidenceTemplate.SetActive(false);
        if (evidenceListButton != null)
            evidenceListButton.onClick.AddListener(OpenReceivedEvidencePopup);
        if (closeReceivedEvidenceButton != null)
            closeReceivedEvidenceButton.onClick.AddListener(
                () => receivedEvidencePopup.SetActive(false));

        // GameManager에서 증거 로드
        collectedEvidences = new List<string>(GameManager.ReceivedEvidences);
    }

    // ──────────────────────────────────────
    // 용의자 팝업 열기
    // ──────────────────────────────────────
    void OpenSuspectPopup(int index)
    {
        selectedSuspect = suspectNames[index];

        nameText.text = $"이름: {suspectNames[index]}";
        jobText.text = $"직업: {suspectJobs[index]}";

        Sprite photo = Resources.Load<Sprite>($"Suspects/suspect_{index + 1}");
        if (photo != null)
            suspectPhoto.sprite = photo;

        ClearSlots();

        suspectPopup.SetActive(true);
        evidenceListPopup.SetActive(false);
    }

    void ClosePopup()
    {
        suspectPopup.SetActive(false);
        evidenceListPopup.SetActive(false);
    }

    // ──────────────────────────────────────
    // 증거 목록 팝업 열기 (동적 생성)
    // ──────────────────────────────────────
    void OpenEvidenceList(int slotIndex)
    {
        currentPlusSlot = slotIndex;

        // 기존 버튼 전부 삭제 (템플릿 제외)
        foreach (Transform child in evidenceContent)
        {
            if (child.gameObject != evidenceBtnTemplate)
                Destroy(child.gameObject);
        }

        // 수집된 증거만큼 버튼 동적 생성
        foreach (string evidence in collectedEvidences)
        {
            string ev = evidence; // 클로저 캡처용

            GameObject btn = Instantiate(evidenceBtnTemplate, evidenceContent);
            btn.SetActive(true);

            // 버튼 텍스트 설정
            btn.GetComponentInChildren<TextMeshProUGUI>().text = ev;

            // 버튼 클릭 이벤트
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                SelectEvidence(ev);
            });
        }

        evidenceListPopup.SetActive(true);
    }

    // ──────────────────────────────────────
    // 증거 선택 → 슬롯에 넣기
    // ──────────────────────────────────────
    void SelectEvidence(string evidence)
    {
        if (currentPlusSlot < 0) return;

        selectedEvidences[currentPlusSlot] = evidence;
        UpdateSlotTexts();

        evidenceListPopup.SetActive(false);
        currentPlusSlot = -1;
    }

    void UpdateSlotTexts()
    {
        slotText1.text = selectedEvidences[0] ?? "";
        slotText2.text = selectedEvidences[1] ?? "";
        slotText3.text = selectedEvidences[2] ?? "";
        slotText4.text = selectedEvidences[3] ?? "";
        slotText5.text = selectedEvidences[4] ?? "";
    }

    void ClearSlots()
    {
        for (int i = 0; i < selectedEvidences.Length; i++)
            selectedEvidences[i] = null;
        UpdateSlotTexts();
    }

    // ──────────────────────────────────────
    // 검거 버튼
    // ──────────────────────────────────────
    // ──────────────────────────────────────
    // GameManager에서 증거 수신 시 호출
    // ──────────────────────────────────────
    public void OnEvidencesReceived(List<string> evidences)
    {
        collectedEvidences = new List<string>(evidences);
        Debug.Log($"SuspectScene 증거 업데이트: {collectedEvidences.Count}개");
    }

    // ──────────────────────────────────────
    // 수신된 증거 팝업 열기
    // ──────────────────────────────────────
    void OpenReceivedEvidencePopup()
    {
        foreach (Transform child in receivedEvidenceContent)
        {
            if (child.gameObject != receivedEvidenceTemplate)
                Destroy(child.gameObject);
        }

        var evidences = GameManager.ReceivedEvidences;

        if (evidences.Count == 0)
        {
            GameObject item = Instantiate(receivedEvidenceTemplate, receivedEvidenceContent);
            item.SetActive(true);
            item.GetComponentInChildren<TextMeshProUGUI>().text = "아직 수신된 증거가 없습니다.";
            item.GetComponent<Button>().interactable = false;
        }
        else
        {
            foreach (string evidence in evidences)
            {
                GameObject item = Instantiate(receivedEvidenceTemplate, receivedEvidenceContent);
                item.SetActive(true);
                item.GetComponentInChildren<TextMeshProUGUI>().text = evidence;
                item.GetComponent<Button>().interactable = false;
            }
        }

        receivedEvidencePopup.SetActive(true);
    }

    void OnArrestClicked()
    {
        // 임시 디버그
        Debug.Log($"선택 용의자: [{selectedSuspect}]");
        Debug.Log($"정답 용의자: [{correctSuspect}]");
        for (int i = 0; i < selectedEvidences.Length; i++)
            Debug.Log($"선택 증거 {i}: [{selectedEvidences[i]}]");
        foreach (string c in correctEvidences)
            Debug.Log($"정답 증거: [{c}]");

        // 용의자 확인
        if (selectedSuspect != correctSuspect)
        {
            SceneManager.LoadScene("FailScene");
            return;
        }

        // 증거 확인
        foreach (string correct in correctEvidences)
        {
            bool found = false;
            foreach (string selected in selectedEvidences)
            {
                if (selected == correct)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                SceneManager.LoadScene("FailScene");
                return;
            }
        }

        SceneManager.LoadScene("ArrestScene");
    }



}