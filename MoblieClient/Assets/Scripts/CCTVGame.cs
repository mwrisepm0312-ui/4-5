using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CCTVGame : MonoBehaviour
{
    // ──────────────────────────────────────
    // 시간대 데이터
    // ──────────────────────────────────────
    private string[] timeLabels = { "23:00", "23:30", "00:00", "00:30", "01:00" };

    // [시간대][모니터] 스프라이트 이름
    // 0=금고앞 1=1층로비 2=은행복도 3=은행외부
    private string[,] spriteNames = new string[5, 4]
    {
        // 23:00: 수집가(금고앞) / 경비원순찰(로비) / 빔 / 청소부퇴근(외부)
        { "monitor1_2300", "monitor2_2300", "monitor3_2300", "monitor4_2300" },
        // 23:30: 빔 / 빔(경비원없음) / 빔 / 수집가(외부)
        { "monitor1_2330", "monitor2_empty", "monitor3_2330", "monitor4_2330" },
        // 00:00: 전부 빔
        { "monitor1_0000", "monitor2_0000", "monitor3_0000", "monitor4_0000" },
        // 00:30: 비서(금고앞) / 빔 / 빔 / 빔
        { "monitor1_0030", "monitor2_0030", "monitor3_0030", "monitor4_0030" },
        // 01:00: 경비원(금고앞) / 빔 / 비서(복도) / 빔
        { "monitor1_0100", "monitor2_0100", "monitor3_0100", "monitor4_0100" },
    };

    // [시간대][모니터] 증거명 (null = 증거 없음)
    private string[,] evidenceNames = new string[5, 4]
    {
        // 23:00
        { "수집가 금고 앞 배회", "경비원 정상 순찰 확인", null, "청소부 퇴근 포착" },
        // 23:30: 로비 경비원 없음
        { null, null, null, "수집가 은행 외부 배회" },
        // 00:00
        { null, null, null, null },
        // 00:30
        { "비서 금고 앞 포착", null, null, null },
        // 01:00: 경비원(금고앞) + 비서(복도)
        { "경비원 01:00 금고 앞 순찰", null, "비서 복도 서성임", null },
    };

    // [시간대][모니터] 증거 설명
    private string[,] evidenceDescs = new string[5, 4]
    {
        // 23:00
        {
            "23:00 금고 앞에서\n수집가가 배회하는 것이 포착됐습니다.",
            "23:00 경비원이\n정상적으로 순찰 중임이 확인됐습니다.",
            null,
            "23:00 청소부가\n은행을 퇴근하는 것이 포착됐습니다."
        },
        // 23:30
        { null, null, null, "23:30 수집가가\n은행 외부를 배회하는 것이 포착됐습니다." },
        // 00:00
        { null, null, null, null },
        // 00:30
        { "00:30 비서가\n금고 앞에서 포착됐습니다!\n범행 시간대와 일치합니다.", null, null, null },
        // 01:00
        { "01:00 경비원이\n금고 앞을 순찰하는 것이 포착됐습니다.", null, "01:00 비서가\n서류가방을 꽉 안고\n은행 복도를 서성이는 것이 포착됐습니다!", null },
    };

    // [시간대][모니터] 진짜 증거 여부
    private bool[,] isRealEvidence = new bool[5, 4]
    {
        { false, false, false, false }, // 23:00: 가짜/피해자
        { false, false, false, false }, // 23:30: 가짜
        { false, false, false, false }, // 00:00: 없음
        { true,  false, false, false }, // 00:30: 비서★
        { false, false, true,  false }, // 01:00: 비서★
    };

    // ──────────────────────────────────────
    // 가이드 팝업
    // ──────────────────────────────────────
    [Header("가이드 팝업")]
    public GameObject guidePopup;
    public Button startButton;

    [Header("슬라이더 힌트")]
    public GameObject sliderHintText;   // TextMeshPro 오브젝트

    // ──────────────────────────────────────
    // 모니터 4개
    // ──────────────────────────────────────
    [Header("모니터 Image")]
    public Image monitor1;
    public Image monitor2;
    public Image monitor3;
    public Image monitor4;

    [Header("모니터 Button")]
    public Button monitorBtn1;
    public Button monitorBtn2;
    public Button monitorBtn3;
    public Button monitorBtn4;

    // ──────────────────────────────────────
    // 슬라이더
    // ──────────────────────────────────────
    [Header("시간 슬라이더")]
    public Slider timeSlider;
    public TextMeshProUGUI currentTimeText;

    // ──────────────────────────────────────
    // 증거 팝업
    // ──────────────────────────────────────
    [Header("증거 팝업")]
    public GameObject evidencePopup;
    public TextMeshProUGUI evidenceTitleText;
    public TextMeshProUGUI evidenceDescText;
    public Button confirmButton;

    // ──────────────────────────────────────
    // 미션 완료
    // ──────────────────────────────────────
    [Header("미션 완료")]
    public GameObject clearTitleText;
    public GameObject clearDescText;
    public TextMeshProUGUI countdownText;

    // ──────────────────────────────────────
    // 상태 변수
    // ──────────────────────────────────────
    private int currentTimeSlot = 0;
    private bool[,] collectedEvidence = new bool[5, 4];
    private int totalEvidenceCount = 0;
    private int maxEvidence = 7;
    private Sprite[,] sprites = new Sprite[5, 4];

    // ──────────────────────────────────────
    // Start
    // ──────────────────────────────────────
    void Start()
    {
        guidePopup.SetActive(true);
        evidencePopup.SetActive(false);
        clearTitleText.SetActive(false);
        clearDescText.SetActive(false);
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        // 모니터 버튼 / 슬라이더 비활성화 (가이드 전)
        SetInteractable(false);

        // 슬라이더 설정
        timeSlider.minValue = 0;
        timeSlider.maxValue = 4;
        timeSlider.wholeNumbers = true;
        timeSlider.value = 0;
        timeSlider.onValueChanged.AddListener(OnSliderChanged);

        // 버튼 이벤트
        startButton.onClick.AddListener(OnStartClicked);
        confirmButton.onClick.AddListener(OnConfirmClicked);
        monitorBtn1.onClick.AddListener(() => OnMonitorClicked(0));
        monitorBtn2.onClick.AddListener(() => OnMonitorClicked(1));
        monitorBtn3.onClick.AddListener(() => OnMonitorClicked(2));
        monitorBtn4.onClick.AddListener(() => OnMonitorClicked(3));

        // 스프라이트 로드
        LoadSprites();

        // 초기 시간대
        UpdateMonitors(0);
        currentTimeText.text = $"현재 시간: {timeLabels[0]}";
    }

    // ──────────────────────────────────────
    // 버튼/슬라이더 활성화 제어
    // ──────────────────────────────────────
    void SetInteractable(bool value)
    {
        monitorBtn1.interactable = value;
        monitorBtn2.interactable = value;
        monitorBtn3.interactable = value;
        monitorBtn4.interactable = value;
        timeSlider.interactable = value;
    }

    // ──────────────────────────────────────
    // 스프라이트 로드
    // ──────────────────────────────────────
    void LoadSprites()
    {
        for (int t = 0; t < 5; t++)
            for (int m = 0; m < 4; m++)
                sprites[t, m] = Resources.Load<Sprite>($"CCTV/{spriteNames[t, m]}");
    }

    // ──────────────────────────────────────
    // 가이드 팝업 시작 버튼
    // ──────────────────────────────────────
    void OnStartClicked()
    {
        guidePopup.SetActive(false);
        SetInteractable(true);
    }

    // ──────────────────────────────────────
    // 슬라이더 변경
    // ──────────────────────────────────────
    void OnSliderChanged(float value)
    {
        currentTimeSlot = (int)value;
        currentTimeText.text = $"현재 시간: {timeLabels[currentTimeSlot]}";
        UpdateMonitors(currentTimeSlot);

        // 처음 드래그 시 힌트 숨기기
        if (sliderHintText != null && sliderHintText.activeSelf)
        {
            DOTween.Pause("sliderHint");
            sliderHintText.SetActive(false);
        }
    }

    // ──────────────────────────────────────
    // 모니터 이미지 업데이트
    // ──────────────────────────────────────
    void UpdateMonitors(int timeSlot)
    {
        if (sprites[timeSlot, 0] != null) monitor1.sprite = sprites[timeSlot, 0];
        if (sprites[timeSlot, 1] != null) monitor2.sprite = sprites[timeSlot, 1];
        if (sprites[timeSlot, 2] != null) monitor3.sprite = sprites[timeSlot, 2];
        if (sprites[timeSlot, 3] != null) monitor4.sprite = sprites[timeSlot, 3];
    }

    // ──────────────────────────────────────
    // 모니터 클릭
    // ──────────────────────────────────────
    void OnMonitorClicked(int monitorIndex)
    {
        string evidence = evidenceNames[currentTimeSlot, monitorIndex];
        string desc = evidenceDescs[currentTimeSlot, monitorIndex];

        // 증거 없음
        if (evidence == null)
        {
            evidenceTitleText.text = "수상한 인물 없음";
            evidenceDescText.text = $"{timeLabels[currentTimeSlot]}\n이 시간대에는 수상한 인물이 없습니다.";
            evidencePopup.SetActive(true);
            return;
        }

        // 이미 수집
        if (collectedEvidence[currentTimeSlot, monitorIndex])
        {
            evidenceTitleText.text = "이미 수집한 증거";
            evidenceDescText.text = $"{evidence}\n이미 수집한 증거입니다.";
            evidencePopup.SetActive(true);
            return;
        }

        // 새 증거 수집
        collectedEvidence[currentTimeSlot, monitorIndex] = true;
        totalEvidenceCount++;

        evidenceTitleText.text = "증거 발견!";
        evidenceDescText.text = desc;
        evidencePopup.SetActive(true);

        if (DataManager.Instance != null)
        {
            if (!DataManager.Instance.CollectedEvidences.Contains(evidence))
                DataManager.Instance.CollectedEvidences.Add(evidence);
        }

        // 7개 모두 수집 시 미션 완료
        if (totalEvidenceCount >= maxEvidence)
        {
            // 버튼 비활성화
            SetInteractable(false);
            StartCoroutine(ShowClearPopup());
        }
    }

    // ──────────────────────────────────────
    // 증거 팝업 확인
    // ──────────────────────────────────────
    void OnConfirmClicked()
    {
        evidencePopup.SetActive(false);
    }

    // ──────────────────────────────────────
    // 미션 완료 팝업 + 카운트다운
    // ──────────────────────────────────────
    IEnumerator ShowClearPopup()
    {
        yield return new WaitForSeconds(0.5f);
        evidencePopup.SetActive(false);
        clearTitleText.SetActive(true);
        clearDescText.SetActive(true);
        countdownText.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        SceneManager.LoadScene("Mobile_LobbyScene");
    }
}