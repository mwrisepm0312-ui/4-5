using UnityEngine;

// ─────────────────────────────────────────────
// 전시 모드 (PC 한 대에서 PC 화면 + 휴대폰 창 4개를 마우스로 체험)
//
// ※ 이 파일은 PC·모바일 두 프로젝트에서 글자 하나까지 같아야 함
//
// 켜지는 경우는 두 가지뿐:
//  1) Windows 빌드를 실행할 때 "-exhibition" 을 붙인 경우 (전시 실행기가 붙여 줌)
//  2) Unity 에디터에서 "PeaceMaker → 전시 모드 테스트" 메뉴를 켠 경우 (그 프로젝트에서만)
//
// 안드로이드 휴대폰에서는 절대 켜지지 않음 → 원래 게임은 그대로
// 그냥 더블클릭해서 실행한 PC 프로그램도 꺼진 상태 → 원래 게임은 그대로
//
// 실행 인자
//  -exhibition   전시 모드 켜기
//  -player N     휴대폰 창 번호 (1~4). PC 프로그램은 0
// ─────────────────────────────────────────────
public static class ExhibitionMode
{
    // 전시 모드에서 휴대폰 창이 바로 연결하는 주소 (같은 PC)
    public const string ServerAddress = "127.0.0.1";
    public const int ServerPort = 7770;

    private static bool initialized = false;
    private static bool isOn = false;
    private static int playerNumber = 0;

    // 전시 모드인지
    public static bool IsOn
    {
        get { Init(); return isOn; }
    }

    // 휴대폰 창 번호 (1~4). PC 프로그램이거나 번호가 없으면 0
    public static int PlayerNumber
    {
        get { Init(); return playerNumber; }
    }

    private static void Init()
    {
        if (initialized) return;
        initialized = true;
        isOn = false;
        playerNumber = 0;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i].ToLowerInvariant();
            if (a == "-exhibition")
                isOn = true;
            else if (a == "-player" && i + 1 < args.Length && int.TryParse(args[i + 1], out int n))
                playerNumber = Mathf.Clamp(n, 0, 4);
        }
#endif

#if UNITY_EDITOR
        if (EditorTestOn)
        {
            isOn = true;
            Debug.LogWarning("[전시 모드] 에디터 테스트로 켜져 있어요. 원래 게임을 테스트하려면 PeaceMaker → 전시 모드 테스트 메뉴를 꺼 주세요.");
        }
#endif

        if (isOn)
            Debug.Log($"[전시 모드] 켜짐 / 휴대폰 창 번호: {playerNumber}");
    }

#if UNITY_EDITOR
    // 에디터 테스트 스위치 (프로젝트마다 따로 저장 → PC·모바일 프로젝트에서 각각 켜야 함)
    private static string EditorPrefKey => "PeaceMaker.ExhibitionTest." + Application.dataPath.GetHashCode();

    public static bool EditorTestOn
    {
        get => UnityEditor.EditorPrefs.GetBool(EditorPrefKey, false);
        set
        {
            UnityEditor.EditorPrefs.SetBool(EditorPrefKey, value);
            initialized = false;
        }
    }
#endif
}
