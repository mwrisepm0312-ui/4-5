// PeaceMaker — 화면 맞춤 · 찌그러짐 고치기 (PC·모바일 같은 파일)
// 메뉴: PeaceMaker → 화면 맞춤 → 1. 점검만 (바꾸지 않음) / 2. 빌드 목록의 모든 씬에 적용
//
// [모바일 씬]
//  - 모든 Canvas Scaler: 1920×1080, Expand → 1920×1080 영역은 휴대폰(20:9)·태블릿(16:10)에서도 잘리지 않음
//  - 배경: 시작·직업 선택·로비 = 비율 유지하며 꽉 채움 (Aspect Ratio Fitter: Envelope)
//          CCTV·현장감식·목격자·사이버 = 1920×1080 무대로 고정(모니터·지문·인물 위치 유지) + 가장자리 흐린 배경
//  - 직업 카드 400×225(16:9, 위치는 그대로), 3인용 카드 그림을 역할에 맞춤, 팝업 직업 그림 480×270
//  - CCTV 모니터 560×290 (CCTV 그림 1000×518 과 같은 비율)
//  - 화면보다 큰 팝업(목격자 진술서 900×1400 등)은 화면 안에 들어오게 축소
// [PC 씬]
//  - 오프닝 컷 6장 500×390(4:3) → 600×338(16:9), 위치는 그대로
// [공통]
//  - 그림 비율과 칸 비율이 8% 넘게 다른 그림에 Preserve Aspect 켜기 (찌그러짐 방지)
// 여러 번 실행해도 결과는 같습니다.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PeaceMakerScreenFit
{
    static readonly HashSet<string> MobileScenes = new HashSet<string>
        { "ClientMobile", "JobSelectScene", "Mobile_LobbyScene", "CCTVScene", "ForensicScene", "WitnessScene", "CyberScene" };
    static readonly HashSet<string> StageScenes = new HashSet<string> { "CCTVScene", "ForensicScene", "WitnessScene", "CyberScene" };
    const string FillFolder = "Assets/UI/ScreenFill/";
    static readonly Vector2 Ref = new Vector2(1920, 1080);
    const float SafeW = 1840f, SafeH = 900f;     // 팝업이 들어와야 하는 크기

    static StringBuilder log;
    static bool dry;
    static int changes;

    [MenuItem("PeaceMaker/화면 맞춤/1. 점검만 (바꾸지 않음)")]
    static void Check() { Run(true); }

    [MenuItem("PeaceMaker/화면 맞춤/2. 빌드 목록의 모든 씬에 적용")]
    static void Apply()
    {
        if (!EditorUtility.DisplayDialog("화면 맞춤",
            "빌드 목록의 씬을 열어 화면 설정을 바꾸고 저장합니다.\n작업 중인 씬이 있으면 먼저 저장하세요.", "적용", "취소")) return;
        Run(false);
    }

    static void Run(bool dryRun)
    {
        dry = dryRun; changes = 0;
        log = new StringBuilder(dry ? "[PeaceMaker] 화면 맞춤 점검 (아직 바꾸지 않음)\n" : "[PeaceMaker] 화면 맞춤 적용 결과\n");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string start = SceneManager.GetActiveScene().path;

        foreach (var s in EditorBuildSettings.scenes)
        {
            if (!s.enabled || !File.Exists(s.path)) continue;
            var scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
            int before = changes;
            log.AppendLine($"■ {scene.name}");
            FixScene(scene);
            if (changes == before) log.AppendLine("  (바꿀 것 없음)");
            else if (!dry) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        }
        if (!string.IsNullOrEmpty(start)) EditorSceneManager.OpenScene(start);
        log.AppendLine(dry ? $"→ 바꿀 곳 {changes}개. 메뉴 '2. 빌드 목록의 모든 씬에 적용' 으로 적용하세요."
                           : $"→ {changes}곳 변경, 씬 저장 완료. Game 창 2400×1080 · 1920×1200 에서 확인하세요.");
        Debug.Log(log.ToString());
    }

    static void FixScene(Scene scene)
    {
        bool mobile = MobileScenes.Contains(scene.name);
        foreach (var root in scene.GetRootGameObjects())
        foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
        {
            if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
            if (mobile)
            {
                FixScaler(canvas);
                foreach (Transform child in canvas.transform.Cast<Transform>().ToArray())
                    if (IsBackground(child))
                    {
                        if (StageScenes.Contains(scene.name)) MakeStage(child); else MakeCover(child);
                    }
                FitPopups(canvas);
            }
        }

        switch (scene.name)
        {
            case "JobSelectScene": FixJobSelect(); break;
            case "CCTVScene":
                for (int i = 1; i <= 4; i++) SetSize(Find("Monitor_" + i), new Vector2(560, 290), true);
                break;
            case "WitnessScene":
                var dp = Find("DialoguePanel");
                if (dp != null && ((RectTransform)dp.transform).anchoredPosition.y != 0)
                    log.AppendLine($"  ? 확인 필요: DialoguePanel 이 위로 {((RectTransform)dp.transform).anchoredPosition.y:0} 만큼 올라가 있음 (의도한 것이 아니면 Pos Y 를 0 으로)");
                break;
            case "OpeningScene":
                for (int i = 1; i <= 6; i++) SetSize(Find("Image_" + i), new Vector2(600, 338), true);
                break;
        }

        FixStretchedImages(scene);
    }

    // ---------- Canvas Scaler ----------
    static void FixScaler(Canvas canvas)
    {
        var sc = canvas.GetComponent<CanvasScaler>();
        if (sc == null) return;
        if (sc.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && sc.referenceResolution == Ref
            && sc.screenMatchMode == CanvasScaler.ScreenMatchMode.Expand) return;
        string was = sc.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
            ? $"{sc.referenceResolution.x:0}×{sc.referenceResolution.y:0} {sc.screenMatchMode} {sc.matchWidthOrHeight:0.##}" : sc.uiScaleMode.ToString();
        Note($"{canvas.name}: Canvas Scaler ({was}) → 1920×1080 Expand");
        if (dry) return;
        Undo.RecordObject(sc, "Screen Fit");
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = Ref;
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        sc.referencePixelsPerUnit = 100;
    }

    // ---------- 배경 ----------
    static bool IsBackground(Transform t)
    {
        string n = t.name.ToLower();
        if (!(n.Contains("background") || n.Contains("bg")) || n.Contains("dim") || n.EndsWith("_fill")) return false;
        var tex = TexOf(t);
        if (tex == null) return false;
        var rt = (RectTransform)t;
        bool stretch = rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one;
        bool stage = rt.anchorMin == rt.anchorMax && rt.sizeDelta == Ref;
        return stretch || stage || t.GetComponent<AspectRatioFitter>() != null;
    }

    static Texture TexOf(Transform t)
    {
        var img = t.GetComponent<Image>();
        if (img != null && img.sprite != null) return img.sprite.texture;
        var raw = t.GetComponent<RawImage>();
        return raw != null ? raw.texture : null;
    }

    static float RatioOf(Transform t)
    {
        var img = t.GetComponent<Image>();
        if (img != null && img.sprite != null) return img.sprite.rect.width / img.sprite.rect.height;
        var tex = TexOf(t);
        return (float)tex.width / tex.height;
    }

    static void MakeCover(Transform t)
    {
        float ratio = RatioOf(t);
        var fit = t.GetComponent<AspectRatioFitter>();
        if (fit != null && fit.aspectMode == AspectRatioFitter.AspectMode.EnvelopeParent && Mathf.Abs(fit.aspectRatio - ratio) < 0.01f) return;
        Note($"{t.name}: 배경을 비율 유지하며 화면 꽉 채우기 (Envelope)");
        if (dry) return;
        var rt = (RectTransform)t;
        Undo.RecordObject(rt, "Screen Fit");
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        var img = t.GetComponent<Image>();
        if (img != null) { Undo.RecordObject(img, "Screen Fit"); img.preserveAspect = false; }
        if (fit == null) fit = Undo.AddComponent<AspectRatioFitter>(t.gameObject);
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = ratio;
    }

    static void MakeStage(Transform t)
    {
        var rt = (RectTransform)t;
        var parent = t.parent;
        string fillName = t.name + "_Fill";
        var fillT = parent.Find(fillName);
        var fit = t.GetComponent<AspectRatioFitter>();
        bool fixedOk = rt.anchorMin == new Vector2(0.5f, 0.5f) && rt.anchorMax == rt.anchorMin
                       && rt.sizeDelta == Ref && rt.anchoredPosition == Vector2.zero && fit == null;
        if (fixedOk && fillT != null && fillT.GetSiblingIndex() < t.GetSiblingIndex()) return;

        Note($"{t.name}: 배경을 1920×1080 무대로 고정 + 가장자리 흐린 배경");
        if (dry) return;

        Undo.RecordObject(rt, "Screen Fit");
        if (fit != null) Undo.DestroyObjectImmediate(fit);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Ref; rt.anchoredPosition = Vector2.zero;
        var bgImg = t.GetComponent<Image>();
        if (bgImg != null) { Undo.RecordObject(bgImg, "Screen Fit"); bgImg.preserveAspect = false; }

        GameObject go;
        if (fillT == null)
        {
            go = new GameObject(fillName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
            Undo.RegisterCreatedObjectUndo(go, "Screen Fit");
            go.layer = t.gameObject.layer;
            go.transform.SetParent(parent, false);
        }
        else go = fillT.gameObject;
        go.transform.SetSiblingIndex(t.GetSiblingIndex());      // 배경 바로 뒤에 그림

        var fimg = go.GetComponent<Image>();
        string texName = TexOf(t).name;
        var fillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FillFolder + texName + "_fill.png");
        if (fillSprite != null) { fimg.sprite = fillSprite; fimg.color = Color.white; }
        else
        {
            fimg.sprite = bgImg != null ? bgImg.sprite : null;
            fimg.color = new Color(0.45f, 0.47f, 0.55f, 1f);
            log.AppendLine($"    (흐린 그림 {texName}_fill.png 이 없어 원래 배경을 어둡게 사용)");
        }
        fimg.raycastTarget = false;
        var ffit = go.GetComponent<AspectRatioFitter>();
        if (ffit == null) ffit = go.AddComponent<AspectRatioFitter>();
        ffit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        ffit.aspectRatio = fimg.sprite != null ? fimg.sprite.rect.width / fimg.sprite.rect.height : 16f / 9f;
    }

    // ---------- 큰 팝업 ----------
    static void FitPopups(Canvas canvas)
    {
        foreach (var rt in canvas.GetComponentsInChildren<RectTransform>(true))
        {
            if (!rt.name.Contains("Popup") || rt.anchorMin != rt.anchorMax) continue;
            float s = Mathf.Min(1f, SafeW / Mathf.Max(1, rt.sizeDelta.x), SafeH / Mathf.Max(1, rt.sizeDelta.y));
            if (s < 0.999f && Mathf.Abs(rt.localScale.x - s) > 0.01f)
            {
                Note($"{rt.name}: {rt.sizeDelta.x:0}×{rt.sizeDelta.y:0} 은 화면 높이보다 큼 → {s:0.00}배");
                if (!dry) { Undo.RecordObject(rt, "Screen Fit"); rt.localScale = new Vector3(s, s, 1); }
            }
        }
    }

    // ---------- 직업 선택 ----------
    static void FixJobSelect()
    {
        foreach (var grid in new[] { "JobGrid_2", "JobGrid_3", "JobGrid_4" })
        {
            var g = Find(grid);
            if (g == null) continue;
            foreach (Transform card in g.transform)
                if (card.name.StartsWith("JobCard"))
                {
                    SetSize(card.gameObject, new Vector2(400, 225), true);      // 위치는 그대로 (직접 조정)
                }
        }
        // 3인용: 수사관1 = 현장+디지털(2인 그림), 수사관2 = CCTV, 수사관3 = 목격자 (코드의 팝업 그림과 같게)
        var g3 = Find("JobGrid_3");
        if (g3 != null)
        {
            SetSprite(g3.transform.Find("JobCard_investigator01"), "Assets/Images/수사관 1 이미지.png");
            SetSprite(g3.transform.Find("JobCard_investigator02"), "Assets/Images/CCTV분석관.png");
            SetSprite(g3.transform.Find("JobCard_investigator03"), "Assets/Images/목격자 조사관.png");
        }
        var popupImg = Find("PopupJobImage");
        if (popupImg != null) SetSize(popupImg, new Vector2(480, 270), false);
    }

    // ---------- 찌그러진 그림 ----------
    static void FixStretchedImages(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        foreach (var img in root.GetComponentsInChildren<Image>(true))
        {
            if (img.sprite == null || img.preserveAspect || img.type != Image.Type.Simple) continue;
            if (img.name.StartsWith("JobCard") || img.name.StartsWith("Monitor_")
                || (scene.name == "OpeningScene" && img.name.StartsWith("Image_"))) continue;   // 위에서 크기와 함께 처리
            if (!AssetDatabase.GetAssetPath(img.sprite).StartsWith("Assets/")) continue;      // Unity 기본 그림 제외
            var rt = img.rectTransform;
            if (rt.anchorMin != rt.anchorMax || rt.sizeDelta.x <= 1 || rt.sizeDelta.y <= 1) continue;
            if (img.GetComponent<AspectRatioFitter>() != null) continue;
            float sr = img.sprite.rect.width / img.sprite.rect.height;
            float rr = rt.sizeDelta.x / rt.sizeDelta.y;
            float diff = Mathf.Abs(sr / rr - 1f);
            if (diff <= 0.08f) continue;
            Note($"{img.name}: 그림이 {diff * 100:0}% 찌그러짐 → Preserve Aspect 켜기 ('{img.sprite.name}')");
            if (!dry) { Undo.RecordObject(img, "Screen Fit"); img.preserveAspect = true; }
        }
    }

    // ---------- 도구 ----------
    static void SetSize(GameObject go, Vector2 size, bool preserve)
    {
        if (go == null) return;
        var rt = (RectTransform)go.transform;
        var img = go.GetComponent<Image>();
        bool sizeOk = rt.sizeDelta == size;
        bool paOk = !preserve || img == null || img.preserveAspect;
        if (sizeOk && paOk) return;
        Note($"{go.name}: {rt.sizeDelta.x:0}×{rt.sizeDelta.y:0} → {size.x:0}×{size.y:0}" + (preserve && img != null ? " + Preserve Aspect" : ""));
        if (dry) return;
        Undo.RecordObject(rt, "Screen Fit");
        rt.sizeDelta = size;
        if (preserve && img != null) { Undo.RecordObject(img, "Screen Fit"); img.preserveAspect = true; }
    }

    static void SetPos(GameObject go, Vector2 pos, string why)
    {
        if (go == null) return;
        var rt = (RectTransform)go.transform;
        if (rt.anchoredPosition == pos) return;
        Note($"{go.name}: 위치 ({rt.anchoredPosition.x:0}, {rt.anchoredPosition.y:0}) → ({pos.x:0}, {pos.y:0})  [{why}]");
        if (dry) return;
        Undo.RecordObject(rt, "Screen Fit");
        rt.anchoredPosition = pos;
    }

    static void SetSprite(Transform t, string path)
    {
        if (t == null) return;
        var img = t.GetComponent<Image>();
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (img == null) return;
        if (sp == null) { log.AppendLine($"  ⚠ 그림을 찾지 못함: {path}"); return; }
        if (img.sprite == sp) return;
        Note($"{t.parent.name}/{t.name}: 카드 그림 '{(img.sprite != null ? img.sprite.name : "없음")}' → '{sp.name}' (역할과 팝업 그림에 맞춤)");
        if (dry) return;
        Undo.RecordObject(img, "Screen Fit");
        img.sprite = sp;
    }

    // 꺼진 오브젝트(팝업 안)까지 이름으로 찾기
    static GameObject Find(string name)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
        return null;
    }

    static void Note(string msg) { changes++; log.AppendLine("  - " + msg); }
}
