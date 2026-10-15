using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class ForensicGame : MonoBehaviour
{
    // ──────────────────────────────────────
    // 배경 및 딤
    // ──────────────────────────────────────
    [Header("배경 및 딤")]
    public GameObject dimBackground;

    // ──────────────────────────────────────
    // ! 버튼 3개
    // ──────────────────────────────────────
    [Header("느낌표 버튼")]
    public Button exclamationBtn1;
    public Button exclamationBtn2;
    public Button exclamationBtn3;

    // ──────────────────────────────────────
    // BrushArea 3개
    // ──────────────────────────────────────
    [Header("브러시 영역")]
    public RectTransform brushArea1;
    public RectTransform brushArea2;
    public RectTransform brushArea3;

    // ──────────────────────────────────────
    // FingerprintImage 3개
    // ──────────────────────────────────────
    [Header("지문 이미지")]
    public GameObject fingerprintImage1;  // 비서 (진짜)
    public GameObject fingerprintImage2;  // 청소부 (가짜)
    public GameObject fingerprintImage3;  // 경비원 (가짜)

    // ──────────────────────────────────────
    // 브러시 설정
    // ──────────────────────────────────────
    [Header("브러시 설정")]
    public GameObject brushCirclePrefab;
    public Transform brushContainer;
    public int brushCountToReveal = 30;
    public float minBrushDistance = 20f;

    // ──────────────────────────────────────
    // 게임 가이드 팝업
    // ──────────────────────────────────────
    [Header("게임 가이드 팝업")]
    public GameObject guidePopup;
    public Button startButton;

    // ──────────────────────────────────────
    // 드래그 가이드 화살표
    // ──────────────────────────────────────
    [Header("드래그 가이드")]
    public GameObject dragArrow;        // 빈 오브젝트 (자식: Arrow1~4)
    private GameObject[] arrows;        // 자식 화살표 자동 수집

    // ──────────────────────────────────────
    // 가루 뿌리기 버튼
    // ──────────────────────────────────────
    [Header("가루 뿌리기 버튼")]
    public Button powderButton;
    public TextMeshProUGUI powderBtnText;

    // ──────────────────────────────────────
    // 지문 발견 팝업
    // ──────────────────────────────────────
    [Header("지문 발견 팝업")]
    public GameObject evidencePopup;
    public Image fingerprintPopupImage;
    public TextMeshProUGUI popupTitleText;
    public TextMeshProUGUI popupDescText;
    public Button confirmButton;

    // ──────────────────────────────────────
    // AFIS 패널
    // ──────────────────────────────────────
    [Header("AFIS 패널")]
    public GameObject afisPanel;
    public Image foundFingerprintImage;
    public TextMeshProUGUI afisDescText;
    public TextMeshProUGUI resultText;

    [Header("드래그 가능한 발견 지문")]
    public DraggableFingerprint draggableFingerprint;

    [Header("용의자 지문 이미지")]
    public Image fpImage1;
    public Image fpImage2;
    public Image fpImage3;
    public Image fpImage4;

    [Header("지문 스프라이트")]
    public Sprite foundSprite;
    public Sprite fpSprite1;
    public Sprite fpSprite2;
    public Sprite fpSprite3;
    public Sprite fpSprite4;

    // ──────────────────────────────────────
    // 문지르기 게이지
    // ──────────────────────────────────────
    [Header("문지르기 게이지")]
    public GameObject gaugeBackground;
    public Image gaugeFill;

    // ──────────────────────────────────────
    // 미션 완료
    // ──────────────────────────────────────
    [Header("미션 완료")]
    public GameObject clearTitleText;
    public GameObject clearDescText;
    public TextMeshProUGUI countdownText;

    // ──────────────────────────────────────
    // 정답 데이터
    // ──────────────────────────────────────
    // correctAnswers[location] → 해당 위치 정답 용의자 이름
    // correctSuspectIndex[location] → fpImage 인덱스 (0=비서, 1=청소부, 2=경비원, 3=수집가)
    private string[] correctAnswers = { "비서", "청소부", "경비원" };
    private int[] correctSuspectIndex = { 0, 1, 2 }; // fpImage1=0, fpImage2=1, fpImage3=2, fpImage4=3
    private string[] evidenceNames = { "금고 지문", "바닥 지문", "데스크 지문" };
    private Sprite[] locationSprites;

    // ──────────────────────────────────────
    // 상태 변수
    // ──────────────────────────────────────
    private int currentLocation = -1;
    private bool powderSpread = false;
    private bool fingerFound = false;
    private int brushCount = 0;
    private Vector2 lastBrushPos = Vector2.zero;
    private RectTransform currentBrushArea;
    private GameObject currentFingerprintImage;
    private List<bool> locationCleared = new List<bool> { false, false, false };

    // ──────────────────────────────────────
    // Start
    // ──────────────────────────────────────
    void Start()
    {
        guidePopup.SetActive(true);
        exclamationBtn1.gameObject.SetActive(false);
        exclamationBtn2.gameObject.SetActive(false);
        exclamationBtn3.gameObject.SetActive(false);
        startButton.onClick.AddListener(OnStartClicked);
        dimBackground.SetActive(false);
        fingerprintImage1.SetActive(false);
        fingerprintImage2.SetActive(false);
        fingerprintImage3.SetActive(false);
        powderButton.gameObject.SetActive(false);
        evidencePopup.SetActive(false);
        afisPanel.SetActive(false);
        clearTitleText.SetActive(false);
        clearDescText.SetActive(false);
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
        gaugeBackground.SetActive(false);
        if (gaugeFill != null) gaugeFill.fillAmount = 0f;

        // 스프라이트 로드
        if (foundSprite == null) foundSprite = Resources.Load<Sprite>("Fingerprints/found_fingerprint");
        if (fpSprite1 == null) fpSprite1 = Resources.Load<Sprite>("Fingerprints/fingerprint_1");
        if (fpSprite2 == null) fpSprite2 = Resources.Load<Sprite>("Fingerprints/fingerprint_2");
        if (fpSprite3 == null) fpSprite3 = Resources.Load<Sprite>("Fingerprints/fingerprint_3");
        if (fpSprite4 == null) fpSprite4 = Resources.Load<Sprite>("Fingerprints/fingerprint_4");

        locationSprites = new Sprite[] { fpSprite3, fpSprite4, fpSprite2 };

        if (fpImage1 != null && fpSprite1 != null) fpImage1.sprite = fpSprite1;
        if (fpImage2 != null && fpSprite2 != null) fpImage2.sprite = fpSprite2;
        if (fpImage3 != null && fpSprite3 != null) fpImage3.sprite = fpSprite3;
        if (fpImage4 != null && fpSprite4 != null) fpImage4.sprite = fpSprite4;

        exclamationBtn1.onClick.AddListener(() => OnExclamationClicked(0));
        exclamationBtn2.onClick.AddListener(() => OnExclamationClicked(1));
        exclamationBtn3.onClick.AddListener(() => OnExclamationClicked(2));

        powderButton.onClick.AddListener(OnPowderClicked);
        confirmButton.onClick.AddListener(OnConfirmClicked);

        // dragArrow 자식 화살표 수집 (Arrow1~4)
        if (dragArrow != null)
        {
            dragArrow.SetActive(false);
            arrows = new GameObject[dragArrow.transform.childCount];
            for (int i = 0; i < arrows.Length; i++)
                arrows[i] = dragArrow.transform.GetChild(i).gameObject;
        }
    }

    // ──────────────────────────────────────
    // 가이드 팝업 시작 버튼
    // ──────────────────────────────────────
    void OnStartClicked()
    {
        guidePopup.SetActive(false);
        exclamationBtn1.gameObject.SetActive(true);
        exclamationBtn2.gameObject.SetActive(true);
        exclamationBtn3.gameObject.SetActive(true);
    }

    // ──────────────────────────────────────
    // ! 버튼 클릭
    // ──────────────────────────────────────
    void OnExclamationClicked(int location)
    {
        if (locationCleared[location]) return;

        currentLocation = location;
        powderSpread = false;
        fingerFound = false;
        brushCount = 0;
        lastBrushPos = Vector2.zero;

        dimBackground.SetActive(true);

        switch (location)
        {
            case 0:
                currentBrushArea = brushArea1;
                currentFingerprintImage = fingerprintImage1;
                break;
            case 1:
                currentBrushArea = brushArea2;
                currentFingerprintImage = fingerprintImage2;
                break;
            case 2:
                currentBrushArea = brushArea3;
                currentFingerprintImage = fingerprintImage3;
                break;
        }

        var img = currentFingerprintImage.GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = 0f;
            img.color = c;
        }
        currentFingerprintImage.SetActive(true);

        foreach (Transform child in brushContainer)
            Destroy(child.gameObject);

        powderButton.gameObject.SetActive(true);
        powderBtnText.text = "가루 뿌리기";
        powderButton.interactable = true;

        // 게이지 초기화 및 표시
        gaugeBackground.SetActive(true);
        if (gaugeFill != null) gaugeFill.fillAmount = 0f;
    }

    // ──────────────────────────────────────
    // 가루 뿌리기 버튼
    // ──────────────────────────────────────
    void OnPowderClicked()
    {
        if (powderSpread) return;
        powderSpread = true;
        powderButton.interactable = false;
        powderBtnText.text = "문질러보세요!";
    }

    // ──────────────────────────────────────
    // 터치/드래그 감지
    // ──────────────────────────────────────
    void Update()
    {
        if (!powderSpread || fingerFound || currentBrushArea == null) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Began)
                OnBrush(touch.position);
        }
#else
        if (Input.GetMouseButton(0))
            OnBrush(Input.mousePosition);
#endif
    }

    // ──────────────────────────────────────
    // 문지르기 처리
    // ──────────────────────────────────────
    void OnBrush(Vector2 screenPos)
    {
        if (Vector2.Distance(screenPos, lastBrushPos) < minBrushDistance) return;
        lastBrushPos = screenPos;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            currentBrushArea, screenPos, null, out Vector2 localPos)) return;

        if (!currentBrushArea.rect.Contains(localPos)) return;

        GameObject circle = Instantiate(brushCirclePrefab, brushContainer);
        circle.GetComponent<RectTransform>().anchoredPosition =
            currentBrushArea.anchoredPosition + localPos;
        Destroy(circle, 1.5f);

        brushCount++;

        float alpha = Mathf.Clamp01((float)brushCount / brushCountToReveal);
        var img = currentFingerprintImage?.GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }

        // 게이지 업데이트
        if (gaugeFill != null)
            gaugeFill.fillAmount = alpha;

        if (brushCount >= brushCountToReveal * 0.8f && !fingerFound)
        {
            fingerFound = true;
            StartCoroutine(ShowFingerprintFound());
        }
    }

    // ──────────────────────────────────────
    // 지문 발견 팝업
    // ──────────────────────────────────────
    IEnumerator ShowFingerprintFound()
    {
        yield return new WaitForSeconds(0.5f);
        powderButton.gameObject.SetActive(false);
        evidencePopup.SetActive(true);
        gaugeBackground.SetActive(false);
        popupTitleText.text = "지문 발견!";
        popupDescText.text = "지문을 채취했습니다!\nAFIS로 대조해보세요!";

        if (fingerprintPopupImage != null && locationSprites[currentLocation] != null)
            fingerprintPopupImage.sprite = locationSprites[currentLocation];
    }

    // ──────────────────────────────────────
    // AFIS 화면으로 이동
    // ──────────────────────────────────────
    void OnConfirmClicked()
    {
        evidencePopup.SetActive(false);
        gaugeBackground.SetActive(false);

        if (foundFingerprintImage != null && locationSprites[currentLocation] != null)
            foundFingerprintImage.sprite = locationSprites[currentLocation];

        afisDescText.text = "발견된 지문을 용의자 지문 위에 올려보세요!";
        resultText.text = "";
        afisPanel.SetActive(true);

        // 정답 용의자 카드 위에 화살표 표시
        ShowArrow(0);
    }

    // ──────────────────────────────────────
    // 화살표 표시 (정답 인덱스만)
    // ──────────────────────────────────────
    void ShowArrow(int correctIndex)
    {
        if (dragArrow == null || arrows == null) return;

        dragArrow.SetActive(true);

        // 4개 전부 표시
        for (int i = 0; i < arrows.Length; i++)
        {
            arrows[i].SetActive(true);
            DOTween.Restart("arrow" + (i + 1));
        }
    }

    // ──────────────────────────────────────
    // 화살표 전체 숨기기
    // ──────────────────────────────────────
    void HideArrows()
    {
        if (dragArrow == null || arrows == null) return;

        for (int i = 0; i < arrows.Length; i++)
            DOTween.Pause("arrow" + (i + 1));

        dragArrow.SetActive(false);
    }

    // ──────────────────────────────────────
    // 드롭존에서 호출
    // ──────────────────────────────────────
    public void OnSuspectDropped(string suspectName, DraggableFingerprint draggable)
    {
        if (suspectName == correctAnswers[currentLocation])
        {
            // 정답 → 화살표 숨기기
            HideArrows();

            resultText.text = $"{suspectName}의 지문과 일치합니다!";
            resultText.color = Color.green;
            StartCoroutine(EvidenceObtained());
        }
        else
        {
            resultText.text = $"{suspectName}의 지문이 아니에요. 다시 시도해보세요!";
            resultText.color = Color.red;
            draggable.ReturnToOriginal();
        }
    }

    // ──────────────────────────────────────
    // 증거 획득
    // ──────────────────────────────────────
    IEnumerator EvidenceObtained()
    {
        string evidence = evidenceNames[currentLocation];
        if (DataManager.Instance != null)
        {
            if (!DataManager.Instance.CollectedEvidences.Contains(evidence))
                DataManager.Instance.CollectedEvidences.Add(evidence);
        }

        locationCleared[currentLocation] = true;

        yield return new WaitForSeconds(2f);

        afisPanel.SetActive(false);
        HideArrows();   // 패널 닫힐 때도 확실히 숨기기
        dimBackground.SetActive(false);
        currentFingerprintImage.SetActive(false);
        powderButton.gameObject.SetActive(false);
        gaugeBackground.SetActive(false);
        if (gaugeFill != null) gaugeFill.fillAmount = 0f;

        switch (currentLocation)
        {
            case 0: exclamationBtn1.gameObject.SetActive(false); break;
            case 1: exclamationBtn2.gameObject.SetActive(false); break;
            case 2: exclamationBtn3.gameObject.SetActive(false); break;
        }

        currentLocation = -1;
        currentBrushArea = null;

        // 3개 완료 시 미션 완료 팝업
        if (locationCleared[0] && locationCleared[1] && locationCleared[2])
        {
            StartCoroutine(ShowClearPopup());
        }
    }

    // ──────────────────────────────────────
    // 미션 완료 팝업 + 카운트다운
    // ──────────────────────────────────────
    IEnumerator ShowClearPopup()
    {
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