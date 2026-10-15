using UnityEngine;

/// <summary>
/// 용의자 1명의 데이터를 담는 클래스
/// 나중에 ScriptableObject로 분리하면 Unity 인스펙터에서 편집 가능
/// </summary>
[System.Serializable]
public class SuspectData
{
    public string Name;
    public int    Age;
    public string Job;
    public string Feature;
    public Sprite Photo;   // 나중에 사진 추가 시 사용

    public SuspectData(string name, int age, string job, string feature, Sprite photo = null)
    {
        Name    = name;
        Age     = age;
        Job     = job;
        Feature = feature;
        Photo   = photo;
    }
}
