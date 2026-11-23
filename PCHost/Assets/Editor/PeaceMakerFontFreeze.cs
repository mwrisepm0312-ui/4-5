// PeaceMaker — 폰트 고정 (PC·모바일 공통 파일)
// 위치: Assets/Editor/PeaceMakerFontFreeze.cs
// 메뉴: PeaceMaker → 폰트 고정 (게임 글자 미리 채우기)
//
// 왜 필요한가
//  Maplestory Bold SDF 가 '동적(Dynamic)' 폰트라서, 처음 보는 글자가 화면에 나올 때마다
//  폰트 파일 안에 글자 그림을 새로 그려 넣고 저장함 → 실행할 때마다 파일이 바뀌어 Git에 계속 올라옴.
//
// 하는 일
//  1) 이 프로젝트의 모든 씬 글자 + 모든 스크립트 문자열 + 영문·숫자·기호에서 쓰는 글자를 모음
//  2) 폰트를 비우고 그 글자들만 미리 그려 넣음
//  3) 폰트를 '고정(Static)'으로 바꿈 → 이후에는 실행해도 폰트 파일이 바뀌지 않음
//
// 주의
//  - 고정 후 새로 쓰는 글자(문구 추가·변경)는 □ 로 보일 수 있음 → 문구를 바꾼 뒤 이 메뉴를 한 번 더 실행
//  - 실행 후 폰트 파일을 커밋해야 팀원 PC에도 적용됨
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class PeaceMakerFontFreeze
{
    private const string FontPath = "Assets/TextMesh Pro/Fonts/Maplestory Bold SDF.asset";

    [MenuItem("PeaceMaker/폰트 고정 (게임 글자 미리 채우기)")]
    public static void Freeze()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            EditorUtility.DisplayDialog("폰트 고정", $"폰트를 찾지 못했어요.\n{FontPath}", "확인");
            return;
        }

        string characters = CollectCharacters();

        if (!EditorUtility.DisplayDialog("폰트 고정",
                $"게임에서 쓰는 글자 {characters.Length}개를 폰트에 미리 넣고 고정할까요?\n\n" +
                "고정 후 새 문구를 넣으면 이 메뉴를 다시 실행해 주세요.", "고정하기", "취소"))
            return;

        var so = new SerializedObject(font);

        // 1) 글자를 넣을 수 있게 잠시 '동적'으로 (필요하면 아틀라스 여러 장 허용)
        so.FindProperty("m_AtlasPopulationMode").intValue = 1;   // 1 = Dynamic
        var multi = so.FindProperty("m_IsMultiAtlasTexturesEnabled");
        if (multi != null) multi.boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 2) 비우고 필요한 글자만 다시 그림
        font.ClearFontAssetData(false);
        font.TryAddCharacters(characters, out string missing);

        // 3) 고정
        so.Update();
        so.FindProperty("m_AtlasPopulationMode").intValue = 0;   // 0 = Static
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();

        // 폰트에 없는 글자(이모지 등)는 보고만 함 (게임 화면에는 쓰지 않는 디버그 로그용이 대부분)
        string missingShown = string.IsNullOrEmpty(missing) ? "없음" : new string(missing.Distinct().ToArray());
        Debug.Log($"[폰트 고정] 글자 {characters.Length}개 고정 완료. 폰트에 없는 글자: {missingShown}");
        EditorUtility.DisplayDialog("폰트 고정",
            $"완료했어요.\n글자 {characters.Length}개를 넣고 고정했어요.\n\n폰트에 없는 글자: {missingShown}\n\n" +
            "이제 'Maplestory Bold SDF' 파일을 커밋해 주세요.", "확인");
    }

    // 씬 글자 + 스크립트 문자열 + 기본 영문·숫자·기호
    private static string CollectCharacters()
    {
        var set = new HashSet<char>();

        for (char c = (char)32; c <= (char)126; c++) set.Add(c);   // 영문·숫자·기호
        foreach (char c in "·…→←↑↓◀▶×—–“”‘’「」『』%℃") set.Add(c);

        // 씬에 적힌 글자 (\uXXXX 로 저장된 한글 포함)
        var unicodeEscape = new Regex(@"\\u([0-9A-Fa-f]{4})");
        foreach (string path in Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories))
        {
            foreach (string line in File.ReadLines(path))
            {
                if (!line.Contains("m_text:") && !line.StartsWith("    ") && !line.StartsWith("  - ")) continue;
                string decoded = unicodeEscape.Replace(line, m => ((char)System.Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
                AddText(set, decoded);
            }
        }

        // 스크립트 안의 문자열 ("..." 사이)
        var literal = new Regex("\"((?:[^\"\\\\\\n]|\\\\.)*)\"");
        foreach (string dir in new[] { "Assets/Scripts", "Assets/Editor" })
        {
            if (!Directory.Exists(dir)) continue;
            foreach (string path in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                string code = File.ReadAllText(path, Encoding.UTF8);
                foreach (Match m in literal.Matches(code)) AddText(set, m.Groups[1].Value);
            }
        }

        var sb = new StringBuilder();
        foreach (char c in set.OrderBy(c => c)) sb.Append(c);
        return sb.ToString();
    }

    private static void AddText(HashSet<char> set, string text)
    {
        foreach (char c in text)
        {
            if (char.IsControl(c) || char.IsSurrogate(c)) continue;
            // 한글 음절, 한글 자모, 영문·숫자·기호, 자주 쓰는 문장 부호만
            if ((c >= '가' && c <= '힣') || (c >= 'ㄱ' && c <= 'ㆎ') || (c >= ' ' && c <= '~')
                || c == '·' || c == '…' || c == '→' || c == '◀' || c == '▶' || c == '×' || c == '—')
                set.Add(c);
        }
    }
}
