// PeaceMaker — 버튼 스킨 (PC·모바일 같은 파일)
// 메뉴: PeaceMaker → 버튼 스킨 → 1. 미리 보기 (모든 씬, 바꾸지 않음) / 2. 빌드 목록의 모든 씬에 적용 / 지금 열린 씬에만 적용
//
// 버튼 이름과 글자를 보고 모양을 고릅니다.
//  - 글자가 그림에 들어 있는 버튼(연결·선택·시작하기·게임 종료 등): 그림 그대로 + 눌림 그림 + 3:1 크기
//  - 나머지: 노랑(진행)·초록(증거)·남색(송신·AFIS·레포트)·빨강(종료·검거)·흰색(닫기) 알약 버튼
//  - 작은 닫기 X, 느낌표 !, 목록 줄, 직업·용의자 카드 테두리, 누름 효과(ButtonPressFeedback)
//  - 현장감식 게이지 막대 그림 연결 (지워진 그림을 가리키던 것)
// 모바일 씬에서만 높이 70 보다 작은 버튼을 70 으로 키웁니다 (PC 는 크기 유지).
// 여러 번 실행해도 결과는 같습니다.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PeaceMakerButtonSkin
{
    private const string SkinDir = "Assets/UI/ButtonSkin/";
    private const string PressedDir = "Assets/Images/눌림/";
    private const float PillHeight = 240f;   // 스킨 그림 높이(px)
    private const float PillLip = 30f;       // 받침 두께(px)
    private const float MinHeight = 70f;     // 아이들이 누르기 쉬운 최소 높이 (모바일)

    private static readonly HashSet<string> MobileScenes = new HashSet<string>
        { "ClientMobile", "JobSelectScene", "Mobile_LobbyScene", "CCTVScene", "ForensicScene", "WitnessScene", "CyberScene" };

    // 글자가 그림 안에 들어 있는 버튼 그림 (스킨 대신 눌림 그림만 연결)
    private static readonly string[] BakedSprites =
    {
        // 모바일
        "Connect", "Select", "시작하기 버튼 이미지", "레포트 버튼 이미지", "수집한 증거 버튼 이미지", "ExitGame",
        // PC
        "Start", "선택 버튼", "확인 버튼", "용의자 버튼", "용의자 특정 버튼", "증거 목록 버튼"
    };

    private enum Kind { Skip, Baked, Pill, Close, Exclaim, Item, Card, Feedback }

    private struct Plan
    {
        public Button button;
        public Kind kind;
        public string theme;
        public string label;
    }

    private static StringBuilder log;

    [MenuItem("PeaceMaker/버튼 스킨/1. 미리 보기 (모든 씬, 바꾸지 않음)")]
    private static void PreviewAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string current = SceneManager.GetActiveScene().path;
        log = new StringBuilder("[PeaceMaker] 버튼 스킨 미리 보기 (아직 바꾸지 않음)\n");
        int total = 0;
        foreach (var s in EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)))
        {
            var scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
            var plans = Collect(scene);
            log.AppendLine($"■ {scene.name} ({plans.Count}개)");
            foreach (var p in plans)
                log.AppendLine($"  {Path(p.button.transform)}  →  {Describe(p)}  (\"{p.label}\")");
            total += plans.Count;
        }
        if (!string.IsNullOrEmpty(current)) EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
        log.AppendLine($"→ 버튼 {total}개. 메뉴 '2. 빌드 목록의 모든 씬에 적용' 으로 적용하세요.");
        Debug.Log(log.ToString());
    }

    [MenuItem("PeaceMaker/버튼 스킨/2. 빌드 목록의 모든 씬에 적용")]
    private static void ApplyAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!EditorUtility.DisplayDialog("버튼 스킨", "빌드 목록의 모든 씬의 버튼을 바꾸고 저장합니다.\n작업 중인 씬이 있으면 먼저 저장하세요.", "적용", "취소")) return;

        string current = SceneManager.GetActiveScene().path;
        log = new StringBuilder("[PeaceMaker] 버튼 스킨 적용 결과\n");
        int total = 0;
        foreach (var s in EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)))
        {
            var scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
            int n = ApplyScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            log.AppendLine($"■ {scene.name}: {n}개");
            total += n;
        }
        if (!string.IsNullOrEmpty(current)) EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
        log.AppendLine($"→ 모든 씬 완료: 버튼 {total}개");
        Debug.Log(log.ToString());
    }

    [MenuItem("PeaceMaker/버튼 스킨/지금 열린 씬에만 적용")]
    private static void ApplyCurrent()
    {
        var scene = SceneManager.GetActiveScene();
        log = new StringBuilder("[PeaceMaker] 버튼 스킨\n");
        int n = ApplyScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        log.AppendLine($"■ {scene.name}: {n}개 적용. 확인 후 저장(Ctrl+S)하세요.");
        Debug.Log(log.ToString());
    }

    // ───────────────────────── 분류 ─────────────────────────
    private static List<Plan> Collect(Scene scene)
    {
        var list = new List<Plan>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var b in root.GetComponentsInChildren<Button>(true))
            {
                string label = string.Join(" ", b.GetComponentsInChildren<TMP_Text>(true).Select(t => t.text.Trim()).Where(t => t.Length > 0));
                list.Add(Classify(b, label));
            }
        return list;
    }

    private static Plan Classify(Button b, string label)
    {
        var p = new Plan { button = b, label = label, theme = "" };
        string n = b.name;
        var img = b.targetGraphic as Image;
        var rt = (RectTransform)b.transform;
        float w = rt.rect.width, h = rt.rect.height;

        if (img != null && img.sprite != null && BakedSprites.Contains(img.sprite.name)) { p.kind = Kind.Baked; return p; }
        if (n.Contains("MonitorBtn") || n.Contains("FileIcon") || n.Contains("WitnessBtn") || n.Contains("Fragment"))
        { p.kind = Kind.Feedback; return p; }
        if (n.Contains("JobCard") || n.Contains("SuspectCard")) { p.kind = Kind.Card; return p; }
        if (n.Contains("Exclamation") || label == "!" || label == "'!'") { p.kind = Kind.Exclaim; return p; }
        if (n.Contains("ItemTemplate") || n.Contains("Keyword")) { p.kind = Kind.Item; return p; }
        if (label == "X" || label == "x" || (n.Contains("Close") && Mathf.Max(w, h) <= 90f)) { p.kind = Kind.Close; return p; }

        p.kind = Kind.Pill;
        if (label.Contains("종료") || label.Contains("검거")) p.theme = "red";
        else if (label.Contains("닫기") || n.Contains("Close")) p.theme = "white";
        else if (label.Contains("증거") || n.Contains("Evidence")) p.theme = "green";
        else if (label.Contains("송신") || label.Contains("AFIS") || label.Contains("레포트") || n.Contains("Send") || n.Contains("Report")) p.theme = "navy";
        else p.theme = "yellow";
        return p;
    }

    private static string Describe(Plan p)
    {
        switch (p.kind)
        {
            case Kind.Baked: return "글자 든 그림 + 눌림 그림";
            case Kind.Pill: return "알약 " + p.theme;
            case Kind.Close: return "닫기 X";
            case Kind.Exclaim: return "느낌표 !";
            case Kind.Item: return "목록 줄";
            case Kind.Card: return "카드 테두리";
            case Kind.Feedback: return "누름 효과만";
            default: return "그대로";
        }
    }

    // ───────────────────────── 적용 ─────────────────────────
    private static int ApplyScene(Scene scene)
    {
        bool mobile = MobileScenes.Contains(scene.name);
        int count = 0;
        foreach (var p in Collect(scene))
        {
            Undo.RegisterFullObjectHierarchyUndo(p.button.gameObject, "버튼 스킨");
            switch (p.kind)
            {
                case Kind.Baked: ApplyBaked(p.button, scene.name); break;
                case Kind.Pill: ApplyPill(p.button, p.theme, mobile); break;
                case Kind.Close: ApplyRound(p.button, "btn_close"); break;
                case Kind.Exclaim: ApplyRound(p.button, "btn_exclaim"); Feedback(p.button, 0f, true); break;
                case Kind.Item: ApplyItem(p.button); break;
                case Kind.Card: ApplyCard(p.button); break;
                case Kind.Feedback: Feedback(p.button, 0f, false); break;
                default: continue;
            }
            EditorUtility.SetDirty(p.button.gameObject);
            count++;
        }
        if (scene.name == "ForensicScene") FixGauge();
        return count;
    }

    private static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(SkinDir + name + ".png");

    private static void SpriteSwap(Button b, Sprite pressed)
    {
        b.transition = Selectable.Transition.SpriteSwap;
        b.spriteState = new SpriteState { pressedSprite = pressed, highlightedSprite = null, selectedSprite = null, disabledSprite = null };
    }

    private static void ApplyPill(Button b, string theme, bool mobile)
    {
        var img = b.targetGraphic as Image;
        if (img == null) return;
        var rt = (RectTransform)b.transform;
        bool inLayout = rt.parent != null && rt.parent.GetComponent<LayoutGroup>() != null;
        if (mobile && !inLayout && rt.rect.height < MinHeight)
            rt.sizeDelta = new Vector2(Mathf.Max(rt.sizeDelta.x, 160f), MinHeight);

        img.sprite = Load("btn_" + theme);
        img.type = Image.Type.Sliced;
        img.preserveAspect = false;
        img.color = Color.white;
        img.pixelsPerUnitMultiplier = PillHeight / Mathf.Max(1f, rt.rect.height);
        SpriteSwap(b, Load("btn_" + theme + "_pressed"));

        bool dark = theme == "yellow" || theme == "white";
        foreach (var t in b.GetComponentsInChildren<TMP_Text>(true))
        {
            t.color = dark ? new Color32(34, 38, 58, 255) : Color.white;
            t.fontStyle |= FontStyles.Bold;
            t.enableAutoSizing = true;
            t.fontSizeMin = 16; t.fontSizeMax = Mathf.Max(t.fontSizeMax, rt.rect.height * 0.4f);
            t.alignment = TextAlignmentOptions.Center;
            // 받침 두께만큼 글자를 위로 (버튼 윗면 가운데)
            t.margin = new Vector4(rt.rect.height * 0.12f, 0, 0, rt.rect.height * PillLip / PillHeight);
        }
        Feedback(b, rt.rect.height * PillLip / PillHeight, false);
    }

    private static void ApplyRound(Button b, string name)
    {
        var img = b.targetGraphic as Image;
        if (img == null) return;
        img.sprite = Load(name);
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        img.color = Color.white;
        SpriteSwap(b, Load(name + "_pressed"));
        foreach (var t in b.GetComponentsInChildren<TMP_Text>(true)) t.gameObject.SetActive(false); // X·! 는 그림에 있음
        if (name == "btn_close") Feedback(b, 0f, false);
    }

    private static void ApplyItem(Button b)
    {
        var img = b.targetGraphic as Image;
        if (img == null) return;
        var rt = (RectTransform)b.transform;
        img.sprite = Load("item");
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        img.pixelsPerUnitMultiplier = 160f / Mathf.Max(1f, rt.rect.height);
        SpriteSwap(b, Load("item_selected"));
        foreach (var t in b.GetComponentsInChildren<TMP_Text>(true))
        {
            t.color = new Color32(34, 38, 58, 255);
            t.fontStyle |= FontStyles.Bold;
            t.margin = new Vector4(rt.rect.height * 0.6f, 0, 10, 0);
        }
        Feedback(b, 0f, false);
    }

    private static void ApplyCard(Button b)
    {
        // 카드 그림 위에 흰 테두리(SkinFrame) 를 덧씌움
        Transform frameT = b.transform.Find("SkinFrame");
        Image frame;
        if (frameT == null)
        {
            var go = new GameObject("SkinFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "버튼 스킨");
            go.layer = b.gameObject.layer;
            go.transform.SetParent(b.transform, false);
            var frt = (RectTransform)go.transform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = frt.offsetMax = Vector2.zero;
            frame = go.GetComponent<Image>();
        }
        else frame = frameT.GetComponent<Image>();
        frame.sprite = Load("card_frame");
        frame.type = Image.Type.Sliced;
        frame.raycastTarget = false;
        frame.pixelsPerUnitMultiplier = 300f / Mathf.Max(1f, ((RectTransform)b.transform).rect.height);
        frame.transform.SetSiblingIndex(0);          // 이름 글자보다 아래에 그려지도록
        var cardImg = b.targetGraphic as Image;
        if (cardImg != null && cardImg.sprite != null && AssetDatabase.GetAssetPath(cardImg.sprite).StartsWith("Assets/"))
            cardImg.preserveAspect = true;
        Feedback(b, 0f, false);
    }

    private static void ApplyBaked(Button b, string sceneName)
    {
        var img = (Image)b.targetGraphic;

        // PC 시작 화면 '확인' 버튼이 '선택' 그림을 쓰고 있던 것 → '확인' 그림으로
        if (sceneName == "HostPc" && b.name == "ConfirmButton" && img.sprite.name == "선택 버튼")
        {
            var ok = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/확인 버튼.png");
            if (ok != null) { img.sprite = ok; log?.AppendLine($"  {b.name}: '선택' 그림 → '확인' 그림"); }
        }

        img.preserveAspect = true;
        img.type = Image.Type.Simple;
        img.color = Color.white;
        var pressed = AssetDatabase.LoadAssetAtPath<Sprite>(PressedDir + img.sprite.name + "_눌림.png");
        if (pressed != null) SpriteSwap(b, pressed);
        foreach (var t in b.GetComponentsInChildren<TMP_Text>(true))
            if (string.IsNullOrWhiteSpace(t.text) || t.text == "Button") t.gameObject.SetActive(false);

        // 칸을 그림 비율(3:1)에 맞춤: 높이 90~120 (직업 팝업 안은 100), 너비 = 높이 × 3
        var rt = (RectTransform)b.transform;
        bool inLayout = rt.parent != null && rt.parent.GetComponent<LayoutGroup>() != null;
        if (!inLayout && rt.anchorMin == rt.anchorMax)
        {
            float ratio = img.sprite.rect.width / img.sprite.rect.height;
            float hh = Mathf.Clamp(rt.sizeDelta.y, 90f, 120f);
            if (rt.parent != null && rt.parent.name == "JobPopup") hh = 100f;
            var size = new Vector2(Mathf.Round(hh * ratio), hh);
            if (rt.sizeDelta != size)
            {
                log?.AppendLine($"  {b.name}: {rt.sizeDelta.x:0}×{rt.sizeDelta.y:0} → {size.x:0}×{size.y:0} (그림 비율)");
                rt.sizeDelta = size;
                KeepInside(rt);
            }
        }
        Feedback(b, 0f, false);
    }

    // 화면 가장자리에 붙은 버튼이 커지면서 밖으로 나가지 않게
    private static void KeepInside(RectTransform rt)
    {
        const float m = 20f;
        var p = rt.anchoredPosition;
        Vector2 half = rt.sizeDelta * 0.5f;
        if (rt.anchorMin.x == 1f) p.x = Mathf.Min(p.x, -(half.x + m));
        if (rt.anchorMin.x == 0f) p.x = Mathf.Max(p.x, half.x + m);
        if (rt.anchorMin.y == 1f) p.y = Mathf.Min(p.y, -(half.y + m));
        if (rt.anchorMin.y == 0f) p.y = Mathf.Max(p.y, half.y + m);
        if (p != rt.anchoredPosition) rt.anchoredPosition = p;
    }

    // 현장감식 게이지: 지워진 그림을 가리키던 GaugeFill 과 빈 GaugeBackground 에 막대 그림 연결
    private static void FixGauge()
    {
        GameObject bg = null, fill = null;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "GaugeBackground") bg = t.gameObject;
                if (t.name == "GaugeFill") fill = t.gameObject;
            }
        var bgImg = bg != null ? bg.GetComponent<Image>() : null;
        var fillImg = fill != null ? fill.GetComponent<Image>() : null;
        if (bgImg != null)
        {
            Undo.RecordObject(bgImg, "버튼 스킨");
            bgImg.sprite = Load("gauge_bg"); bgImg.type = Image.Type.Sliced; bgImg.color = Color.white;
            bgImg.pixelsPerUnitMultiplier = 80f / Mathf.Max(1f, ((RectTransform)bg.transform).rect.height);
        }
        if (fillImg != null)
        {
            Undo.RecordObject(fillImg, "버튼 스킨");
            fillImg.sprite = Load("gauge_fill"); fillImg.color = Color.white;   // 채우기(Filled) 방식은 그대로
        }
        if (bgImg != null || fillImg != null) log?.AppendLine("  게이지 막대 그림 연결 (GaugeBackground · GaugeFill)");
    }

    private static void Feedback(Button b, float drop, bool pulse)
    {
        var f = b.GetComponent<ButtonPressFeedback>();
        if (f == null) f = Undo.AddComponent<ButtonPressFeedback>(b.gameObject);
        f.textDrop = drop;
        f.pulse = pulse;
        f.pressScale = 0.96f;
    }

    private static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
}
