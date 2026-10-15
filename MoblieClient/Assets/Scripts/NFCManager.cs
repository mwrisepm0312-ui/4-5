using UnityEngine;
using UnityEngine.SceneManagement;
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
        string tagId = tag.ID;
        int maxPlayers = DataManager.Instance?.MaxPlayers ?? 4;
        string myJob = DataManager.Instance?.SelectedJob ?? "";
        Debug.Log($"NFC 태그 감지: {tagId} / 인원수: {maxPlayers} / 직업: {myJob}");

        string targetScene = GetTargetScene(tagId, maxPlayers);

        if (targetScene == null)
        {
            Debug.Log("등록되지 않은 카드!");
            return;
        }

        // 직업 체크
        if (!IsAllowedForJob(tagId, myJob, maxPlayers))
        {
            Debug.Log($"이 카드는 {myJob} 직업용이 아닙니다!");
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
        if (nativeNFC != null)
        {
            nativeNFC.NFCTagDetected -= HandleNFCTag;
            nativeNFC.Disable();
        }
    }
}