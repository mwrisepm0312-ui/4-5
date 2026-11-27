using UnityEditor;
using UnityEngine;

// ─────────────────────────────────────────────
// 전시 모드 에디터 테스트 스위치
//
// 메뉴: PeaceMaker → 전시 모드 테스트 (에디터에서만)
//  - 켜면 이 프로젝트를 에디터에서 Play 할 때 전시 모드로 동작 (체크 표시)
//  - 빌드한 프로그램에는 영향 없음 (빌드는 실행할 때 -exhibition 을 붙여야만 켜짐)
//  - PC·모바일 프로젝트에서 각각 따로 켜고 끔
//
// ※ 이 파일은 PC·모바일 두 프로젝트에서 같음
// ─────────────────────────────────────────────
public static class PeaceMakerExhibitionTest
{
    private const string MenuPath = "PeaceMaker/전시 모드 테스트 (에디터에서만)";

    [MenuItem(MenuPath, false, 200)]
    private static void Toggle()
    {
        bool on = !ExhibitionMode.EditorTestOn;
        ExhibitionMode.EditorTestOn = on;
        Menu.SetChecked(MenuPath, on);

        if (on)
            Debug.LogWarning("[전시 모드] 에디터 테스트를 켰어요. 이 프로젝트를 Play 하면 전시 모드로 동작해요. 원래 게임을 테스트할 때는 꼭 꺼 주세요.");
        else
            Debug.Log("[전시 모드] 에디터 테스트를 껐어요. 원래 게임으로 동작해요.");
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, ExhibitionMode.EditorTestOn);
        // Play 중에는 바꾸지 않게 (도중에 바뀌면 상태가 섞임)
        return !EditorApplication.isPlaying;
    }
}
