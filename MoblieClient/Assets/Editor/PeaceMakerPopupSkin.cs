// PeaceMaker — 팝업 스킨 적용 (PC·모바일 공통)
// 위치: Assets/Editor/PeaceMakerPopupSkin.cs   그림: Assets/UI/PopupSkin/
// 메뉴: PeaceMaker → 팝업 스킨 → 지금 열린 씬에 적용 / 빌드 목록의 모든 씬에 적용
//
// 모든 팝업에 같은 남색 틀(금색 안쪽 선·모서리 리벳)을 씌우고, 팝업 성격에 따라 위에 탭을 얹습니다.
//   안내(엠블럼 탭)  : GuidePopup · TutorialPopup · JobPopup · PopupPanel(HostPc)
//   안내(탭 없음)    : 사이버 *Tutorial/Image (제목 글자가 상자 바깥 위에 있어 탭과 겹치므로)
//   증거(초록 탭)    : EvidencePopup · ReceivedEvidencePopup
//   서류(집게 탭)    : StatementPopup · ReportPopup · SuspectPopup
//   안쪽 판(탭 없음) : EvidenceListPopup · KeywordListPanel · *ScrollView
// 틀 위에 바로 놓인 어두운 글자는 흰색으로 바꿉니다 (버튼 안 글자는 그대로).
// 직업 팝업(800×600)은 그림·이름·설명·선택 버튼 배치를 틀 안에 맞춥니다.
// 여러 번 실행해도 결과는 같습니다.
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PeaceMakerPopupSkin
{
    const string Dir = "Assets/UI/PopupSkin/";
    enum Kind { Guide, GuideNoTab, Evidence, Document, Inset }
    static StringBuilder log;
    static int count;

    [MenuItem("PeaceMaker/팝업 스킨/지금 열린 씬에만 적용")]
    static void ApplyCurrent()
    {
        log = new StringBuilder("[PeaceMaker] 팝업 스킨\n"); count = 0;
        if (!CheckSprites()) return;
        var scene = SceneManager.GetActiveScene();
        ApplyScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        log.AppendLine($"→ {count}개 적용. Ctrl+S 로 씬을 저장하세요.");
        Debug.Log(log.ToString());
    }

    [MenuItem("PeaceMaker/팝업 스킨/1. 빌드 목록의 모든 씬에 적용")]
    static void ApplyAll()
    {
        if (!EditorUtility.DisplayDialog("팝업 스킨", "빌드 목록의 모든 씬을 열어 팝업 그림을 바꾸고 저장합니다.\n먼저 프로젝트를 백업했는지 확인하세요.", "적용", "취소")) return;
        log = new StringBuilder("[PeaceMaker] 팝업 스킨\n"); count = 0;
        if (!CheckSprites()) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string start = SceneManager.GetActiveScene().path;
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (!s.enabled || !System.IO.File.Exists(s.path)) continue;
            var scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
            int before = count;
            ApplyScene(scene);
            if (count > before) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        }
        if (!string.IsNullOrEmpty(start)) EditorSceneManager.OpenScene(start);
        log.AppendLine($"→ 모두 {count}개 적용, 씬 저장 완료");
        Debug.Log(log.ToString());
    }

    static bool CheckSprites()
    {
        foreach (var n in new[] { "popup_frame", "panel_inset", "tab_guide", "tab_evidence", "tab_document" })
            if (Load(n) == null)
            {
                EditorUtility.DisplayDialog("팝업 스킨", $"{Dir}{n}.png 을 찾지 못했습니다.\n묶음의 Assets/UI/PopupSkin 폴더를 프로젝트에 넣었는지 확인하세요.", "확인");
                return false;
            }
        return true;
    }

    static Sprite Load(string n) => AssetDatabase.LoadAssetAtPath<Sprite>(Dir + n + ".png");

    static Kind? KindOf(Transform t)
    {
        string n = t.name;
        if (n == "EvidenceListPopup" || n == "KeywordListPanel" || n.EndsWith("ScrollView")) return Kind.Inset;
        if (n == "GuidePopup" || n == "TutorialPopup" || n == "JobPopup" || n == "PopupPanel") return Kind.Guide;
        if (n == "Image" && t.parent != null && t.parent.name.EndsWith("Tutorial")) return Kind.GuideNoTab;
        if (n == "EvidencePopup" || n == "ReceivedEvidencePopup") return Kind.Evidence;
        if (n == "StatementPopup" || n == "ReportPopup" || n == "SuspectPopup") return Kind.Document;
        return null;
    }

    static void ApplyScene(Scene scene)
    {
        log.AppendLine($"■ {scene.name}");
        foreach (var root in scene.GetRootGameObjects())
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            var k = KindOf(t);
            if (k == null) continue;
            var img = t.GetComponent<Image>();
            if (img == null) continue;
            Apply(t, img, k.Value);
        }
    }

    static void Apply(Transform t, Image img, Kind k)
    {
        Undo.RecordObject(img, "Popup Skin");
        img.sprite = Load(k == Kind.Inset ? "panel_inset" : "popup_frame");
        img.type = Image.Type.Sliced;
        img.fillCenter = true;
        img.pixelsPerUnitMultiplier = k == Kind.Inset ? 2f : 1.4f;     // 테두리 굵기 (클수록 얇아짐)
        img.color = Color.white;

        string tab = k == Kind.Guide ? "tab_guide" : k == Kind.Evidence ? "tab_evidence" : k == Kind.Document ? "tab_document" : null;
        if (tab != null) AddTab(t, tab);
        if (t.name == "JobPopup") LayoutJobPopup(t);
        int fixedTexts = FixTexts(t);

        // HostPc 의 옛 Back.png (실제 경찰 마크 워터마크) 끄기
        foreach (var raw in t.GetComponentsInChildren<RawImage>(true))
            if (raw.texture != null && raw.texture.name == "Back" && raw.gameObject.activeSelf)
            { Undo.RecordObject(raw.gameObject, "Popup Skin"); raw.gameObject.SetActive(false); log.AppendLine($"    {raw.name}: 옛 Back.png 그림 끔"); }
        foreach (var im in t.GetComponentsInChildren<Image>(true))
            if (im != img && im.sprite != null && im.sprite.name == "Back" && im.gameObject.activeSelf)
            { Undo.RecordObject(im.gameObject, "Popup Skin"); im.gameObject.SetActive(false); log.AppendLine($"    {im.name}: 옛 Back.png 그림 끔"); }

        count++;
        log.AppendLine($"  - {Path(t)} → {k}" + (fixedTexts > 0 ? $" (어두운 글자 {fixedTexts}개 → 흰색)" : ""));
    }

    // 직업 팝업 800×600: 틀 테두리·탭 안쪽으로 그림 400×225 · 이름 · 설명 · 선택 버튼 300×100
    static void LayoutJobPopup(Transform popup)
    {
        void Set(string child, Vector2? size, float y)
        {
            var c = popup.Find(child) as RectTransform;
            if (c == null) return;
            Undo.RecordObject(c, "Popup Skin");
            if (size.HasValue) c.sizeDelta = size.Value;
            c.anchoredPosition = new Vector2(c.anchoredPosition.x, y);
        }
        Set("PopupJobImage", new Vector2(400, 225), -165f);    // 위 기준 (-52 ~ -277)
        Set("PopupJobName", null, -320f);
        Set("PopupJobDesc", null, -385f);
        Set("SelectButton", new Vector2(300, 100), 75f);       // 아래 기준 (25 ~ 125)
        log.AppendLine("    직업 팝업 배치: 그림 400×225 · 이름 · 설명 · 선택 버튼 300×100");
    }

    static void AddTab(Transform popup, string sprite)
    {
        var tabT = popup.Find("PopupTab");
        GameObject go;
        if (tabT == null)
        {
            go = new GameObject("PopupTab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Popup Skin");
            go.layer = popup.gameObject.layer;
            go.transform.SetParent(popup, false);
        }
        else { go = tabT.gameObject; Undo.RecordObject(go.transform, "Popup Skin"); }
        go.transform.SetAsFirstSibling();                        // 틀 바로 위, 제목·버튼보다 아래
        var img = go.GetComponent<Image>();
        Undo.RecordObject(img, "Popup Skin");
        img.sprite = Load(sprite); img.type = Image.Type.Simple; img.preserveAspect = true;
        img.raycastTarget = false; img.color = Color.white;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(216, 119);
        rt.anchoredPosition = new Vector2(0, -26);               // 틀 위쪽 가장자리에 살짝 걸침
        rt.localScale = Vector3.one;
    }

    // 남색 틀 위에 바로 놓인 어두운 글자 → 흰색
    static int FixTexts(Transform root)
    {
        int n = 0;
        foreach (var g in root.GetComponentsInChildren<Graphic>(true))
        {
            bool isText = g is Text || g.GetType().Name.Contains("TextMeshPro");
            if (!isText) continue;
            var c = g.color;
            if (c.r * 0.3f + c.g * 0.59f + c.b * 0.11f > 0.45f) continue;   // 이미 밝은 글자
            if (OnLightSurface(g.transform, root)) continue;
            Undo.RecordObject(g, "Popup Skin");
            g.color = new Color(1, 1, 1, c.a);
            n++;
        }
        return n;
    }

    // 글자와 팝업 사이에 버튼·밝은 판이 있으면 건드리지 않음
    static bool OnLightSurface(Transform t, Transform root)
    {
        for (var p = t.parent; p != null && p != root; p = p.parent)
        {
            if (p.GetComponent<Selectable>() != null) return true;
            var im = p.GetComponent<Image>();
            if (im != null && im.enabled && KindOf(p) == null)
            {
                var c = im.color;
                if (c.a > 0.5f && c.r * 0.3f + c.g * 0.59f + c.b * 0.11f > 0.6f) return true;
            }
        }
        return false;
    }

    static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
}
