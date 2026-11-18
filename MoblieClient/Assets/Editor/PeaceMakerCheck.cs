// PeaceMaker — 빠진 연결 찾기 (아무것도 바꾸지 않고 검사만 함)
// 메뉴: PeaceMaker → 점검 → 빠진 연결 찾기 (빌드 목록의 모든 씬)
//
// 검사 항목
//  1) 없어진 스크립트 (Missing Script)
//  2) 끊어진 그림 연결 (Image·RawImage·Button 눌림 그림·SpriteRenderer 가 지워진 파일을 가리킴)
//     — 씬 파일 글자에서 그림 GUID 를 직접 찾아, 프로젝트에 없는 파일이면 알려 줌
//  3) 찌그러진 그림 (Preserve Aspect 꺼짐 + 칸 비율과 그림 비율이 8% 넘게 다름)
//  4) 코드가 Resources.Load 로 부르는 그림 경로가 실제로 있는지
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PeaceMakerCheck
{
    [MenuItem("PeaceMaker/점검/빠진 연결 찾기 (빌드 목록의 모든 씬)")]
    static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string start = SceneManager.GetActiveScene().path;
        var log = new StringBuilder();
        int missingScript = 0, brokenRef = 0, stretched = 0, badPath = 0;

        foreach (var s in EditorBuildSettings.scenes)
        {
            if (!s.enabled || !File.Exists(s.path)) continue;
            var scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
            var lines = new List<string>();
            foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var go = t.gameObject;
                int ms = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (ms > 0) { missingScript += ms; lines.Add($"  ✖ 없어진 스크립트 {ms}개 : {PathOf(t)}"); }

                foreach (var c in go.GetComponents<Component>())
                {
                    if (c == null) continue;
                    if (!(c is Image || c is RawImage || c is Selectable || c is SpriteRenderer)) continue;
                    var so = new SerializedObject(c);
                    var it = so.GetIterator();
                    while (it.NextVisible(true))
                    {
                        if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                        if (it.objectReferenceValue == null && it.objectReferenceInstanceIDValue != 0)
                        { brokenRef++; lines.Add($"  ✖ 끊어진 그림 연결 ({it.displayName}) : {PathOf(t)}"); }
                    }
                }

                var img = go.GetComponent<Image>();
                var rt = t as RectTransform;
                if (img != null && img.sprite != null && rt != null && img.type == Image.Type.Simple && !img.preserveAspect
                    && rt.anchorMin == rt.anchorMax && go.GetComponent<AspectRatioFitter>() == null
                    && rt.sizeDelta.x > 1 && rt.sizeDelta.y > 1)
                {
                    float sr = img.sprite.rect.width / img.sprite.rect.height;
                    float rr = rt.sizeDelta.x / rt.sizeDelta.y;
                    float diff = Mathf.Abs(sr / rr - 1f);
                    if (diff > 0.08f)
                    {
                        stretched++;
                        lines.Add($"  △ 찌그러짐 {diff * 100:0}% : {PathOf(t)}  (칸 {rt.sizeDelta.x:0}×{rt.sizeDelta.y:0}, 그림 {img.sprite.rect.width:0}×{img.sprite.rect.height:0} '{img.sprite.name}')");
                    }
                }
            }
            // 씬 파일에 적힌 그림 GUID 중 프로젝트에 없는 것 (지워진 파일)
            string yaml = File.ReadAllText(s.path);
            var blocks = Regex.Split(yaml, "(?=^--- !u!)", RegexOptions.Multiline);
            var goNames = new Dictionary<string, string>();
            foreach (var b in blocks)
            {
                var goHead = Regex.Match(b, "^--- !u!1 &(-?\\d+)");
                if (!goHead.Success) continue;
                var nm = Regex.Match(b, "m_Name: (.*)");
                goNames[goHead.Groups[1].Value] = nm.Success ? nm.Groups[1].Value.Trim() : "?";
            }
            var guidRx = new Regex("\\{fileID: (?:21300000|2800000), guid: ([0-9a-f]{32}), type: 3\\}");
            foreach (var b in blocks)
                foreach (Match m in guidRx.Matches(b))
                {
                    if (!string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value))) continue;
                    var owner = Regex.Match(b, "m_GameObject: \\{fileID: (-?\\d+)\\}");
                    string who = owner.Success && goNames.ContainsKey(owner.Groups[1].Value) ? goNames[owner.Groups[1].Value] : "?";
                    brokenRef++;
                    lines.Add($"  ✖ 지워진 그림을 가리킴 : {who}  (GUID {m.Groups[1].Value.Substring(0, 8)}…)");
                }

            log.AppendLine($"■ {scene.name}" + (lines.Count == 0 ? "  ✔ 문제 없음" : ""));
            foreach (var l in lines) log.AppendLine(l);
        }

        // 코드의 Resources.Load 경로 검사
        var resLines = new List<string>();
        var rx = new Regex("Resources\\.Load<\\w+>\\(\\s*\"([^\"{}]+)\"\\s*\\)");
        var lit = new Regex("\"((?:Jobs|CCTV|Cyber|Witnesses|Fingerprints|Suspects)/[^\"{}]+)\"");
        string codeDir = Directory.Exists("Assets/Scripts") ? "Assets/Scripts" : "Assets";
        foreach (var f in Directory.GetFiles(codeDir, "*.cs", SearchOption.AllDirectories))
        {
            if (f.Replace('\\', '/').Contains("/Editor/")) continue;
            string code = File.ReadAllText(f);
            var paths = new HashSet<string>();
            foreach (Match m in rx.Matches(code)) paths.Add(m.Groups[1].Value);
            foreach (Match m in lit.Matches(code)) paths.Add(m.Groups[1].Value);
            foreach (var p in paths)
                if (Resources.Load(p) == null) { badPath++; resLines.Add($"  ✖ {Path.GetFileName(f)} : \"{p}\" 를 Resources 에서 찾지 못함"); }
        }
        log.AppendLine("■ 코드의 Resources 경로" + (resLines.Count == 0 ? "  ✔ 문제 없음" : ""));
        foreach (var l in resLines) log.AppendLine(l);

        if (!string.IsNullOrEmpty(start)) EditorSceneManager.OpenScene(start);
        int total = missingScript + brokenRef + badPath;
        string head = total == 0
            ? $"[PeaceMaker] 점검 완료 ✔ 빠진 연결 0개 (찌그러짐 {stretched}개)"
            : $"[PeaceMaker] 점검 완료 — 없어진 스크립트 {missingScript} · 끊어진 그림 {brokenRef} · 잘못된 Resources 경로 {badPath} · 찌그러짐 {stretched}";
        if (total == 0) Debug.Log(head + "\n" + log); else Debug.LogWarning(head + "\n" + log);
    }

    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
}
