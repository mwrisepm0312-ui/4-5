using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class ForensicGame : MonoBehaviour
{
    [Header("배경 및 딤")]
    public GameObject dimBackground;

    [Header("느낌표 버튼")]
    public Button exclamationBtn1;
    public Button exclamationBtn2;
    public Button exclamationBtn3;

    [Header("브러시 영역")]
    public RectTransform brushArea1;
    public RectTransform brushArea2;
    public RectTransform brushArea3;

    [Header("지문 이미지")]
    public GameObject fingerprintImage1;
    public GameObject fingerprintImage2;
    public GameObject fingerprintImage3;

    [Header("브러시 설정")]
    public GameObject brushCirclePrefab;
    public Transform brushContainer;
    public int brushCountToReveal = 30;
    public float minBrushDistance = 20f;

    [Header("게임 가이드 팝업")]
    public GameObject guidePopup;
    public Button startButton;

    [Header("드래그 가이드")]
    public GameObject dragArrow;
    private GameObject[] arrows;

    [Header("가루 뿌리기 버튼")]
    public Button powderButton;
    public TextMeshProUGUI powderBtnText;

    [Header("가이드 손가락 이미지")]
    public GameObject fingerGuideImage;
    public GameObject afisFingerGuideImage; // AFIS 패널용 드래그 가이드 손가락

    [Header("지문 발견 팝업")]
    public GameObject evidencePopup;
    public Image fingerprintPopupImage;
    public TextMeshProUGUI popupTitleText;
    public TextMeshProUGUI popupDescText;
    public Button confirmButton;

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

    [Header("미션 완료")]
    public GameObject clearTitleText;
    public GameObject clearDescText;
    public TextMeshProUGUI countdownText;

    private string[] correctAnswers = { "비서", "청소부", "경비원" };
    private int[] correctSuspectIndex = { 0, 1, 2 };
    private string[] evidenceNames = { "금고 지문", "바닥 지문", "데스크 지문" };
    // 일치했을 때 한 줄 설명 (왜 중요한 지문인지)
    private string[] matchHints = {
        "집에 갔다던 비서가 금고 손잡이를 만졌어요!",
        "바닥을 청소하며 남긴 지문이에요.",
        "경비 데스크에서 근무하며 남긴 지문이에요."
    };
    private Sprite[] locationSprites;

    private int currentLocation = -1;
    private bool powderSpread = false;
    private bool fingerFound = false;
    private int brushCount = 0;
    private Vector2 lastBrushPos = Vector2.zero;
    private RectTransform currentBrushArea;
    private GameObject currentFingerprintImage;
    private List<bool> locationCleared = new List<bool> { false, false, false };

    private bool fingerGuideActive = false;
    private bool afisGuideActive = false;

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

        if (fingerGuideImage != null)
            fingerGuideImage.SetActive(false);
        if (afisFingerGuideImage != null)
            afisFingerGuideImage.SetActive(false);

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

        if (dragArrow != null)
        {
            dragArrow.SetActive(false);
            arrows = new GameObject[dragArrow.transform.childCount];
            for (int i = 0; i < arrows.Length; i++)
                arrows[i] = dragArrow.transform.GetChild(i).gameObject;
        }
    }

    void OnStartClicked()
    {
        guidePopup.SetActive(false);

        exclamationBtn1.gameObject.SetActive(true);
        exclamationBtn2.gameObject.SetActive(true);
        exclamationBtn3.gameObject.SetActive(true);

        AnimateExclamationBtn(exclamationBtn1, 0f);
        AnimateExclamationBtn(exclamationBtn2, 0.2f);
        AnimateExclamationBtn(exclamationBtn3, 0.4f);
    }

    // ! 버튼의 원래 높이 (다시 켤 때마다 위로 밀려 올라가지 않도록 처음 값을 기억)
    private readonly System.Collections.Generic.Dictionary<Transform, float> exclamationBaseY
        = new System.Collections.Generic.Dictionary<Transform, float>();

    void AnimateExclamationBtn(Button btn, float delay)
    {
        Transform t = btn.transform;
        if (!exclamationBaseY.TryGetValue(t, out float baseY))
        {
            baseY = t.localPosition.y;
            exclamationBaseY[t] = baseY;
        }

        // 남아 있던 흔들림을 끄고 제자리·원래 크기에서 다시 시작
        DOTween.Kill(t);
        t.localPosition = new Vector3(t.localPosition.x, baseY, t.localPosition.z);
        t.localScale = Vector3.one;

        t.DOLocalMoveY(baseY + 15f, 0.6f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetDelay(delay);

        t.DOScale(Vector3.one * 1.15f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetDelay(delay);
    }

    void OnExclamationClicked(int location)
    {
        if (locationCleared[location]) return;

        Button clickedBtn = location == 0 ? exclamationBtn1
                          : location == 1 ? exclamationBtn2
                          : exclamationBtn3;
        DOTween.Kill(clickedBtn.transform);
        clickedBtn.transform.localScale = Vector3.one;

        if (location != 0) exclamationBtn1.gameObject.SetActive(false);
        if (location != 1) exclamationBtn2.gameObject.SetActive(false);
        if (location != 2) exclamationBtn3.gameObject.SetActive(false);

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
    }

    void OnPowderClicked()
    {
        if (powderSpread) return;
        powderButton.interactable = false;
        powderBtnText.text = "문질러 보세요!";
        StartCoroutine(PlayFingerGuide());
    }

    IEnumerator PlayFingerGuide()
    {
        if (fingerGuideImage == null)
        {
            Debug.LogWarning("[ForensicGame] fingerGuideImage가 null! Inspector에서 연결해주세요.");
            powderSpread = true;
            yield break;
        }

        fingerGuideActive = true;

        RectTransform guideRect = fingerGuideImage.GetComponent<RectTransform>();
        RectTransform areaRect = currentBrushArea;
        Canvas canvas = fingerGuideImage.GetComponentInParent<Canvas>();
        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

        // 부모가 없거나 Canvas 자체이면 Canvas의 RectTransform을 기준으로 사용
        RectTransform guideParent = fingerGuideImage.transform.parent?.GetComponent<RectTransform>();
        if (guideParent == null)
            guideParent = canvas.GetComponent<RectTransform>();

        // brushArea의 4개 코너를 월드→스크린→guideParent 로컬로 변환
        Vector3[] areaCorners = new Vector3[4];
        areaRect.GetWorldCorners(areaCorners);
        // 0:좌하, 1:좌상, 2:우상, 3:우하

        Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(cam, (areaCorners[0] + areaCorners[2]) * 0.5f);
        Vector2 screenTop = RectTransformUtility.WorldToScreenPoint(cam, (areaCorners[1] + areaCorners[2]) * 0.5f);
        Vector2 screenBottom = RectTransformUtility.WorldToScreenPoint(cam, (areaCorners[0] + areaCorners[3]) * 0.5f);
        Vector2 screenLeft = RectTransformUtility.WorldToScreenPoint(cam, (areaCorners[0] + areaCorners[1]) * 0.5f);
        Vector2 screenRight = RectTransformUtility.WorldToScreenPoint(cam, (areaCorners[2] + areaCorners[3]) * 0.5f);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(guideParent, screenCenter, cam, out Vector2 localCenter);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(guideParent, screenTop, cam, out Vector2 localTop);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(guideParent, screenBottom, cam, out Vector2 localBottom);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(guideParent, screenLeft, cam, out Vector2 localLeft);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(guideParent, screenRight, cam, out Vector2 localRight);

        // 시작: brushArea 위쪽, 끝: brushArea 아래쪽
        Vector2 startPos = localTop + new Vector2(0f, 30f);
        Vector2 endPos = localBottom + new Vector2(0f, -10f);
        // 좌우 흔들림: brushArea 너비의 30% — 월드→로컬 변환으로 정확한 픽셀 단위
        float swingWidth = (localRight.x - localLeft.x) * 0.3f;

        guideRect.anchoredPosition = startPos;
        fingerGuideImage.SetActive(true);

        // 버튼 터치가 완전히 끝날 때까지 대기
        yield return new WaitForSeconds(0.3f);

        for (int i = 0; i < 3; i++)
        {
            if (!fingerGuideActive) break;

            float duration = 0.7f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (!fingerGuideActive) break;

#if UNITY_ANDROID && !UNITY_EDITOR
                if (Input.touchCount > 0) { fingerGuideActive = false; break; }
#else
                if (Input.GetMouseButtonDown(0)) { fingerGuideActive = false; break; }
#endif
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                float yPos = Mathf.Lerp(startPos.y, endPos.y, t);
                float xOffset = Mathf.Sin(t * Mathf.PI * 3f) * swingWidth;
                guideRect.anchoredPosition = new Vector2(localCenter.x + xOffset, yPos);

                yield return null;
            }

            if (!fingerGuideActive) break;

            guideRect.anchoredPosition = startPos;
            yield return new WaitForSeconds(0.08f);
        }

        fingerGuideImage.SetActive(false);
        fingerGuideActive = false;
        powderSpread = true;
    }

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

        if (brushCount >= brushCountToReveal * 0.8f && !fingerFound)
        {
            fingerFound = true;
            StartCoroutine(ShowFingerprintFound());
        }
    }

    IEnumerator ShowFingerprintFound()
    {
        yield return new WaitForSeconds(0.5f);
        powderButton.gameObject.SetActive(false);
        evidencePopup.SetActive(true);
        popupTitleText.text = "지문 발견!";
        popupDescText.text = "지문을 채취했어요!\nAFIS로 주인을 찾아 보세요.";

        if (fingerprintPopupImage != null && locationSprites[currentLocation] != null)
            fingerprintPopupImage.sprite = locationSprites[currentLocation];
    }

    void OnConfirmClicked()
    {
        evidencePopup.SetActive(false);

        if (foundFingerprintImage != null && locationSprites[currentLocation] != null)
            foundFingerprintImage.sprite = locationSprites[currentLocation];

        afisDescText.text = "발견된 지문을 끌어서\n같은 용의자 지문 위에 놓으세요.";
        resultText.text = "";
        afisPanel.SetActive(true);

        ShowArrow(0);
        StartCoroutine(PlayAfisFingerGuide());
    }

    void ShowArrow(int correctIndex)
    {
        if (dragArrow == null || arrows == null) return;
        dragArrow.SetActive(true);
        for (int i = 0; i < arrows.Length; i++)
        {
            arrows[i].SetActive(true);
            DOTween.Restart("arrow" + (i + 1));
        }
    }

    void HideArrows()
    {
        if (dragArrow == null || arrows == null) return;
        for (int i = 0; i < arrows.Length; i++)
            DOTween.Pause("arrow" + (i + 1));
        dragArrow.SetActive(false);
    }

    // AFIS 패널: 발견된 지문 → 용의자 지문 방향으로 손가락 가이드 (3회)
    IEnumerator PlayAfisFingerGuide()
    {
        if (afisFingerGuideImage == null) yield break;

        afisGuideActive = true;

        RectTransform guideRect = afisFingerGuideImage.GetComponent<RectTransform>();
        Canvas canvas = afisFingerGuideImage.GetComponentInParent<Canvas>();
        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

        RectTransform guideParent = afisFingerGuideImage.transform.parent?.GetComponent<RectTransform>();
        if (guideParent == null) guideParent = canvas.GetComponent<RectTransform>();

        // 출발: foundFingerprintImage (발견된 지문)
        RectTransform fromRect = foundFingerprintImage.GetComponent<RectTransform>();
        // 도착: 정답 용의자 지문 이미지
        Image[] fpImages = { fpImage1, fpImage2, fpImage3, fpImage4 };
        int targetIdx = correctSuspectIndex[currentLocation];
        RectTransform toRect = fpImages[targetIdx].GetComponent<RectTransform>();

        Vector3[] fromCorners = new Vector3[4];
        Vector3[] toCorners = new Vector3[4];
        fromRect.GetWorldCorners(fromCorners);
        toRect.GetWorldCorners(toCorners);

        Vector2 screenFrom = RectTransformUtility.WorldToScreenPoint(cam, (fromCorners[0] + fromCorners[2]) * 0.5f);
        Vector2 screenTo = RectTransformUtility.WorldToScreenPoint(cam, (toCorners[0] + toCorners[2]) * 0.5f);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(guideParent, screenFrom, cam, out Vector2 localFrom);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(guideParent, screenTo, cam, out Vector2 localTo);

        guideRect.anchoredPosition = localFrom;
        afisFingerGuideImage.SetActive(true);

        // 확인 버튼 터치가 완전히 끝날 때까지 대기
        yield return new WaitForSeconds(0.3f);

        for (int i = 0; i < 3; i++)
        {
            if (!afisGuideActive) break;

            float duration = 0.8f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (!afisGuideActive) break;
#if UNITY_ANDROID && !UNITY_EDITOR
                if (Input.touchCount > 0) { afisGuideActive = false; break; }
#else
                if (Input.GetMouseButtonDown(0)) { afisGuideActive = false; break; }
#endif
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                guideRect.anchoredPosition = Vector2.Lerp(localFrom, localTo, t);
                yield return null;
            }

            if (!afisGuideActive) break;

            // 도착 후 짧은 대기 → 출발점으로 순간이동
            yield return new WaitForSeconds(0.15f);
            guideRect.anchoredPosition = localFrom;
            yield return new WaitForSeconds(0.1f);
        }

        afisFingerGuideImage.SetActive(false);
        afisGuideActive = false;
    }

    public void OnSuspectDropped(string suspectName, DraggableFingerprint draggable)
    {
        afisGuideActive = false; // 드래그 시작 시 가이드 중단
        if (afisFingerGuideImage != null) afisFingerGuideImage.SetActive(false);

        if (suspectName == correctAnswers[currentLocation])
        {
            HideArrows();
            resultText.text = $"{suspectName}의 지문과 일치해요!\n{matchHints[currentLocation]}";
            resultText.color = Color.green;
            StartCoroutine(EvidenceObtained());
        }
        else
        {
            //resultText.text = /*$"{suspectName}의 지문이 아니에요. 다시 시도해보세요!";*/
            resultText.text = "일치하지 않아요. 다시 해 보세요.";
            resultText.color = Color.red;
            draggable.ReturnToOriginal();
        }
    }

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
        HideArrows();
        dimBackground.SetActive(false);
        currentFingerprintImage.SetActive(false);
        powderButton.gameObject.SetActive(false);

        if (!locationCleared[0]) { exclamationBtn1.gameObject.SetActive(true); AnimateExclamationBtn(exclamationBtn1, 0f); }
        else exclamationBtn1.gameObject.SetActive(false);

        if (!locationCleared[1]) { exclamationBtn2.gameObject.SetActive(true); AnimateExclamationBtn(exclamationBtn2, 0.1f); }
        else exclamationBtn2.gameObject.SetActive(false);

        if (!locationCleared[2]) { exclamationBtn3.gameObject.SetActive(true); AnimateExclamationBtn(exclamationBtn3, 0.2f); }
        else exclamationBtn3.gameObject.SetActive(false);

        currentLocation = -1;
        currentBrushArea = null;

        if (locationCleared[0] && locationCleared[1] && locationCleared[2])
            StartCoroutine(ShowClearPopup());
    }

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

    void OnDestroy()
    {
        fingerGuideActive = false;
        afisGuideActive = false;
    }
}