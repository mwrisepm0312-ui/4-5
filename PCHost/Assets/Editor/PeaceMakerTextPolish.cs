// PeaceMaker — 화면 문구 다듬기 + 글자 위치·크기 정리 + 용의자 카드 배경판 (PC·모바일 공통 파일)
// 위치: Assets/Editor/PeaceMakerTextPolish.cs
// 메뉴: PeaceMaker → 문구·글자 정리 → 1. 미리 보기 (바꾸지 않음) / 2. 빌드 목록의 모든 씬에 적용
//
// 하는 일
//  1) 표(Edits)에 적힌 글자를 짧고 분명한 문구로 바꾸고, 필요한 곳은 글자 크기·칸 크기·위치를 맞춤
//  2) 모든 글자 끝에 붙은 빈 줄·공백 제거 (가운데 정렬이 위로 치우치던 원인), 글꼴에 없는 이모지 제거
//  3) PC 엔딩·대기 화면의 Canvas Scaler 를 1920×1080 기준으로 (다른 모니터에서도 같은 비율)
//  4) 용의자 선택 화면 카드 4장에 종이 카드 배경판 + 사진 배경
// 스크립트가 바꾸는 이름·증거 이름(정답 판정에 쓰는 값)은 건드리지 않습니다.
// 몇 번 실행해도 결과는 같습니다.
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PeaceMakerTextPolish
{
    // 씬, 경로 끝부분(같으면 모두), 바꿀 글자(null = 그대로), 글자 크기(0 = 그대로), 칸 크기(0 = 그대로), 위치(NaN = 그대로)
    private struct E
    {
        public string scene, path, text;
        public float size, w, h, x, y;
        public E(string scene, string path, string text, float size = 0, float w = 0, float h = 0, float x = float.NaN, float y = float.NaN)
        { this.scene = scene; this.path = path; this.text = text; this.size = size; this.w = w; this.h = h; this.x = x; this.y = y; }
    }

    private const string Lobby = "잠시 후 수사 본부로 돌아갑니다";

    private static readonly E[] Edits =
    {
        // ───────── PC ─────────
        new E("HostPc", "PopupPanel/PopupTitle", "참가 인원을 선택하세요"),
        new E("HostPc", "Canvas/StatusText", ""),                                   // 쓰지 않는 글자 (경찰 그림 위에 겹쳐 보였음)
        // 대기 화면 배경 간판이 시작 화면과 같은 가운데로 옮겨져서, 글자도 가운데(x 0)로
        new E("WaitingScene", "Canvas/StatusText", "휴대폰에서 [연결]을 눌러 주세요", 0, 0, 0, 0, 50),
        new E("WaitingScene", "Canvas/ConnectedText", "연결된 휴대폰  0 / 4", 0, 0, 0, 0, 250),
        new E("PC_LobbyScene", "Canvas/LobbyText", "수사 진행 중", 0, 800, 120),
        new E("PC_LobbyScene", "Canvas/ConnectedText", "연결된 휴대폰  0 / 4", 48, 700, 80, -370, 60),
        new E("PC_LobbyScene", "Canvas/TimerText", null, 0, 300, 100, -170, -70),
        new E("PC_LobbyScene", "SuspectImagePlaceholder/Text (TMP)", "사진 없음", 28),
        new E("PC_LobbyScene", "SuspectPopup/NameText", "이름:", 32),
        new E("PC_LobbyScene", "SuspectPopup/AgeText", "나이:", 32),
        new E("PC_LobbyScene", "SuspectPopup/JobText", "직업:", 32),
        new E("PC_LobbyScene", "SuspectPopup/FeatureText", "특징:", 28, 520, 170, 0, -510),
        new E("SuspectScene", "ReceivedEvidencePopup/TitleText", "받은 증거"),
        // 카드 이름: 누구인지 한눈에 (비서 = 반지 주인 주얼리씨의 비서)
        new E("SuspectScene", "SuspectCard_1/NameText", "보석 수집가", 32),
        new E("SuspectScene", "SuspectCard_2/NameText", "은행 경비원", 32),
        new E("SuspectScene", "SuspectCard_3/NameText", "주얼리씨의 비서", 32),
        new E("SuspectScene", "SuspectCard_4/NameText", "은행 청소부", 32),
        new E("SuspectScene", "SuspectPopup/NameText", null, 36),
        new E("SuspectScene", "SuspectPopup/JobText", null, 32),
        new E("SuspectScene", "EvidenceItemTemplate/Text (TMP)", "증거 이름", 30),
        new E("SuspectScene", "EvidenceBtn_Template/Text (TMP)", "증거 이름", 28),
        new E("SuspectScene", "EvidenceSlot_1/SlotText_1", "+ 를 눌러 증거 선택", 26),
        new E("SuspectScene", "EvidenceSlot_2/SlotText_2", "+ 를 눌러 증거 선택", 26),
        new E("SuspectScene", "EvidenceSlot_3/SlotText_3", "+ 를 눌러 증거 선택", 26),
        new E("SuspectScene", "EvidenceSlot_4/SlotText_4", "+ 를 눌러 증거 선택", 26),
        new E("SuspectScene", "EvidenceSlot_5/SlotText_5", "+ 를 눌러 증거 선택", 26),
        new E("OpeningScene", "Canvas/StoryText", "", 40, 1500, 140),
        new E("ArrestScene", "Canvas/SubtitleText", "", 52, 1600, 150, 0, 90),
        new E("FailScene", "Canvas/SubtitleText", "", 52, 1600, 150, 0, 90),

        // ───────── 모바일 ─────────
        new E("ClientMobile", "Canvas/StatusText", ""),
        new E("JobSelectScene", "Canvas/TitleText", "직업을 선택하세요", 0, 0, 0, 0, -250),   // 간판 가운데 (x -45 → 0)
        new E("JobSelectScene", "Canvas/StatusText", "", 0, 0, 0, 0, 125),
        new E("JobSelectScene", "JobPopup/PopupJobName", ""),
        new E("JobSelectScene", "JobPopup/PopupJobDesc", ""),
        new E("Mobile_LobbyScene", "Canvas/TitleText", "수사 본부", 0, 600, 150),
        new E("Mobile_LobbyScene", "Canvas/JobText", ""),
        new E("Mobile_LobbyScene", "EvidencePopup/EvidenceTitleText", "수집한 증거"),
        new E("Mobile_LobbyScene", "TutorialPopup/Text (TMP)", "NFC 태그하는 방법"),
        new E("Mobile_LobbyScene", "SendButton/Text (TMP)", "PC로 보내기"),
        new E("Mobile_LobbyScene", "EvidenceItemTemplate/Text (TMP)", "증거 이름"),

        new E("CCTVScene", "GuidePopup/GuideTitleText", "CCTV 분석 미션"),
        new E("CCTVScene", "GuidePopup/GuideDescText", "1. 아래 막대를 밀어 시간을 바꾸세요\n2. 수상한 사람이 보이면 그 화면을 누르세요\n3. 증거를 모아 사건을 해결하세요"),
        new E("CCTVScene", "TimeSlider/SliderHintText", "막대를 밀어 시간을 바꿔 보세요"),
        new E("CCTVScene", "EvidencePopup/EvidenceDescText", ""),
        new E("CCTVScene", "Canvas/ClearTitleText", "미션 완료!", 140),
        new E("CCTVScene", "Canvas/ClearDescText", Lobby, 60),

        new E("ForensicScene", "GuidePopup/GuideTitleText", "현장 감식 미션"),
        new E("ForensicScene", "GuidePopup/GuideDescText", "1. 느낌표(!)를 눌러 수상한 곳을 살펴보세요\n2. 가루를 뿌리고 문질러 지문을 채취하세요\n3. AFIS로 지문의 주인을 찾으세요"),
        new E("ForensicScene", "EvidencePopup/PopupDescText", "지문을 채취했어요!\nAFIS로 주인을 찾아 보세요."),
        new E("ForensicScene", "AFISPanel/AFISTitleText", "AFIS 지문 대조"),
        new E("ForensicScene", "AFISPanel/AFISDescText", "발견된 지문을 끌어서\n같은 용의자 지문 위에 놓으세요."),
        new E("ForensicScene", "Canvas/ClearTitleText", "미션 완료!", 140),
        new E("ForensicScene", "Canvas/ClearDescText", Lobby, 60),

        new E("WitnessScene", "GuidePopup/GuideText", "1. 목격자를 눌러 이야기를 들어 보세요\n2. 목격 내용과 말이 다른 용의자를 고르세요"),
        new E("WitnessScene", "StatementPopup/GuideLabel", "목격 내용과 말이 다른 용의자는?"),
        new E("WitnessScene", "StatementPopup/WitnessNameText", ""),
        new E("WitnessScene", "StatementPopup/WitnessStatementText", ""),
        new E("WitnessScene", "StatementPopup/ResultText", ""),
        new E("WitnessScene", "ReportPopup/ReportTitle", "수집한 진술"),
        new E("WitnessScene", "ReportPopup/ReportText", ""),
        new E("WitnessScene", "SuspectCard_1/SuspectStatement_1", ""),
        new E("WitnessScene", "SuspectCard_2/SuspectStatement_2", ""),
        new E("WitnessScene", "SuspectCard_3/SuspectStatement_3", ""),
        new E("WitnessScene", "SuspectCard_4/SuspectStatement_4", ""),
        new E("WitnessScene", "Canvas/ClearTitleText", "미션 완료!", 140),
        new E("WitnessScene", "Canvas/ClearDescText", "모든 진술을 모았어요!\n" + Lobby, 56),

        new E("CyberScene", "GuidePopup/GuideTitleText", "사이버 수사 미션"),
        new E("CyberScene", "GuidePopup/GuideDescText", "지워진 파일 3개를 복원하세요\n파일마다 복원 방법이 달라요\n복원한 파일에서 증거를 찾으세요"),
        new E("CyberScene", "ScanTutorial/GuideTitleText", "1. 이메일 복구"),
        new E("CyberScene", "ScanTutorial/GuideDescText", "수집가가 범행 직후 이메일을 지웠어요.\n\n화면을 드래그해 숨은 데이터 조각 5개를\n모두 찾아 보세요!"),
        new E("CyberScene", "SliderTutorial/GuideTitleText", "2. 휴대폰 복구"),
        new E("CyberScene", "SliderTutorial/GuideDescText", "경비원의 망가진 휴대폰을 찾았어요.\n\n오른쪽 막대 3개를 조절해\n휴대폰 화면을 되살려 보세요!"),
        new E("CyberScene", "KeywordTutorial/GuideTitleText", "3. 검색 기록 분석"),
        new E("CyberScene", "KeywordTutorial/GuideDescText", "비서의 인터넷 검색 기록을 확보했어요.\n\n사건과 관련된 검색어만 모두 고른 뒤\n[증거 수집]을 누르세요!"),
        new E("CyberScene", "ScanGame/ScanTitleText", "데이터 조각을 모두 찾으세요!"),
        new E("CyberScene", "SliderGame/SliderTitleText", "휴대폰 화면을 되살리세요!"),
        new E("CyberScene", "SliderGame/ResultText", ""),
        new E("CyberScene", "KeywordGame/KeywordTitleText", "사건과 관련된 검색어를 모두 고르세요"),
        new E("CyberScene", "KeywordGame/ResultText", ""),
        new E("CyberScene", "Button/Text (TMP)", "증거 수집"),
        new E("CyberScene", "StartButton/Text (TMP)", "시작하기"),
        new E("CyberScene", "EvidencePopup/EvidenceTitleText", "파일 복원 완료!"),
        new E("CyberScene", "Canvas/ClearTitleText", "미션 완료!", 140),
        new E("CyberScene", "Canvas/ClearDescText", Lobby, 60),
    };

    // PC 엔딩 자막 (음성이 없는 화면만. 오프닝은 녹음된 내레이션과 같아야 해서 그대로)
    private static readonly Dictionary<string, string[]> Story = new Dictionary<string, string[]>
    {
        { "ArrestScene", new[] {
            "범인 검거 성공!\n범인은 반지 주인 주얼리씨의 비서였습니다.",
            "비서는 주얼리씨 대신 금고를 여는 대리인 카드로\n침입 흔적 없이 VIP 금고를 열었습니다.",
            "낮에는 모르는 번호로 경비원의 순찰 시간을 알아내고,\n밤에는 수면제를 탄 커피로 경비원을 재웠습니다.",
            "새벽 0시 30분, 반지를 서류가방에 숨겨 빠져나가려 했지만\n금고 손잡이의 지문과 CCTV, 목격자를 피하지 못했습니다.",
            "수집가와 청소부는 범행 전에 은행을 떠났습니다.\n2주 전부터 반지를 팔 곳을 찾던 사람은 비서뿐이었습니다.",
            "시민의 안전을 지켜 낸 여러분은\n진정한 경찰입니다!",
            "태블릿을 반납해 주세요." } },
        { "FailScene", new[] { "범인을 찾지 못했습니다...", "수사는 미궁에 빠지고, 범인은 달아났습니다.", "금고를 열 수 있었던 사람은 누구였을까요?\n다시 조사해서 꼭 범인을 잡아 주세요!" } },
    };

    // Canvas Scaler 를 화면 크기에 맞추는 씬 (지금은 '픽셀 크기 고정'이라 1920×1080 이 아닌 모니터에서 크기가 달라짐)
    private static readonly string[] ScaleScenes = { "WaitingScene", "OpeningScene", "ArrestScene", "FailScene" };

    private static readonly string[] Emoji = { "📋 ", "🎉 ", "📋", "🎉" };
    private static readonly Color32 Navy = new Color32(34, 38, 58, 255);

    private static StringBuilder log;
    private static bool dry;
    private static int changes;

    [MenuItem("PeaceMaker/문구·글자 정리/1. 미리 보기 (바꾸지 않음)")]
    private static void Preview() { Run(true); }

    [MenuItem("PeaceMaker/문구·글자 정리/2. 빌드 목록의 모든 씬에 적용")]
    private static void ApplyAll()
    {
        if (!EditorUtility.DisplayDialog("문구·글자 정리", "빌드 목록의 모든 씬에서 문구·글자 크기·카드 배경을 바꾸고 저장합니다.\n먼저 커밋(백업)했는지 확인하세요.", "적용", "취소")) return;
        Run(false);
    }

    private static void Run(bool preview)
    {
        dry = preview; changes = 0;
        log = new StringBuilder(preview ? "[PeaceMaker] 문구·글자 정리 — 미리 보기 (아직 안 바뀜)\n" : "[PeaceMaker] 문구·글자 정리 — 적용\n");
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
        var texts = new List<TMP_Text>();
        foreach (var root in scene.GetRootGameObjects()) texts.AddRange(root.GetComponentsInChildren<TMP_Text>(true));

        // 1) 표에 적힌 글자
        foreach (var e in Edits)
        {
            if (e.scene != scene.name) continue;
            int hit = 0;
            foreach (var t in texts)
            {
                if (!PathOf(t.transform).EndsWith(e.path)) continue;
                hit++;
                Edit(t, e);
            }
            if (hit == 0) log.AppendLine($"  (못 찾음) {e.path}");
        }

        // 2) 끝의 빈 줄·공백, 이모지
        foreach (var t in texts)
        {
            string s = t.text ?? "";
            string n = s;
            foreach (var em in Emoji) n = n.Replace(em, "");
            n = n.TrimEnd(' ', '\n', '\r', '\t');
            if (n.StartsWith(" ")) n = n.TrimStart(' ');
            if (n == s) continue;
            log.AppendLine($"  {t.name}: 끝의 빈 줄·공백·이모지 정리");
            changes++;
            if (!dry) { Undo.RecordObject(t, "문구 정리"); t.text = n; EditorUtility.SetDirty(t); }
        }

        // 3) 엔딩 자막
        if (Story.TryGetValue(scene.name, out var lines)) SetStory(scene, lines);

        // 4) Canvas Scaler
        if (System.Array.IndexOf(ScaleScenes, scene.name) >= 0)
            foreach (var root in scene.GetRootGameObjects())
                foreach (var cs in root.GetComponentsInChildren<CanvasScaler>(true))
                {
                    if (cs.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && cs.referenceResolution == new Vector2(1920, 1080) && cs.matchWidthOrHeight == 0f) continue;
                    log.AppendLine($"  {cs.name}: Canvas Scaler → 화면 크기에 맞춤 (1920×1080)");
                    changes++;
                    if (dry) continue;
                    Undo.RecordObject(cs, "문구 정리");
                    cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    cs.referenceResolution = new Vector2(1920, 1080);
                    cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    cs.matchWidthOrHeight = 0f;                       // 다른 PC 화면과 같게 (가로 기준)
                }

        // 5) PC 대기 화면 배경: 시작 화면과 똑같이 가운데 1920×1080 (늘이기 → 고정)
        //    배경 그림도 시작 화면 무대와 같은 위치·크기로 맞춰 두었으므로, 넘어갈 때 그림이 움직이지 않음
        if (scene.name == "WaitingScene")
            foreach (var root in scene.GetRootGameObjects())
                foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rt.name != "WaitingBackground") continue;
                    var c = new Vector2(0.5f, 0.5f);
                    if (rt.anchorMin == c && rt.anchorMax == c && rt.sizeDelta == new Vector2(1920, 1080) && rt.anchoredPosition == Vector2.zero) continue;
                    log.AppendLine("  WaitingBackground: 화면 늘이기 → 시작 화면과 같은 가운데 1920×1080");
                    changes++;
                    if (dry) continue;
                    Undo.RecordObject(rt, "문구 정리");
                    rt.anchorMin = rt.anchorMax = c; rt.pivot = c;
                    rt.sizeDelta = new Vector2(1920, 1080); rt.anchoredPosition = Vector2.zero;
                }

        // 6) 용의자 카드 배경판
        if (scene.name == "SuspectScene") CardBacks(scene);

        // 7) PC 로비 사건 메모 (수사 내내 큰 화면에 보이는 핵심 질문)
        if (scene.name == "PC_LobbyScene") CaseMemo(scene);
    }

    private static void Edit(TMP_Text t, E e)
    {
        var rt = t.rectTransform;
        var parts = new List<string>();
        if (e.text != null && t.text != e.text) parts.Add($"'{Short(t.text)}' → '{Short(e.text)}'");
        float cur = t.enableAutoSizing ? t.fontSizeMax : t.fontSize;
        if (e.size > 0 && Mathf.Abs(cur - e.size) > 0.5f) parts.Add($"글자 {cur:0} → {e.size:0}");
        bool sizeChange = e.w > 0 && e.h > 0 && rt.anchorMin == rt.anchorMax && rt.sizeDelta != new Vector2(e.w, e.h);
        if (sizeChange) parts.Add($"칸 {rt.sizeDelta.x:0}×{rt.sizeDelta.y:0} → {e.w:0}×{e.h:0}");
        bool posChange = !float.IsNaN(e.x) && rt.anchoredPosition != new Vector2(e.x, e.y);
        if (posChange) parts.Add($"위치 → ({e.x:0},{e.y:0})");
        if (parts.Count == 0) return;
        log.AppendLine($"  {t.name}: " + string.Join(", ", parts));
        changes++;
        if (dry) return;
        Undo.RecordObject(t, "문구 정리");
        Undo.RecordObject(rt, "문구 정리");
        if (e.text != null) t.text = e.text;
        if (e.size > 0)
        {
            if (t.enableAutoSizing) { t.fontSizeMax = e.size; t.fontSizeMin = Mathf.Min(t.fontSizeMin, e.size * 0.7f); }
            else t.fontSize = e.size;
        }
        if (sizeChange) rt.sizeDelta = new Vector2(e.w, e.h);
        if (posChange) rt.anchoredPosition = new Vector2(e.x, e.y);
        EditorUtility.SetDirty(t);
    }

    private static void SetStory(Scene scene, string[] lines)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                var so = new SerializedObject(mb);
                var p = so.FindProperty("storyLines");
                if (p == null || !p.isArray) continue;
                bool same = p.arraySize == lines.Length;
                for (int i = 0; same && i < lines.Length; i++) same = p.GetArrayElementAtIndex(i).stringValue == lines[i];
                if (same) continue;
                log.AppendLine($"  {mb.name}: 자막 {lines.Length}줄 다듬음 ('{Short(lines[0])}' …)");
                changes++;
                if (dry) continue;
                p.arraySize = lines.Length;
                for (int i = 0; i < lines.Length; i++) p.GetArrayElementAtIndex(i).stringValue = lines[i];
                so.ApplyModifiedProperties();
            }
    }

    // 용의자 카드: 크림색 종이 카드(사진·이름·선택 버튼을 감쌈) + 사진 뒤 하늘색 배경, 이름은 남색
    private static void CardBacks(Scene scene)
    {
        var card = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/CardSkin/card_suspect.png");
        var photoBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/CardSkin/card_photo_bg.png");
        if (card == null || photoBg == null) { log.AppendLine("  (Assets/UI/CardSkin 그림을 찾지 못함)"); return; }
        foreach (var root in scene.GetRootGameObjects())
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (!rt.name.StartsWith("SuspectCard_")) continue;
                var photo = rt.Find("Photo") as RectTransform;
                if (photo == null) continue;
                bool made = Ensure(rt, "CardBG", card, Image.Type.Sliced, new Vector2(290, 440), new Vector2(0, -5), 0, 2f);
                made |= Ensure(rt, "PhotoBG", photoBg, Image.Type.Simple, photo.sizeDelta, photo.anchoredPosition, 1, 1f);
                var name = rt.Find("NameText") ? rt.Find("NameText").GetComponent<TMP_Text>() : null;
                if (name != null && name.color != (Color)Navy)
                {
                    made = true;
                    if (!dry) { Undo.RecordObject(name, "문구 정리"); name.color = Navy; name.fontStyle |= FontStyles.Bold; EditorUtility.SetDirty(name); }
                }
                if (made) { log.AppendLine($"  {rt.name}: 종이 카드 배경판 + 사진 배경, 이름 남색"); changes++; }
            }
    }

    private static bool Ensure(RectTransform parent, string name, Sprite sprite, Image.Type type, Vector2 size, Vector2 pos, int index, float ppu)
    {
        var t = parent.Find(name) as RectTransform;
        if (t != null)
        {
            var img0 = t.GetComponent<Image>();
            bool ok = img0 != null && img0.sprite == sprite && t.sizeDelta == size && t.anchoredPosition == pos && t.GetSiblingIndex() == index;
            if (ok) return false;
        }
        if (dry) return true;
        if (t == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "문구 정리");
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            t = (RectTransform)go.transform;
        }
        Undo.RecordObject(t, "문구 정리");
        t.anchorMin = t.anchorMax = new Vector2(0.5f, 0.5f);
        t.pivot = new Vector2(0.5f, 0.5f);
        t.sizeDelta = size;
        t.anchoredPosition = pos;
        t.localScale = Vector3.one;
        t.SetSiblingIndex(index);
        var img = t.GetComponent<Image>();
        Undo.RecordObject(img, "문구 정리");
        img.sprite = sprite; img.type = type; img.pixelsPerUnitMultiplier = ppu;
        img.color = Color.white; img.raycastTarget = false; img.preserveAspect = false;
        EditorUtility.SetDirty(t.gameObject);
        return true;
    }

    private const string MemoText =
        "<b>사건 메모</b>\n" +
        "· 사라진 것: 레드 다이아 반지\n" +
        "· 반지 주인: 주얼리씨 (VIP 고객)\n" +
        "· 사건 시각: 밤 11시 ~ 새벽 1시\n" +
        "· 금고: 부서진 곳 없음\n" +
        "<color=#FFD24A>→ 금고를 열 수 있었던 사람은?</color>";

    // PC 로비 왼쪽 위에 사건 메모판 (안쪽 판 + 글자)
    private static void CaseMemo(Scene scene)
    {
        var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/PopupSkin/panel_inset.png");
        RectTransform canvas = null; TMP_Text titleRef = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name == "LobbyText") { canvas = rt.parent as RectTransform; titleRef = rt.GetComponent<TMP_Text>(); }
            }
        if (canvas == null || panelSprite == null) { log.AppendLine("  (사건 메모: LobbyText 또는 panel_inset 을 찾지 못함)"); return; }

        var memo = canvas.Find("CaseMemo") as RectTransform;
        var memoText = memo != null && memo.Find("MemoText") ? memo.Find("MemoText").GetComponent<TMP_Text>() : null;
        bool ok = memo != null && memoText != null && memoText.text == MemoText && memo.sizeDelta == new Vector2(520, 340);
        if (ok) return;
        log.AppendLine("  CaseMemo: 왼쪽 위에 사건 메모판 (반지 주인·시각·부서진 곳 없음 → 금고를 열 수 있던 사람은?)");
        changes++;
        if (dry) return;

        if (memo == null)
        {
            var go = new GameObject("CaseMemo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "문구 정리");
            go.layer = canvas.gameObject.layer;
            go.transform.SetParent(canvas, false);
            memo = (RectTransform)go.transform;
        }
        Undo.RecordObject(memo, "문구 정리");
        memo.anchorMin = memo.anchorMax = new Vector2(0f, 1f);
        memo.pivot = new Vector2(0.5f, 0.5f);
        memo.sizeDelta = new Vector2(520, 340);
        memo.anchoredPosition = new Vector2(290, -280);   // 위 가운데 "수사 진행 중" 글자와 겹치지 않게
        memo.localScale = Vector3.one;
        var img = memo.GetComponent<Image>();
        img.sprite = panelSprite; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 2f;
        img.color = Color.white; img.raycastTarget = false;

        if (memoText == null)
        {
            var tgo = new GameObject("MemoText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(tgo, "문구 정리");
            tgo.layer = memo.gameObject.layer;
            tgo.transform.SetParent(memo, false);
            memoText = tgo.GetComponent<TextMeshProUGUI>();
        }
        var trt = memoText.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(34, 26); trt.offsetMax = new Vector2(-30, -26);
        if (titleRef != null && titleRef.font != null) memoText.font = titleRef.font;
        memoText.text = MemoText;
        memoText.richText = true;
        memoText.color = Color.white;
        memoText.alignment = TextAlignmentOptions.TopLeft;
        memoText.enableAutoSizing = true; memoText.fontSizeMin = 22; memoText.fontSizeMax = 32;
        memoText.lineSpacing = 8f;
        memoText.raycastTarget = false;
        EditorUtility.SetDirty(memo.gameObject);
    }

    private static string Short(string s)
    {
        s = (s ?? "").Replace("\n", " / ");
        return s.Length > 24 ? s.Substring(0, 24) + "…" : s;
    }

    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
}
