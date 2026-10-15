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
