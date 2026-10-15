using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class MobileJobSelect : MonoBehaviour
{
    public static MobileJobSelect Instance;

    // ──────────────────────────────────────
    // 상태 텍스트
    // ──────────────────────────────────────
    [Header("상태 텍스트")]
    public TextMeshProUGUI statusText;

    // ──────────────────────────────────────
    // 2인용 JobGrid
    // ──────────────────────────────────────
    [Header("2인용")]
    public GameObject jobGrid2;
    public Button jobCard2_1;
    public Button jobCard2_2;

    // ──────────────────────────────────────
    // 3인용 JobGrid
    // ──────────────────────────────────────
    [Header("3인용")]
    public GameObject jobGrid3;
    public Button jobCard3_1;
    public Button jobCard3_2;
    public Button jobCard3_3;

    // ──────────────────────────────────────
    // 4인용 JobGrid
    // ──────────────────────────────────────
    [Header("4인용")]
    public GameObject jobGrid4;
    public Button jobCard4_1;
    public Button jobCard4_2;
    public Button jobCard4_3;
    public Button jobCard4_4;

    // ──────────────────────────────────────
    // 팝업
    // ──────────────────────────────────────
    [Header("직업 팝업")]
    public GameObject jobPopup;
    public RawImage popupJobImage;
    public TextMeshProUGUI popupJobName;
    public TextMeshProUGUI popupJobDesc;
    public Button selectButton;
    public Button closeButton; 

    // ──────────────────────────────────────
    // 직업 데이터
    // ──────────────────────────────────────
    private string selectedJob = "";
    private string pendingJob = "";
    private bool isLocked = true;

    private string[] jobs2 = { "수사관1", "수사관2" };
    private string[] jobs3 = { "수사관1", "수사관2", "수사관3" };
    private string[] jobs4 = { "현장감식관", "CCTV분석관", "목격자조사관", "사이버수사관" };

    private string[] desc2 = {
        "현장과 디지털 증거를 분석하는 수사관",
        "목격자와 영상을 분석하는 수사관"
    };
    private string[] desc3 = {
        "현장과 디지털 증거를 분석하는 수사관",
        "CCTV 영상으로 동선을 파악하는 수사관",
        "목격자 진술을 분석하는 수사관"
    };
    private string[] desc4 = {
        "범죄 현장에서 지문을 채취하고 AFIS로 용의자를 특정합니다",
        "CCTV 영상을 분석하여 용의자의 동선을 파악합니다",
        "목격자 진술을 수집하고 모순점을 찾아냅니다",
        "삭제된 디지털 파일을 복원하여 증거를 찾아냅니다"
    };

    private string[] images2 = { "Jobs/수사관1", "Jobs/수사관2" };
    private string[] images3 = { "Jobs/수사관1", "Jobs/수사관2", "Jobs/수사관3" };
    private string[] images4 = { "Jobs/현장감식관", "Jobs/CCTV분석관", "Jobs/목격자조사관", "Jobs/사이버수사관" };

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        jobPopup.SetActive(false);
        statusText.text = "PC에서 오프닝 영상 재생 중입니다.\n잠시 기다려주세요!";

        int maxPlayers = DataManager.Instance.MaxPlayers;
        jobGrid2.SetActive(maxPlayers == 2);
        jobGrid3.SetActive(maxPlayers == 3);
        jobGrid4.SetActive(maxPlayers == 4);

        jobCard2_1.onClick.AddListener(() => OnJobCardClicked(jobs2[0], desc2[0], images2[0]));
        jobCard2_2.onClick.AddListener(() => OnJobCardClicked(jobs2[1], desc2[1], images2[1]));

        jobCard3_1.onClick.AddListener(() => OnJobCardClicked(jobs3[0], desc3[0], images3[0]));
        jobCard3_2.onClick.AddListener(() => OnJobCardClicked(jobs3[1], desc3[1], images3[1]));
        jobCard3_3.onClick.AddListener(() => OnJobCardClicked(jobs3[2], desc3[2], images3[2]));

        jobCard4_1.onClick.AddListener(() => OnJobCardClicked(jobs4[0], desc4[0], images4[0]));
        jobCard4_2.onClick.AddListener(() => OnJobCardClicked(jobs4[1], desc4[1], images4[1]));
        jobCard4_3.onClick.AddListener(() => OnJobCardClicked(jobs4[2], desc4[2], images4[2]));
        jobCard4_4.onClick.AddListener(() => OnJobCardClicked(jobs4[3], desc4[3], images4[3]));

        selectButton.onClick.AddListener(OnSelectClicked);

        closeButton.onClick.AddListener(() =>
        {
            jobPopup.SetActive(false);
            pendingJob = "";
        });

        var gm = FindFirstObjectByType<GameManager>();
        gm?.RequestJobStatusServerRpc();
    }

    void OnJobCardClicked(string jobName, string desc, string imagePath)
    {
        if (isLocked)
        {
            statusText.text = "PC에서 오프닝 영상 재생 중입니다.\n잠시 기다려주세요!";
            return;
        }

        if (selectedJob != "") return;

        pendingJob = jobName;

        popupJobName.text = jobName;
        popupJobDesc.text = desc;

        Texture2D tex = Resources.Load<Texture2D>(imagePath);
        if (tex != null && popupJobImage != null)
            popupJobImage.texture = tex;

        jobPopup.SetActive(true);
    }

    void OnSelectClicked()
    {
        if (pendingJob == "") return;

        jobPopup.SetActive(false);
        statusText.text = $"{pendingJob} 선택 중...";

        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
            gm.SelectJobServerRpc(pendingJob);
        else
            statusText.text = "연결 오류!";
    }

    public void UnlockJobSelect()
    {
        isLocked = false;
        statusText.text = "직업을 선택하세요!";
        DOTween.Restart("cardDrop");
    }

    public void OnJobConfirmed(string jobName)
    {
        selectedJob = jobName;
        statusText.text = $"{jobName} 선택 완료!";
        DataManager.Instance.SelectedJob = jobName;
        SceneManager.LoadScene("Mobile_LobbyScene");
    }

    public void OnJobRejected(string jobName)
    {
        jobPopup.SetActive(false);
        pendingJob = "";
        statusText.text = $"{jobName}은 이미 선택됐어요!\n다른 직업을 선택하세요.";
    }

    public void OnJobStatusUpdated(string jobName, bool isTaken)
    {
        int max = DataManager.Instance.MaxPlayers;
        Button btn = null;

        if (max == 2)
        {
            if (jobName == jobs2[0]) btn = jobCard2_1;
            else if (jobName == jobs2[1]) btn = jobCard2_2;
        }
        else if (max == 3)
        {
            if (jobName == jobs3[0]) btn = jobCard3_1;
            else if (jobName == jobs3[1]) btn = jobCard3_2;
            else if (jobName == jobs3[2]) btn = jobCard3_3;
        }
        else
        {
            if (jobName == jobs4[0]) btn = jobCard4_1;
            else if (jobName == jobs4[1]) btn = jobCard4_2;
            else if (jobName == jobs4[2]) btn = jobCard4_3;
            else if (jobName == jobs4[3]) btn = jobCard4_4;
        }

        if (btn != null)
            btn.interactable = !isTaken;
    }
}