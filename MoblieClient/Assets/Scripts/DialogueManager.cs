using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    // ──────────────────────────────────────
    // UI 슬롯
    // ──────────────────────────────────────
    [Header("대화 패널")]
    public GameObject dialoguePanel;
    public Image dialogueBG;
    public Image characterImage;
    public Image fadePanel;

    [Header("대화창")]
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public GameObject nextIcon;

    [Header("타이핑 설정")]
    public float charInterval = 0.03f;

    // ──────────────────────────────────────
    // 대화 데이터
    // ──────────────────────────────────────
    [System.Serializable]
    public class DialogueLine
    {
        public string speakerName;
        [TextArea(2, 5)]
        public string text;
        public Sprite characterSprite;
    }

    // ──────────────────────────────────────
    // 상태 변수
    // ──────────────────────────────────────
    private List<DialogueLine> currentLines = new List<DialogueLine>();
    private int currentIndex = 0;
    private bool isTyping = false;
    private bool isDialogueActive = false;
    private bool isFading = false;          // 페이드 중에는 터치를 받지 않음 (첫 대사가 건너뛰어지지 않게)
    private System.Action onDialogueFinished;

    // ──────────────────────────────────────
    // 대화 시작
    // ──────────────────────────────────────
    public void StartDialogue(List<DialogueLine> lines, System.Action onFinished = null)
    {
        currentLines = lines;
        currentIndex = 0;
        onDialogueFinished = onFinished;
        isDialogueActive = true;

        // 패널을 켜기 전에 이번 목격자의 모습·빈 대화창으로 먼저 바꿔 둠
        // (안 그러면 페이드 동안 하얀 네모나 이전 목격자 모습·대사가 잠깐 보임)
        Sprite first = null;
        foreach (var l in lines)
            if (l.characterSprite != null) { first = l.characterSprite; break; }
        characterImage.sprite = first;
        characterImage.enabled = first != null;
        speakerNameText.text = lines.Count > 0 ? lines[0].speakerName : "";
        dialogueText.text = "";

        dialoguePanel.SetActive(true);
        nextIcon.SetActive(false);

        StartCoroutine(FadeIn());
    }

    // ──────────────────────────────────────
    // 페이드 인
    // ──────────────────────────────────────
    IEnumerator FadeIn()
    {
        isFading = true;
        fadePanel.gameObject.SetActive(true);
        Color c = fadePanel.color;
        c.a = 1f;
        fadePanel.color = c;

        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / 0.5f);
            fadePanel.color = c;
            yield return null;
        }

        c.a = 0f;
        fadePanel.color = c;
        fadePanel.gameObject.SetActive(false);
        isFading = false;

        ShowLine(currentIndex);
    }

    // ──────────────────────────────────────
    // 페이드 아웃
    // ──────────────────────────────────────
    IEnumerator FadeOut()
    {
        isFading = true;
        fadePanel.gameObject.SetActive(true);
        Color c = fadePanel.color;
        c.a = 0f;
        fadePanel.color = c;

        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, t / 0.5f);
            fadePanel.color = c;
            yield return null;
        }

        c.a = 1f;
        fadePanel.color = c;

        dialoguePanel.SetActive(false);
        isDialogueActive = false;
        isFading = false;

        // 대화 완료 콜백
        onDialogueFinished?.Invoke();
    }

    // ──────────────────────────────────────
    // 대화 라인 표시
    // ──────────────────────────────────────
    void ShowLine(int index)
    {
        if (index >= currentLines.Count)
        {
            StartCoroutine(FadeOut());
            return;
        }

        DialogueLine line = currentLines[index];

        // 화자 이름
        speakerNameText.text = line.speakerName;

        // 캐릭터 이미지
        if (line.characterSprite != null)
        {
            characterImage.sprite = line.characterSprite;
            characterImage.enabled = true;
        }

        // 타이핑 효과
        StartCoroutine(TypeText(line.text));
    }

    // ──────────────────────────────────────
    // 타이핑 효과
    // ──────────────────────────────────────
    IEnumerator TypeText(string text)
    {
        isTyping = true;
        nextIcon.SetActive(false);
        dialogueText.text = "";

        // Rich Text 지원 타이핑
        int visibleCount = 0;
        string fullText = text;

        dialogueText.text = fullText;
        dialogueText.maxVisibleCharacters = 0;

        while (visibleCount < dialogueText.textInfo.characterCount)
        {
            visibleCount++;
            dialogueText.maxVisibleCharacters = visibleCount;
            yield return new WaitForSeconds(charInterval);
        }

        isTyping = false;
        nextIcon.SetActive(true);
    }

    // ──────────────────────────────────────
    // 화면 터치/클릭
    // ──────────────────────────────────────
    void Update()
    {
        if (!isDialogueActive || isFading) return;

        bool clicked = false;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            clicked = true;
#else
        if (Input.GetMouseButtonDown(0))
            clicked = true;
#endif

        if (!clicked) return;

        if (isTyping)
        {
            // 타이핑 중 클릭 → 전체 텍스트 즉시 표시
            StopAllCoroutines();
            isTyping = false;
            dialogueText.maxVisibleCharacters = 9999;
            nextIcon.SetActive(true);
        }
        else
        {
            // 다음 대화
            currentIndex++;
            ShowLine(currentIndex);
        }
    }
}
