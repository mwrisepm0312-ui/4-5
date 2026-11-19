// PeaceMaker — 휴대폰에서 글자·팝업·버튼이 작아 보이는 문제 (모바일 전용)
// 위치: Assets/Editor/PeaceMakerMobileReadable.cs
// 메뉴: PeaceMaker → 휴대폰 크기 키우기 → 1. 미리 보기 (바꾸지 않음) / 2. 빌드 목록의 모든 씬에 적용
//
// 왜 작아 보이나: 화면이 PC 모니터용 1920×1080 기준이라, 6인치대 휴대폰에서는 글자 30px 가 약 2mm 높이밖에 안 됨.
// 배경·지문·모니터처럼 그림 위 위치가 정해진 것은 그대로 두고, 읽고 누르는 것만 키웁니다.
//   1) 팝업(…Popup) : 화면(높이 1000, 너비 1800) 안에 들어오는 한도에서 최대 1.35배 (Scale)
//   2) 팝업 밖 글자 : 1.3배 (최소 40), 글자 칸도 같은 비율로 넓힘 / 칸이 늘어나는 글자는 자동 크기로
//   3) 팝업 밖 알약 버튼(btn_*) : 1.2배, 화면 가장자리 밖으로 나가지 않게
//   4) 목격자 대화창 : 아래 대화 상자 250 → 330, 대화 화면이 129 위로 밀린 것 바로잡기
// 2)·3) 은 씬마다 한 번만 적용됩니다 (씬에 '__PM_Readable' 표시를 남김, 빌드에는 포함 안 됨).
// 1)·4) 는 몇 번 실행해도 결과가 같습니다.
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PeaceMakerMobileReadable
{
    private const string Marker = "__PM_Readable";
    private const float PopupMax = 1.35f, PopupMaxH = 1000f, PopupMaxW = 1800f;
    private const float TextScale = 1.3f, TextMin = 40f, TextMaxScale = 1.6f;
    private const float ButtonScale = 1.2f, Edge = 20f;

    private static StringBuilder log;
    private static bool dry;
    private static int changes;

    [MenuItem("PeaceMaker/휴대폰 크기 키우기/1. 미리 보기 (바꾸지 않음)")]
    private static void Preview() { Run(true); }

    [MenuItem("PeaceMaker/휴대폰 크기 키우기/2. 빌드 목록의 모든 씬에 적용")]
    private static void ApplyAll()
    {
        if (!EditorUtility.DisplayDialog("휴대폰 크기 키우기",
            "빌드 목록의 모든 씬에서 팝업·글자·버튼을 키우고 저장합니다.\n먼저 커밋(백업)했는지 확인하세요.", "적용", "취소")) return;
        Run(false);
    }

    private static void Run(bool preview)
    {
        dry = preview; changes = 0;
        log = new StringBuilder(preview ? "[PeaceMaker] 휴대폰 크기 키우기 — 미리 보기 (아직 안 바뀜)\n" : "[PeaceMaker] 휴대폰 크기 키우기 — 적용\n");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string start = SceneManager.GetActiveScene().path;
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (!s.enabled || !System.IO.File.Exists(s.path)) continue;
            var scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
            int before = changes;
            ApplyScene(scene);
            if (!dry && changes > before) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        }
        if (!string.IsNullOrEmpty(start)) EditorSceneManager.OpenScene(start);
        log.AppendLine(dry ? $"→ 바뀔 곳 {changes}개. 괜찮으면 '2. 적용' 을 실행하세요." : $"→ {changes}곳 바꾸고 씬 저장 완료");
        Debug.Log(log.ToString());
    }

    private static void ApplyScene(Scene scene)
    {
        log.AppendLine($"■ {scene.name}");
        bool already = false;
        foreach (var r in scene.GetRootGameObjects()) if (r.name == Marker) already = true;

        var popups = new HashSet<Transform>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
                if (IsPopup(rt) && !UnderAny(rt, popups)) popups.Add(rt);

        // 1) 팝업
        foreach (RectTransform p in popups) ScalePopup(p);

        // 4) 목격자 대화 화면
        if (scene.name == "WitnessScene") FixDialogue(scene);

        if (already) { log.AppendLine("  (글자·버튼은 이미 키웠음 — 건너뜀)"); return; }

        // 2) 팝업 밖 글자
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (UnderAny(t.transform, popups) || t.GetComponentInParent<Selectable>(true) != null) continue;
                GrowText(t);
            }

        // 3) 팝업 밖 알약 버튼
        foreach (var root in scene.GetRootGameObjects())
            foreach (var b in root.GetComponentsInChildren<Button>(true))
            {
                if (UnderAny(b.transform, popups)) continue;
                var img = b.targetGraphic as Image;
                if (img == null || img.sprite == null || !img.sprite.name.StartsWith("btn_") || img.sprite.name.StartsWith("btn_round") || img.sprite.name.StartsWith("btn_close")) continue;
                GrowButton((RectTransform)b.transform);
            }

        if (!dry && changes > 0)
        {
            var mark = new GameObject(Marker) { tag = "EditorOnly" };
            mark.SetActive(false);
            SceneManager.MoveGameObjectToScene(mark, scene);
        }
    }

    private static bool IsPopup(RectTransform rt)
    {
        if (!rt.name.EndsWith("Popup") || rt.anchorMin != rt.anchorMax) return false;
        var img = rt.GetComponent<Image>();
        return img != null && rt.sizeDelta.x > 100f && rt.sizeDelta.y > 100f;
    }

    private static bool UnderAny(Transform t, HashSet<Transform> set)
    {
        for (var p = t.parent; p != null; p = p.parent) if (set.Contains(p)) return true;
        return false;
    }

    private static void ScalePopup(RectTransform p)
    {
        float cur = p.localScale.x;
        float fit = Mathf.Min(PopupMaxH / p.sizeDelta.y, PopupMaxW / p.sizeDelta.x);
        // 위에 탭(PopupTab)이 튀어나와 있으면 탭까지 화면 안에 (화면 위 끝 540 - 여유 10)
        var tab = p.Find("PopupTab") as RectTransform;
        if (tab != null && tab.gameObject.activeSelf)
        {
            float above = tab.anchoredPosition.y + tab.sizeDelta.y;           // 팝업 위 가장자리 위로 튀어나온 높이
            fit = Mathf.Min(fit, (540f - 10f - Mathf.Abs(p.anchoredPosition.y)) / (p.sizeDelta.y * 0.5f + Mathf.Max(0f, above)));
        }
        float target = Mathf.Min(PopupMax, fit);
        if (target <= cur + 0.01f) return;                       // 이미 충분히 크거나 화면을 넘으면 그대로
        log.AppendLine($"  팝업 {p.name}: {p.sizeDelta.x:0}×{p.sizeDelta.y:0} 배율 {cur:0.##} → {target:0.##}");
        changes++;
        if (dry) return;
        Undo.RecordObject(p, "휴대폰 크기");
        p.localScale = new Vector3(target, target, 1f);
    }

    private static void GrowText(TMP_Text t)
    {
        var rt = t.rectTransform;
        float size = t.enableAutoSizing ? t.fontSizeMax : t.fontSize;
        if (size <= 0f) return;
        float f = Mathf.Clamp(Mathf.Max(TextScale, TextMin / size), 1f, TextMaxScale);
        bool fixedBox = rt.anchorMin == rt.anchorMax && rt.parent != null && rt.parent.GetComponent<LayoutGroup>() == null;
        log.AppendLine($"  글자 {t.name}: {size:0} → {size * f:0}" + (fixedBox ? $" (칸 {rt.sizeDelta.x:0}×{rt.sizeDelta.y:0} → {rt.sizeDelta.x * f:0}×{rt.sizeDelta.y * f:0})" : " (자동 크기)"));
        changes++;
        if (dry) return;
        Undo.RecordObject(t, "휴대폰 크기");
        Undo.RecordObject(rt, "휴대폰 크기");
        if (fixedBox)
        {
            rt.sizeDelta *= f;
            KeepInside(rt);
            if (t.enableAutoSizing) { t.fontSizeMax *= f; t.fontSizeMin *= f; }
            else t.fontSize *= f;
        }
        else
        {
            // 칸 크기가 부모를 따라가는 글자: 넘치지 않게 자동 크기 (원래 크기 ~ 키운 크기)
            float min = t.enableAutoSizing ? t.fontSizeMin : size;
            t.enableAutoSizing = true;
            t.fontSizeMin = Mathf.Min(min, size);
            t.fontSizeMax = size * f;
        }
        EditorUtility.SetDirty(t);
    }

    private static void GrowButton(RectTransform rt)
    {
        if (rt.anchorMin != rt.anchorMax) return;
        log.AppendLine($"  버튼 {rt.name}: {rt.sizeDelta.x:0}×{rt.sizeDelta.y:0} → {rt.sizeDelta.x * ButtonScale:0}×{rt.sizeDelta.y * ButtonScale:0}");
        changes++;
        if (dry) return;
        Undo.RecordObject(rt, "휴대폰 크기");
        rt.sizeDelta *= ButtonScale;
        foreach (var t in rt.GetComponentsInChildren<TMP_Text>(true))
        {
            Undo.RecordObject(t, "휴대폰 크기");
            if (t.enableAutoSizing) t.fontSizeMax *= ButtonScale; else t.fontSize *= ButtonScale;
        }
        KeepInside(rt);
    }

    // 가장자리에 붙은 것이 커지면서 화면(또는 부모) 밖으로 나가지 않게
    private static void KeepInside(RectTransform rt)
    {
        if (rt.parent == null || rt.parent.GetComponent<Canvas>() == null) return;   // 화면 가장자리 기준인 것만 (상자 안 이름표 등은 그대로)
        Vector2 p = rt.anchoredPosition, half = rt.sizeDelta * 0.5f;
        if (rt.anchorMin.x == 0f) p.x = Mathf.Max(p.x, half.x + Edge);
        if (rt.anchorMin.x == 1f) p.x = Mathf.Min(p.x, -half.x - Edge);
        if (rt.anchorMin.y == 0f) p.y = Mathf.Max(p.y, half.y + Edge);
        if (rt.anchorMin.y == 1f) p.y = Mathf.Min(p.y, -half.y - Edge);
        rt.anchoredPosition = p;
    }

    // 목격자 대화: 화면 전체를 덮어야 하는 패널이 129 위로 밀려 있었음 + 아래 대화 상자 키우기
    private static void FixDialogue(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                if ((rt.name == "DialoguePanel" || rt.name == "FadePanel") && rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one
                    && (rt.offsetMin != Vector2.zero || rt.offsetMax != Vector2.zero))
                {
                    log.AppendLine($"  {rt.name}: 위치 어긋남 ({rt.anchoredPosition.y:0}) → 화면 전체");
                    changes++;
                    if (!dry) { Undo.RecordObject(rt, "휴대폰 크기"); rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
                }
                // 대화 인물: 대화 상자가 높아진 만큼 위로, 조금 크게 (아래 80 은 상자 뒤로 살짝 겹침)
                if (rt.name == "CharacterImage" && rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.zero)
                {
                    var size = new Vector2(720f, 480f);
                    var pos = new Vector2(400f, 330f - 80f + size.y * rt.pivot.y);
                    if (rt.sizeDelta != size || rt.anchoredPosition != pos)
                    {
                        log.AppendLine($"  CharacterImage: {rt.sizeDelta.x:0}×{rt.sizeDelta.y:0} → 720×480, 대화 상자 위로");
                        changes++;
                        if (!dry) { Undo.RecordObject(rt, "휴대폰 크기"); rt.sizeDelta = size; rt.anchoredPosition = pos; }
                    }
                }
                // 대화 상자: 기준점이 가운데라 위치 0 이면 아래 절반이 화면 밖 (그래서 패널을 129 올려 두었던 것)
                // → 높이 330, 바닥에 딱 붙게 (위치 = 높이 × 기준점)
                if (rt.name == "DialogueBox" && rt.anchorMin.y == 0f && rt.anchorMax.y == 0f)
                {
                    float h = Mathf.Max(330f, rt.sizeDelta.y);
                    float y = h * rt.pivot.y;
                    if (Mathf.Abs(rt.sizeDelta.y - h) > 0.5f || Mathf.Abs(rt.anchoredPosition.y - y) > 0.5f)
                    {
                        log.AppendLine($"  DialogueBox: 높이 {rt.sizeDelta.y:0} → {h:0}, 화면 바닥에 붙임 (위치 {rt.anchoredPosition.y:0} → {y:0})");
                        changes++;
                        if (!dry)
                        {
                            Undo.RecordObject(rt, "휴대폰 크기");
                            rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
                            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
                        }
                    }
                }
            }
    }
}
