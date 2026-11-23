using UnityEngine;
using System.Collections.Generic;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    // 인원수
    public int MaxPlayers { get; set; } = 4;

    // 내 직업
    public string SelectedJob { get; set; } = "";

    // 수집한 증거 목록
    public List<string> CollectedEvidences { get; set; }
        = new List<string>();

    // 공유된 증거 목록
    public List<string> SharedEvidences { get; set; }
        = new List<string>();

    public bool TutorialShown { get; set; } = false;

    // 수사 도중 연결이 끊겨 연결 화면으로 돌아온 경우 (모은 증거를 지우지 않음)
    public bool ReconnectPending { get; set; } = false;

    // 한 판이 정상적으로 끝나 연결 화면으로 돌아온 경우 (다음 판을 위해 초기화)
    public bool RoundEnded { get; set; } = false;

    // 현재 스테이지
    public int CurrentStage { get; set; } = 1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
