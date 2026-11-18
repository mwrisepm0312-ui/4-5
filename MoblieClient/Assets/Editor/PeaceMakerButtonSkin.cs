// PeaceMaker — 버튼 스킨 (PC·모바일 같은 파일)
// 메뉴: PeaceMaker → 버튼 스킨 → 1. 미리 보기 (모든 씬, 바꾸지 않음) / 2. 빌드 목록의 모든 씬에 적용 / 지금 열린 씬에만 적용
//
// 버튼 이름과 글자를 보고 모양을 고릅니다.
//  - 글자가 그림에 들어 있는 버튼(연결·선택·시작하기·게임 종료 등): 그림 그대로 + 눌림 그림 + 3:1 크기
//  - 나머지: 노랑(진행)·초록(증거)·남색(송신·AFIS·레포트)·빨강(종료·검거)·흰색(닫기) 알약 버튼
//  - 작은 닫기 X, 목록 줄, 직업·용의자 카드 테두리, 누름 효과(ButtonPressFeedback)
//  - 배경이 투명한 버튼(현장감식 ! 등)은 건드리지 않음. 예전 판으로 노란 ! 가 된 것은 원래대로 되돌림
//  - 정사각형에 가깝거나 글자가 + − ◀ ▶ 인 버튼: 노란 동그라미 (알약을 정사각형에 늘리면 모양이 뭉개지므로)
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
    private const float FaceLift = 0.10f;    // 아래 그늘만큼 글자를 위로 (버튼 높이 비율)
    private const float MinHeight = 70f;     // 아이들이 누르기 쉬운 최소 높이 (모바일)

    private static readonly HashSet<string> MobileScenes = new HashSet<string>
        { "ClientMobile", "JobSelectScene", "Mobile_LobbyScene", "CCTVScene", "ForensicScene", "WitnessScene", "CyberScene" };

    // 글자가 그림 안에 들어 있는 버튼 그림 (스킨 대신 눌림 그림만 연결)
    private static readonly string[] BakedSprites =
    {
        // 모바일
        "Connect", "Select", "시작하기 버튼 이미지", "레포트 버튼 이미지", "수집한 증거 버튼 이미지", "ExitGame",
        // PC
        "Start", "선택 버튼", "확인 버튼", "용의자 버튼", "용의자 특정 버튼", "증거 목록 버튼", "검거 버튼"
    };

    // 화면별 주요 버튼의 최종 크기·위치 (1920×1080 기준, 앵커는 원래대로 두고 그 기준 위치)
    //  씬, 이름, 너비, 높이, x, y, 바꿀 그림(없으면 null)
    private struct Spot { public string scene, name, sprite; public float w, h, x, y; }
    private static readonly Spot[] Layout =
    {
        // PC 시작 화면: 게임 시작·게임 종료 같은 크기로 위아래, 인원 팝업 확인
        new Spot { scene = "HostPc", name = "StartButton", w = 420, h = 140, x = 0, y = -175 },
        new Spot { scene = "HostPc", name = "ExitButton", w = 420, h = 140, x = 0, y = 200 },
        new Spot { scene = "HostPc", name = "ConfirmButton", w = 300, h = 100, x = 0, y = 75 },
        // PC 로비: 왼쪽 아래 두 버튼
        new Spot { scene = "PC_LobbyScene", name = "SuspectButton", w = 390, h = 130, x = 250, y = 245 },
        new Spot { scene = "PC_LobbyScene", name = "GoToSuspectSceneButton", w = 390, h = 130, x = 250, y = 95 },
        // PC 용의자 선택: 카드 밑 선택, 오른쪽 아래 증거 목록, 팝업 왼쪽 아래 검거
        new Spot { scene = "SuspectScene", name = "SelectButton", w = 240, h = 80, x = 0, y = -160 },
        new Spot { scene = "SuspectScene", name = "EvidenceListButton", w = 390, h = 130, x = -260, y = 120 },
        new Spot { scene = "SuspectScene", name = "ArrestButton", w = 330, h = 110, x = -520, y = -280, sprite = "Assets/Images/검거 버튼.png" },
        // 모바일
        new Spot { scene = "ClientMobile", name = "ConnectButton", w = 450, h = 150, x = 0, y = -190 },
        new Spot { scene = "ClientMobile", name = "ExitButton", w = 450, h = 150, x = 0, y = -360 },
        new Spot { scene = "Mobile_LobbyScene", name = "EvidenceButton", w = 420, h = 140, x = -250, y = 110 },
        new Spot { scene = "Mobile_LobbyScene", name = "ConfirmButton", w = 320, h = 90, x = 0, y = -330 },   // 안내 팝업 확인
        new Spot { scene = "WitnessScene", name = "ReportButton", w = 330, h = 110, x = -195, y = -85 },
    };

    private enum Kind { Skip, Baked, Pill, Round, Close, Restore, Item, Card, Feedback }

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
        bool invisible = img == null || img.color.a < 0.05f;
        if (n.Contains("Exclamation") || label == "!" || label == "'!'")
        {
            // 원래 투명 배경 + '!' 글자만 있는 버튼 → 그대로 둠. 예전 판으로 노란 그림이 씌워졌으면 되돌림
            p.kind = (img != null && img.sprite != null && img.sprite.name.StartsWith("btn_exclaim")) ? Kind.Restore : Kind.Skip;
            return p;
        }
        if (invisible && label.Trim().Length == 0) { p.kind = Kind.Skip; return p; }   // 글자 없는 투명 버튼(그림 위 터치 영역)은 그대로
        // 투명 배경 + 글자만 있던 버튼(PC 의 + − ◀ ▶ X)은 아래 규칙대로 동그라미·닫기 버튼으로
        if (n.Contains("ItemTemplate") || n.Contains("Keyword") || n.Contains("EvidenceBtn")) { p.kind = Kind.Item; return p; }
        if (label == "X" || label == "x" || (n.Contains("Close") && Mathf.Max(w, h) <= 90f)) { p.kind = Kind.Close; return p; }
        string g = label.Replace("\n", "").Trim();
        if (g == "+" || g == "-" || g == "−" || g == "▶" || g == "◀" || (h >= 40f && w / Mathf.Max(1f, h) <= 1.3f))
        { p.kind = Kind.Round; p.theme = "yellow"; return p; }

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
            case Kind.Round: return "동그라미 " + p.theme;
            case Kind.Close: return "닫기 X";
            case Kind.Restore: return "원래 모습으로 되돌림 (투명 배경 + 글자)";
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
            if (p.kind != Kind.Skip && p.kind != Kind.Restore) FoldScale((RectTransform)p.button.transform);
            switch (p.kind)
            {
                case Kind.Baked: ApplyBaked(p.button, scene.name); break;
                case Kind.Pill: ApplyPill(p.button, p.theme, mobile); break;
                case Kind.Round: ApplyRoundLabel(p.button, p.theme); break;
                case Kind.Close: ApplyRound(p.button, "btn_close"); break;
                case Kind.Restore: RestoreOriginal(p.button); break;
                case Kind.Item: ApplyItem(p.button); break;
                case Kind.Card: ApplyCard(p.button); break;
                case Kind.Feedback: Feedback(p.button, 0f, false); break;
                default: continue;
            }
            EditorUtility.SetDirty(p.button.gameObject);
            count++;
        }
        if (scene.name == "ForensicScene") FixGauge();
        ApplyLayout(scene);
        if (scene.name == "HostPc") FixHostDim(scene);
        return count;
    }

    // 버튼에 Scale(예: 3배, 1.5×2배)이 걸려 있으면 크기 계산이 어긋나고 찌그러짐 → 크기에 합치고 Scale 은 1 로
    private static void FoldScale(RectTransform rt)
    {
        Vector3 s = rt.localScale;
        if (Mathf.Abs(s.x - 1f) < 0.001f && Mathf.Abs(s.y - 1f) < 0.001f) return;
        if (s.x <= 0f || s.y <= 0f || rt.anchorMin != rt.anchorMax) return;
        log?.AppendLine($"  {rt.name}: Scale {s.x:0.##}×{s.y:0.##} → 1 (크기 {rt.sizeDelta.x:0}×{rt.sizeDelta.y:0} → {rt.sizeDelta.x * s.x:0}×{rt.sizeDelta.y * s.y:0})");
        rt.sizeDelta = new Vector2(rt.sizeDelta.x * s.x, rt.sizeDelta.y * s.y);
        foreach (Transform ct in rt)
        {
            var c = ct as RectTransform;
            if (c == null) continue;
            if (c.anchorMin == c.anchorMax) { c.sizeDelta = Vector2.Scale(c.sizeDelta, s); c.anchoredPosition = Vector2.Scale(c.anchoredPosition, s); }
            else { c.offsetMin = Vector2.Scale(c.offsetMin, s); c.offsetMax = Vector2.Scale(c.offsetMax, s); }
        }
        float k = (s.x + s.y) * 0.5f;
        foreach (var t in rt.GetComponentsInChildren<TMP_Text>(true))
        { t.fontSize *= k; t.fontSizeMin *= k; t.fontSizeMax *= k; }
        rt.localScale = Vector3.one;
    }

    // 표(Layout)에 적힌 화면별 주요 버튼의 크기·위치를 맞춤
    private static void ApplyLayout(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var b in root.GetComponentsInChildren<Button>(true))
                foreach (var sp in Layout)
                {
                    if (sp.scene != scene.name || sp.name != b.name) continue;
                    var rt = (RectTransform)b.transform;
                    if (rt.anchorMin != rt.anchorMax) continue;
                    Undo.RecordObject(rt, "버튼 스킨");
                    FoldScale(rt);
                    if (sp.sprite != null)
                    {
                        var spr = AssetDatabase.LoadAssetAtPath<Sprite>(sp.sprite);
                        var img = b.targetGraphic as Image;
                        if (spr != null && img != null)
                        {
                            Undo.RecordObject(img, "버튼 스킨");
                            img.sprite = spr; img.type = Image.Type.Simple; img.preserveAspect = true; img.color = Color.white;
                            var pressed = AssetDatabase.LoadAssetAtPath<Sprite>(PressedDir + spr.name + "_눌림.png");
                            if (pressed != null) SpriteSwap(b, pressed);
                            foreach (var t in b.GetComponentsInChildren<TMP_Text>(true)) t.gameObject.SetActive(false);   // 글자는 그림에 있음
                            Feedback(b, 0f, false);
                        }
                    }
                    var size = new Vector2(sp.w, sp.h);
                    var pos = new Vector2(sp.x, sp.y);
                    if (rt.sizeDelta != size || rt.anchoredPosition != pos)
                        log?.AppendLine($"  {b.name}: 크기 {rt.sizeDelta.x:0}×{rt.sizeDelta.y:0} → {size.x:0}×{size.y:0}, 위치 ({rt.anchoredPosition.x:0},{rt.anchoredPosition.y:0}) → ({pos.x:0},{pos.y:0})");
                    rt.sizeDelta = size;
                    rt.anchoredPosition = pos;
                    EditorUtility.SetDirty(b.gameObject);
                }
    }

    // PC 시작 화면: 팝업용 어두운 막(DimBG)이 '게임 시작' 버튼 안에 들어 있어 버튼 크기를 따라 움직임
    // → 화면 전체를 덮도록 Canvas 로 옮기고, 팝업과 함께 맨 위(게임 종료 버튼보다 위)에 그려지게
    private static void FixHostDim(Scene scene)
    {
        Transform dim = null, popup = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "DimBG") dim = t;
                if (t.name == "PopupPanel") popup = t;
            }
        if (dim == null || popup == null || popup.parent == null) return;
        var canvas = popup.parent;
        if (dim.parent != canvas)
        {
            Undo.SetTransformParent(dim, canvas, "버튼 스킨");
            log?.AppendLine("  DimBG: 게임 시작 버튼 안 → 화면 전체 어두운 막");
        }
        var rt = (RectTransform)dim;
        Undo.RecordObject(rt, "버튼 스킨");
        rt.localScale = Vector3.one;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-200f, -200f); rt.offsetMax = new Vector2(200f, 200f);   // 넓은 화면에서도 가장자리까지
        rt.anchoredPosition = Vector2.zero;
        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "버튼 스킨");
        dim.SetAsLastSibling();
        popup.SetAsLastSibling();
        EditorUtility.SetDirty(dim.gameObject);
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
            // 아래 그늘을 뺀 윗면 가운데에 글자
            t.margin = new Vector4(rt.rect.height * 0.15f, 0, rt.rect.height * 0.15f, rt.rect.height * FaceLift);
        }
        Feedback(b, 0f, false);
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

    // 동그라미 버튼 (+ − ◀ ▶ 등): 그림은 비율 유지, 글자는 그대로 보이게
    private static void ApplyRoundLabel(Button b, string theme)
    {
        var img = b.targetGraphic as Image;
        if (img == null) return;
        var rt = (RectTransform)b.transform;
        // 용의자 팝업의 + 버튼 중 160×30 처럼 납작한 것 → 다른 + 버튼과 같은 65×65
        if (b.name.StartsWith("PlusBtn") && rt.anchorMin == rt.anchorMax && Mathf.Abs(rt.sizeDelta.x - rt.sizeDelta.y) > 1f)
        {
            log?.AppendLine($"  {b.name}: {rt.sizeDelta.x:0}×{rt.sizeDelta.y:0} → 65×65 (다른 + 버튼과 같게)");
            rt.sizeDelta = new Vector2(65f, 65f);
        }
        float d = Mathf.Min(rt.rect.width, rt.rect.height);
        img.sprite = Load("btn_round_" + theme);
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        img.color = Color.white;
        SpriteSwap(b, Load("btn_round_" + theme + "_pressed"));
        float lip = d * 0.08f;           // 아래 그늘만큼 글자를 위로
        foreach (var t in b.GetComponentsInChildren<TMP_Text>(true))
        {
            t.color = new Color32(34, 38, 58, 255);
            t.fontStyle |= FontStyles.Bold;
            t.enableAutoSizing = true;
            t.fontSizeMin = 10; t.fontSizeMax = Mathf.Max(12f, d * 0.5f);
            t.alignment = TextAlignmentOptions.Center;
            t.margin = new Vector4(0, 0, 0, lip);
        }
        Feedback(b, 0f, false);
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
            if (sceneName == "PC_LobbyScene") hh = 105f;     // 위아래 두 버튼(용의자·용의자 특정) 사이가 붙지 않게
            var size = new Vector2(Mathf.Round(hh * ratio), hh);
            if (rt.sizeDelta != size)
            {
                log?.AppendLine($"  {b.name}: {rt.sizeDelta.x:0}×{rt.sizeDelta.y:0} → {size.x:0}×{size.y:0} (그림 비율)");
                rt.sizeDelta = size;
                KeepInside(rt);
            }
            // 가운데 기준 버튼이 몇 픽셀 어긋나 있으면 가운데로 (예: 시작 화면 게임 종료 x = -15)
            if (rt.anchorMin.x == 0.5f && Mathf.Abs(rt.anchoredPosition.x) > 0f && Mathf.Abs(rt.anchoredPosition.x) <= 20f)
            {
                log?.AppendLine($"  {b.name}: 가로 위치 {rt.anchoredPosition.x:0} → 0 (가운데 맞춤)");
                rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);
            }
        }
        Feedback(b, 0f, false);
    }

    // 예전 판에서 노란 '!' 그림을 씌웠던 버튼 → 원래 모습(투명 배경 + '!' 글자, 색 변화 누름)
    private static void RestoreOriginal(Button b)
    {
        var img = b.targetGraphic as Image;
        if (img == null) return;
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.preserveAspect = false;
        img.color = new Color(1f, 1f, 1f, 0f);
        b.transition = Selectable.Transition.ColorTint;
        b.spriteState = new SpriteState();
        foreach (var t in b.GetComponentsInChildren<TMP_Text>(true)) t.gameObject.SetActive(true);
        var f = b.GetComponent<ButtonPressFeedback>();
        if (f != null) Undo.DestroyObjectImmediate(f);
        log?.AppendLine($"  {b.name}: 원래 모습으로 되돌림");
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
