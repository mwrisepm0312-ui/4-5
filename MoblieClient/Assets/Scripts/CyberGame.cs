using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CyberGame : MonoBehaviour
{
    // ──────────────────────────────────────
    // 가이드 팝업
    // ──────────────────────────────────────
    [Header("가이드 팝업")]
    public GameObject guidePopup;
    public Button startButton;

    [Header("튜토리얼")]
    public GameObject scanTutorial;
    public GameObject sliderTutorial;
    public GameObject keywordTutorial;
    public Button scanTutorialButton;
    public Button sliderTutorialButton;
    public Button keywordTutorialButton;

    // ──────────────────────────────────────
    // 파일 아이콘 3개
    // ──────────────────────────────────────
    [Header("파일 아이콘")]
    public Button fileBtn1;
    public Button fileBtn2;
    public Button fileBtn3;

    // 파일 이미지 스프라이트
    public Sprite fileSprite1;
    public Sprite fileSprite2;
    public Sprite fileSprite3;

    // ──────────────────────────────────────
    // ScanGame (파일 1)
    // ──────────────────────────────────────
    [Header("스캔 게임")]
    public GameObject scanGame;
    public TextMeshProUGUI scanTitleText;
    public RectTransform scanArea;
    public GameObject scanBeam;
    public List<GameObject> fragments;
    public TextMeshProUGUI scanCountText;
    public Button scanCloseButton;

    // ──────────────────────────────────────
    // SliderGame (파일 2)
    // ──────────────────────────────────────
    [Header("슬라이더 게임")]
    public GameObject sliderGame;
    public Image phoneImage;
    public Image grayOverlay;
    public Slider slider1;
    public Slider slider2;
    public Slider slider3;
    public TextMeshProUGUI sliderValue1;
    public TextMeshProUGUI sliderValue2;
    public TextMeshProUGUI sliderValue3;
    public Button restoreButton;
    public TextMeshProUGUI sliderResultText;
    public Button sliderCloseButton;
    private bool sliderSolved = false;

    [Header("슬라이더 이미지")]
    public Sprite phoneColorSprite;
    public Sprite phoneGraySprite;

    // ──────────────────────────────────────
    // KeywordGame (파일 3)
    // ──────────────────────────────────────
    [Header("키워드 게임")]
    public GameObject keywordGame;
    public GameObject analyzingPanel;   // 로딩 화면
    public Slider analyzingBar;         // 로딩 바
    public GameObject keywordListPanel; // 검색어 목록
    public Transform keywordContent;    // ScrollView Content
    public GameObject keywordTemplate;  // 버튼 템플릿
    public TextMeshProUGUI keywordResultText;
    public Button keywordCloseButton;
    public Button keywordSubmitButton; // 정답 제출 버튼

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
    // 검색어 데이터
    // ──────────────────────────────────────
    private string[] allKeywords =
    {
        "레드 다이아몬드 반지 시세",    // 정답 ★
        "오늘 날씨",
        "점심 맛집 추천",
        "레드 다이아몬드 중고 거래",    // 정답 ★
        "영화 상영 시간",
        "레드 다이아 반지 가격",        // 정답 ★
        "주말 나들이 장소",
        "택배 조회",
        "스트레스 해소 방법",
        "퇴직금 계산기"
    };

    private string[] correctKeywords =
    {
        "레드 다이아몬드 반지 시세",
        "레드 다이아몬드 중고 거래",
        "레드 다이아 반지 가격"
    };

    private List<string> selectedKeywords = new List<string>();

    // ──────────────────────────────────────
    // 증거 데이터
    // ──────────────────────────────────────
    private string[] evidenceNames =
    {
        "수집가 반지 구매 요청 이메일",
        "경비원 불만 메시지",
        "비서 레드 다이아 반지 시세 검색 기록"
    };

    private string[] evidenceDescs =
    {
        "[수집가의 반지 구매 요청 이메일]\n\n수집가가 반지를 팔아달라고 하는 내용의 이메일이 여러 건 있다.",
        "[경비원의 수상한 메시지]\n\n알 수 없는 번호로부터 메시지가 와 있다.\n- 오늘 밤 은행 비워?\n- 몇 시에 끝나?",
        "[비서의 반지 시세 검색 기록]\n\n사건 2주일 전부터 지속적으로 반지 시세를 검색하고 판매처를 알아본 것으로 보인다."
    };

    // ──────────────────────────────────────
    // 상태 변수
    // ──────────────────────────────────────
    private int currentFile = -1;
    private bool[] fileCleared = { false, false, false };
    private int totalCleared = 0;
    private int foundCount = 0;
    private float[] answerMin = { 35f, 70f, 55f };
    private float[] answerMax = { 45f, 80f, 65f };

    // ──────────────────────────────────────
    // Start
    // ──────────────────────────────────────
    void Start()
    {
        guidePopup.SetActive(true);
        scanGame.SetActive(false);
        sliderGame.SetActive(false);
        keywordGame.SetActive(false);
        scanTutorial.SetActive(false);
        sliderTutorial.SetActive(false);
        keywordTutorial.SetActive(false);
        evidencePopup.SetActive(false);
        clearTitleText.SetActive(false);
        clearDescText.SetActive(false);
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        fileBtn1.gameObject.SetActive(false);
        fileBtn2.gameObject.SetActive(false);
        fileBtn3.gameObject.SetActive(false);

        // 스프라이트 로드
        if (phoneColorSprite == null)
            phoneColorSprite = Resources.Load<Sprite>("Cyber/phone_color");
        if (phoneGraySprite == null)
            phoneGraySprite = Resources.Load<Sprite>("Cyber/phone_gray");

        // 버튼 이벤트
        startButton.onClick.AddListener(OnStartClicked);
        fileBtn1.onClick.AddListener(() => OnFileClicked(0));
        fileBtn2.onClick.AddListener(() => OnFileClicked(1));
        fileBtn3.onClick.AddListener(() => OnFileClicked(2));
        confirmButton.onClick.AddListener(OnConfirmClicked);
        scanCloseButton.onClick.AddListener(CloseScanGame);
        sliderCloseButton.onClick.AddListener(CloseSliderGame);
        restoreButton.onClick.AddListener(OnRestoreClicked);
        keywordCloseButton.onClick.AddListener(CloseKeywordGame);
        keywordSubmitButton.onClick.AddListener(OnKeywordSubmit);
        scanTutorialButton.onClick.AddListener(() => OnTutorialConfirmed(0));
        sliderTutorialButton.onClick.AddListener(() => OnTutorialConfirmed(1));
        keywordTutorialButton.onClick.AddListener(() => OnTutorialConfirmed(2));

        slider1.onValueChanged.AddListener(OnSlider1Changed);
        slider2.onValueChanged.AddListener(OnSlider2Changed);
        slider3.onValueChanged.AddListener(OnSlider3Changed);

        keywordTemplate.SetActive(false);
    }

    // ──────────────────────────────────────
    // 가이드 팝업 시작
    // ──────────────────────────────────────
    void OnStartClicked()
    {
        guidePopup.SetActive(false);
        fileBtn1.gameObject.SetActive(true);
        fileBtn2.gameObject.SetActive(true);
        fileBtn3.gameObject.SetActive(true);
    }

    // ──────────────────────────────────────
    // 파일 클릭
    // ──────────────────────────────────────
    // 튜토리얼 띄우기
    void OnFileClicked(int fileIndex)
    {
        if (fileCleared[fileIndex]) return;
        currentFile = fileIndex;

        switch (fileIndex)
        {
            case 0: OpenScanGame(); break;
            case 1: OpenSliderGame(); break;
            case 2: OpenKeywordGame(); break;
        }

        ShowTutorial(fileIndex);   // 게임이 열린 뒤 그 위에 튜토리얼 표시
    }

    void ShowTutorial(int fileIndex)
    {
        switch (fileIndex)
        {
            case 0: scanTutorial.SetActive(true); break;
            case 1: sliderTutorial.SetActive(true); break;
            case 2: keywordTutorial.SetActive(true); break;
        }
    }

    void OnTutorialConfirmed(int fileIndex)
    {
        scanTutorial.SetActive(false);
        sliderTutorial.SetActive(false);
        keywordTutorial.SetActive(false);
    }

    // ──────────────────────────────────────
    // 스캔 게임
    // ──────────────────────────────────────
    void OpenScanGame()
    {
        foundCount = 0;
        int total = fragments.Count;

        foreach (var f in fragments)
        {
            f.SetActive(true);
            var img = f.GetComponent<Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = 0f;
                img.color = c;
            }
        }

        scanTitleText.text = "데이터 조각을 스캔하세요!";
        scanCountText.text = $"0 / {total}";
        scanGame.SetActive(true);
    }

    void CloseScanGame()
    {
        scanGame.SetActive(false);
        currentFile = -1;
    }

    void Update()
    {
        if (scanTutorial.activeSelf) return;   // 튜토리얼 중에는 스캔 입력 무시

        if (scanGame.activeSelf && currentFile == 0)
        {
            Vector2 inputPos = Vector2.zero;
            bool isDragging = false;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Began)
                {
                    inputPos = touch.position;
                    isDragging = true;
                }
            }
#else
            if (Input.GetMouseButton(0))
            {
                inputPos = Input.mousePosition;
                isDragging = true;
            }
#endif
            if (isDragging) OnScanDrag(inputPos);
        }
    }

    void OnScanDrag(Vector2 screenPos)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            scanArea, screenPos, null, out Vector2 localPos)) return;

        if (scanBeam != null)
        {
            scanBeam.SetActive(true);
            scanBeam.GetComponent<RectTransform>().anchoredPosition = localPos;
        }

        for (int i = 0; i < fragments.Count; i++)
        {
            var f = fragments[i];
            var img = f.GetComponent<Image>();
            if (img == null || img.color.a >= 1f) continue;

            RectTransform frt = f.GetComponent<RectTransform>();
            float dist = Vector2.Distance(localPos, frt.anchoredPosition);

            if (dist < 80f)
            {
                Color c = img.color;
                c.a = 1f;
                img.color = c;
                foundCount++;
                scanCountText.text = $"{foundCount} / {fragments.Count}";

                if (foundCount >= fragments.Count)
                    StartCoroutine(ScanGameClear());
            }
        }
    }

    IEnumerator ScanGameClear()
    {
        yield return new WaitForSeconds(1.5f);
        ShowEvidencePopup(currentFile);
        scanGame.SetActive(false);
    }

    // ──────────────────────────────────────
    // 슬라이더 게임
    // ──────────────────────────────────────
    void OpenSliderGame()
    {
        sliderSolved = false;

        slider1.value = 0;
        slider2.value = 0;
        slider3.value = 0;
        sliderResultText.text = "";

        // 컬러 원본 이미지로 시작 (셰이더가 어둡게/흐리게/흑백으로 가려줌)
        if (phoneImage != null)
        {
            if (phoneColorSprite != null)
                phoneImage.sprite = phoneGraySprite;
            phoneImage.color = Color.white;   // 기존 알파 0.3 제거 (셰이더 쓰면 투명해짐)
        }

        // 안개 오버레이는 셰이더가 대신하므로 끔
        if (grayOverlay != null)
            grayOverlay.gameObject.SetActive(false);

        UpdatePhoneVisual();   // 값이 이미 0이면 이벤트가 안 와서 직접 호출
        sliderGame.SetActive(true);
    }

    void CloseSliderGame()
    {
        sliderGame.SetActive(false);
        currentFile = -1;
    }

    void OnSlider1Changed(float value)
    {
        sliderValue1.text = ((int)value).ToString();
        UpdatePhoneVisual();
    }

    void OnSlider2Changed(float value)
    {
        sliderValue2.text = ((int)value).ToString();
        UpdatePhoneVisual();
    }

    void OnSlider3Changed(float value)
    {
        sliderValue3.text = ((int)value).ToString();
        UpdatePhoneVisual();
    }

    void UpdatePhoneVisual()
    {
        if (phoneImage == null || sliderSolved) return;

        float c1 = (answerMin[0] + answerMax[0]) / 2f;
        float c2 = (answerMin[1] + answerMax[1]) / 2f;
        float c3 = (answerMin[2] + answerMax[2]) / 2f;

        float bright = slider1.value / c1;
        float color = slider3.value / c3;
        float sharp = slider2.value / c2;

        Material m = phoneImage.material;

        // 밝기: 정답까지 0.1→1, 넘으면 급격히 증가 (과노출로 하얗게)
        float b = bright <= 1f ? Mathf.Lerp(0.1f, 1f, bright) : 1f + (bright - 1f) * 8f;
        m.SetFloat("_Brightness", b);

        // 채도: 정답까지 0→1, 넘으면 급격히 증가 (색이 형광처럼 튐)
        float s = color <= 1f ? color : 1f + (color - 1f) * 8f;
        m.SetFloat("_Saturation", s);

        // 블러: 정답에서 0
        // 정답 전: 흐림→선명 / 정답 후: 다시 흐려짐 (너무 올리면 번져서 안 보임)
        float blur = sharp <= 1f ? 1f - sharp : (sharp - 1f) * 1.5f;
        m.SetFloat("_Blur", blur);
    }

    void OnRestoreClicked()
    {
        bool s1 = slider1.value >= answerMin[0] && slider1.value <= answerMax[0];
        bool s2 = slider2.value >= answerMin[1] && slider2.value <= answerMax[1];
        bool s3 = slider3.value >= answerMin[2] && slider3.value <= answerMax[2];

        if (s1 && s2 && s3)
        {
            sliderSolved = true;   // 이후 슬라이더를 움직여도 화면이 안 바뀜

            // 정답 이미지로 교체 + 셰이더 효과 모두 해제
            if (phoneImage != null)
            {
                if (phoneColorSprite != null)
                    phoneImage.sprite = phoneColorSprite;   // ← 준비한 정답 이미지
                Material m = phoneImage.material;
                m.SetFloat("_Brightness", 1f);
                m.SetFloat("_Saturation", 1f);
                m.SetFloat("_Blur", 0f);
            }

            sliderResultText.text = "복원 완료!";
            sliderResultText.color = Color.green;
            StartCoroutine(SliderGameClear());
        }
        else
        {
            // 힌트 표시
            string hint = "";
            if (!s1) hint += slider1.value < answerMin[0] ? "밝기 높여보세요  " : "밝기 낮춰보세요  ";
            if (!s2) hint += slider2.value < answerMin[1] ? "선명도 높여보세요  " : "선명도 낮춰보세요  ";
            if (!s3) hint += slider3.value < answerMin[2] ? "색상 높여보세요" : "색상 낮춰보세요";
            sliderResultText.text = $"복원 실패!\n{hint}";
            sliderResultText.color = Color.red;
        }
    }

    IEnumerator SliderGameClear()
    {
        yield return new WaitForSeconds(1.5f);
        ShowEvidencePopup(currentFile);
        sliderGame.SetActive(false);
    }

    // ──────────────────────────────────────
    // 키워드 게임
    // ──────────────────────────────────────
    void OpenKeywordGame()
    {
        selectedKeywords.Clear();
        keywordResultText.text = "";
        keywordListPanel.SetActive(true);
        GenerateKeywordButtons();
        keywordGame.SetActive(true);
        keywordSubmitButton.interactable = true;

    }

    void GenerateKeywordButtons()
    {
        // 기존 버튼 삭제
        foreach (Transform child in keywordContent)
        {
            if (child.gameObject != keywordTemplate)
                Destroy(child.gameObject);
        }

        // 검색어 버튼 생성
        foreach (string keyword in allKeywords)
        {
            string kw = keyword;
            GameObject btn = Instantiate(keywordTemplate, keywordContent);
            btn.SetActive(true);

            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            btnText.text = kw;

            Button button = btn.GetComponent<Button>();
            button.onClick.AddListener(() => OnKeywordSelected(kw, btn));
        }
    }

    void OnKeywordSelected(string keyword, GameObject btnObj)
    {
        if (selectedKeywords.Contains(keyword))
        {
            selectedKeywords.Remove(keyword);
            btnObj.GetComponent<Image>().color = new Color(0.08f, 0.16f, 0.31f);
        }
        else
        {
            selectedKeywords.Add(keyword);
            btnObj.GetComponent<Image>().color = new Color(0.1f, 0.5f, 0.1f);
        }

        // 선택을 바꾸면 이전 결과 메시지 지우기
        keywordResultText.text = "";
    }

    void OnKeywordSubmit()
    {
        // 이미 정답 처리 중이면 중복 제출 방지
        if (!keywordSubmitButton.interactable) return;

        bool allCorrect = selectedKeywords.Count == correctKeywords.Length;
        if (allCorrect)
        {
            foreach (string correct in correctKeywords)
            {
                if (!selectedKeywords.Contains(correct))
                {
                    allCorrect = false;
                    break;
                }
            }
        }

        if (allCorrect)
        {
            keywordResultText.text = "필요한 검색어를 모두 수집했습니다!";
            keywordResultText.color = Color.green;
            keywordSubmitButton.interactable = false;
            StartCoroutine(KeywordGameClear());
        }
        else
        {
            keywordResultText.text = "필요 없는 검색어가 포함됐거나\n빠진 검색어가 있습니다. 다시 확인해보세요.";
            keywordResultText.color = Color.red;
        }
    }

    IEnumerator KeywordGameClear()
    {
        yield return new WaitForSeconds(1.5f);
        ShowEvidencePopup(currentFile);
        keywordGame.SetActive(false);
    }

    void CloseKeywordGame()
    {
        keywordGame.SetActive(false);
        currentFile = -1;
    }

    // ──────────────────────────────────────
    // 증거 팝업
    // ──────────────────────────────────────
    void ShowEvidencePopup(int fileIndex)
    {
        // 파일 버튼 비활성화
        fileBtn1.interactable = false;
        fileBtn2.interactable = false;
        fileBtn3.interactable = false;

        evidenceTitleText.text = "파일 복원 완료!\n\n다음 증거물을 획득했습니다.";
        evidenceDescText.text = evidenceDescs[fileIndex];
        evidencePopup.SetActive(true);
    }

    void OnConfirmClicked()
    {
        evidencePopup.SetActive(false);

        // 완료 안 된 파일 버튼만 다시 활성화
        if (!fileCleared[0]) fileBtn1.interactable = true;
        if (!fileCleared[1]) fileBtn2.interactable = true;
        if (!fileCleared[2]) fileBtn3.interactable = true;

        string evidence = evidenceNames[currentFile];
        if (DataManager.Instance != null)
        {
            if (!DataManager.Instance.CollectedEvidences.Contains(evidence))
                DataManager.Instance.CollectedEvidences.Add(evidence);
        }

        fileCleared[currentFile] = true;
        totalCleared++;

        switch (currentFile)
        {
            case 0:
                fileBtn1.image.sprite = fileSprite1;
                fileBtn1.interactable = false;
                break;
            case 1:
                fileBtn2.image.sprite = fileSprite2;
                fileBtn2.interactable = false;
                break;
            case 2:
                fileBtn3.image.sprite = fileSprite3;
                fileBtn3.interactable = false;
                break;
        }

        currentFile = -1;

        if (totalCleared >= 3)
            StartCoroutine(ShowClearPopup());
    }

    // ──────────────────────────────────────
    // 미션 완료
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