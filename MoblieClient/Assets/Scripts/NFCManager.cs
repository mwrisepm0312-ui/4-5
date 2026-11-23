using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using DigitsNFCToolkit;

public class NFCManager : MonoBehaviour
{
    // ──────────────────────────────────────
    // NFC 카드 ID 등록
    // ──────────────────────────────────────
    private const string FORENSIC_ID = "04a13e2a2c0389";
    private const string CCTV_ID = "0451351c2c0389";
    private const string WITNESS_ID = "041119252c0389";
    private const string CYBER_ID = "0461d9202c0389";

    // 테스트용 카드 ID
    private const string UNKNOWN_ID = "0401BC292C0389";
    private const string INVALID_ID = "0461E71A2C0389";
    private const string UNREGISTERED_ID = "045167222C0389";

    // ──────────────────────────────────────
    // NativeNFC 컴포넌트
    // ──────────────────────────────────────
    private NativeNFC nativeNFC;

    // ──────────────────────────────────────
    // Start
    // ──────────────────────────────────────
    void Start()
    {
#if !UNITY_EDITOR && UNITY_ANDROID
        // NativeNFC 컴포넌트 추가 및 초기화
        nativeNFC = gameObject.AddComponent<AndroidNFC>();
        nativeNFC.Initialize();
        nativeNFC.NFCTagDetected += HandleNFCTag;
        nativeNFC.Enable();

        Debug.Log("NFCManager 초기화 완료");
#else
#endif
    }

    // ──────────────────────────────────────
    // NFC 태그 감지
    // ──────────────────────────────────────
    void HandleNFCTag(NFCTag tag)
    {
        // 카드 ID는 기기마다 대·소문자가 다르게 올 수 있어 소문자로 맞춤
        string tagId = (tag.ID ?? "").Trim().ToLowerInvariant();
        int maxPlayers = DataManager.Instance?.MaxPlayers ?? 4;
        string myJob = DataManager.Instance?.SelectedJob ?? "";
        Debug.Log($"NFC 태그 감지: {tagId} / 인원수: {maxPlayers} / 직업: {myJob}");

        string targetScene = GetTargetScene(tagId, maxPlayers);

        if (targetScene == null)
        {
            Debug.Log("등록되지 않은 카드!");
            ShowToast("등록되지 않은 카드예요.\n수사 카드를 찍어 주세요.");
            return;
        }

        // 직업 체크
        if (!IsAllowedForJob(tagId, myJob, maxPlayers))
        {
            Debug.Log($"이 카드는 {myJob} 직업용이 아닙니다!");
            string myCards = GetMyCardNames(myJob, maxPlayers);
            ShowToast(string.IsNullOrEmpty(myCards)
                ? "내 직업 카드가 아니에요."
                : $"내 직업 카드가 아니에요.\n내 카드: {myCards}");
            return;
        }

        LoadScene(targetScene);
    }

    // ──────────────────────────────────────
    // 직업별 카드 허용 여부
    // ──────────────────────────────────────
    bool IsAllowedForJob(string tagId, string myJob, int maxPlayers)
    {
        if (maxPlayers == 2)
        {
            // 수사관1: 카드 1, 2
            // 수사관2: 카드 3, 4
            if (myJob == "수사관1")
                return tagId == FORENSIC_ID || tagId == CCTV_ID;
            if (myJob == "수사관2")
                return tagId == WITNESS_ID || tagId == CYBER_ID;
        }
        else if (maxPlayers == 3)
        {
            // 수사관1: 카드 1 / 수사관2: 카드 2 / 수사관3: 카드 3 / 공용: 카드 4
            if (myJob == "수사관1")
                return tagId == FORENSIC_ID || tagId == CYBER_ID;
            if (myJob == "수사관2")
                return tagId == CCTV_ID || tagId == CYBER_ID;
            if (myJob == "수사관3")
                return tagId == WITNESS_ID || tagId == CYBER_ID;
        }
        else // 4인
        {
            // 각자 자기 카드만
            if (myJob == "현장감식관") return tagId == FORENSIC_ID;
            if (myJob == "CCTV분석관") return tagId == CCTV_ID;
            if (myJob == "목격자조사관") return tagId == WITNESS_ID;
            if (myJob == "사이버수사관") return tagId == CYBER_ID;
        }

        return false;
    }

    // ──────────────────────────────────────
    // 내 직업이 쓸 수 있는 카드 이름 (안내 문구용)
    // ──────────────────────────────────────
    string GetMyCardNames(string myJob, int maxPlayers)
    {
        var names = new List<string>();
        if (IsAllowedForJob(FORENSIC_ID, myJob, maxPlayers)) names.Add("현장감식");
        if (IsAllowedForJob(CCTV_ID, myJob, maxPlayers)) names.Add("CCTV");
        if (IsAllowedForJob(WITNESS_ID, myJob, maxPlayers)) names.Add("목격자");
        if (IsAllowedForJob(CYBER_ID, myJob, maxPlayers)) names.Add("사이버");
        return string.Join(" · ", names);
    }

    // ──────────────────────────────────────
    // 화면 아래 안내 말풍선 (잘못 찍은 카드 안내)
    // 씬을 고치지 않고 코드에서 만들어 씀
    // ──────────────────────────────────────
    private CanvasGroup toastGroup;
    private TextMeshProUGUI toastText;
    private Sequence toastSeq;

    void ShowToast(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
        if (toastGroup == null && !CreateToast()) return;

        toastText.text = message;
        toastGroup.transform.SetAsLastSibling();

        toastSeq?.Kill();
        toastGroup.alpha = 0f;
        toastGroup.transform.localScale = Vector3.one * 0.9f;
        toastSeq = DOTween.Sequence()
            .Append(toastGroup.DOFade(1f, 0.2f))
            .Join(toastGroup.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack))
            .AppendInterval(2.5f)
            .Append(toastGroup.DOFade(0f, 0.4f));
    }

    bool CreateToast()
    {
        // 이 씬(로비)의 최상위 캔버스 중 가장 앞에 그려지는 것에 붙임
        Canvas canvas = null;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!c.isRootCanvas || c.gameObject.scene != gameObject.scene || !c.isActiveAndEnabled) continue;
            if (canvas == null || c.sortingOrder > canvas.sortingOrder) canvas = c;
        }
        if (canvas == null) return false;

        var panel = new GameObject("NFCToast", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var rt = (RectTransform)panel.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 150f);
        rt.sizeDelta = new Vector2(1100f, 220f);

        var bg = panel.GetComponent<Image>();
        bg.color = new Color32(0x1A, 0x27, 0x51, 0xF2);   // 남색 판 (팝업과 같은 색)
        bg.raycastTarget = false;

        toastGroup = panel.GetComponent<CanvasGroup>();
        toastGroup.alpha = 0f;
        toastGroup.blocksRaycasts = false;
        toastGroup.interactable = false;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        var trt = (RectTransform)textGo.transform;
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(30f, 20f);
        trt.offsetMax = new Vector2(-30f, -20f);

        toastText = textGo.GetComponent<TextMeshProUGUI>();
        var sample = canvas.GetComponentInChildren<TextMeshProUGUI>(true);   // 씬에서 쓰는 글꼴을 그대로 사용
        if (sample != null && sample != toastText) toastText.font = sample.font;
        toastText.fontSize = 56f;
        toastText.enableAutoSizing = true;   // 혹시 길어져도 판 밖으로 나가지 않게
        toastText.fontSizeMin = 36f;
        toastText.fontSizeMax = 56f;
        toastText.alignment = TextAlignmentOptions.Center;
        toastText.color = Color.white;
        toastText.raycastTarget = false;
        return true;
    }

    // ──────────────────────────────────────
    // 인원수별 카드 → 씬 매핑
    // ──────────────────────────────────────
    string GetTargetScene(string tagId, int maxPlayers)
    {
        if (maxPlayers == 2)
        {
            // 수사관1: 카드1 → ForensicScene / 카드2 → CCTVScene
            // 수사관2: 카드3 → WitnessScene  / 카드4 → CyberScene
            if (tagId == FORENSIC_ID) return "ForensicScene";
            if (tagId == CCTV_ID) return "CCTVScene";
            if (tagId == WITNESS_ID) return "WitnessScene";
            if (tagId == CYBER_ID) return "CyberScene";
        }
        else if (maxPlayers == 3)
        {
            // 카드 1 → 수사관1 → ForensicScene
            // 카드 2 → 수사관2 → CCTVScene
            // 카드 3 → 수사관3 → WitnessScene
            // 카드 4 → 공용 → CyberScene
            if (tagId == FORENSIC_ID) return "ForensicScene";
            if (tagId == CCTV_ID) return "CCTVScene";
            if (tagId == WITNESS_ID) return "WitnessScene";
            if (tagId == CYBER_ID) return "CyberScene";
        }
        else // 4인
        {
            // 카드 1 → 현장감식관 → ForensicScene
            // 카드 2 → CCTV분석관 → CCTVScene
            // 카드 3 → 목격자조사관 → WitnessScene
            // 카드 4 → 사이버수사관 → CyberScene
            if (tagId == FORENSIC_ID) return "ForensicScene";
            if (tagId == CCTV_ID) return "CCTVScene";
            if (tagId == WITNESS_ID) return "WitnessScene";
            if (tagId == CYBER_ID) return "CyberScene";
        }

        return null;
    }

    // ──────────────────────────────────────
    // 씬 이동
    // ──────────────────────────────────────
    void LoadScene(string sceneName)
    {
        // NFC 비활성화 후 씬 이동
        if (nativeNFC != null)
        {
            nativeNFC.NFCTagDetected -= HandleNFCTag;
            nativeNFC.Disable();
        }

        SceneManager.LoadScene(sceneName);
    }

    // ──────────────────────────────────────
    // 종료 시 NFC 비활성화
    // ──────────────────────────────────────
    void OnDestroy()
    {
        toastSeq?.Kill();
        if (nativeNFC != null)
        {
            nativeNFC.NFCTagDetected -= HandleNFCTag;
            nativeNFC.Disable();
        }
    }
}