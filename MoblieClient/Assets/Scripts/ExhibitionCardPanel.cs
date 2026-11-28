using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// ─────────────────────────────────────────────
// 전시 모드 수사 카드 (NFC 대신 화면의 카드를 눌러 미니게임 입장)  E-8
//
// - 전시 모드일 때만 NFCManager.Start에서 만들어짐 (원래 게임에는 나타나지 않음)
// - 씬은 고치지 않고 코드로 로비 화면에 붙임
// - 내 직업이 쓸 수 있는 카드만 보여 줌 (NFC 규칙과 같음, 4인이면 1장)
// - 카드를 누르면 NFC로 찍은 것과 똑같이 NFCManager.HandleCardId 로 처리
// - 끝낸 미션 카드에는 '수사 완료' 도장 (다시 눌러 다시 할 수 있음)
// - 그림: Resources/ExhibitionCards/card_<forensic|cctv|witness|cyber>.png, stamp_done.png
//   (카드 이름·안내 글자는 그림에 들어 있어 폰트 고정과 무관)
//
// 위치: 로비 아래 가운데 파란 카펫 자리 (1920×1080 기준, 아래에서 45)
//       1장이면 500×316, 2장이면 420×265 + 간격 40 ('수집한 증거' 버튼과 겹치지 않음)
//       '수집한 증거' 버튼 바로 다음 순서라 증거 팝업·튜토리얼이 열리면 그 아래에 가려짐
// ─────────────────────────────────────────────
public class ExhibitionCardPanel : MonoBehaviour
{
    // 카드가 1장(4인)이면 크게, 2장(2·3인)이면 조금 작게 (그림 비율 640:404)
    private const float CardWidthOne = 500f;
    private const float CardWidthMany = 420f;
    private const float CardAspect = 404f / 640f;
    private const float CardGap = 40f;
    private const float BottomMargin = 45f;

    private NFCManager nfc;
    private readonly List<Tween> tweens = new List<Tween>();
    private readonly List<Button> buttons = new List<Button>();
    private bool entering = false;

    public static void Create(NFCManager nfc)
    {
        Canvas canvas = FindLobbyCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("[전시 모드] 로비 Canvas를 찾지 못해 카드를 만들지 못했어요.");
            return;
        }

        var go = new GameObject("ExhibitionCards", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);

        // '수집한 증거' 버튼 바로 다음 순서 (팝업들보다 앞 → 팝업이 열리면 카드를 덮음)
        Transform evidenceButton = canvas.transform.Find("EvidenceButton");
        Transform evidencePopup = canvas.transform.Find("EvidencePopup");
        if (evidenceButton != null)
            rt.SetSiblingIndex(evidenceButton.GetSiblingIndex() + 1);
        else if (evidencePopup != null)
            rt.SetSiblingIndex(evidencePopup.GetSiblingIndex());

        var panel = go.AddComponent<ExhibitionCardPanel>();
        panel.nfc = nfc;
        panel.Build(rt);
    }

    static Canvas FindLobbyCanvas()
    {
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.isRootCanvas && c.GetComponent<CanvasScaler>() != null && c.name == "Canvas")
                return c;
        }
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.isRootCanvas && c.GetComponent<CanvasScaler>() != null)
                return c;
        }
        return null;
    }

    void Build(RectTransform rt)
    {
        List<NFCManager.CardInfo> cards = nfc.GetMyCards();
        int n = cards.Count;
        if (n == 0)
        {
            Debug.LogWarning("[전시 모드] 내 직업이 쓸 수 있는 카드가 없어요. (직업 선택 전?)");
            return;
        }

        float CardWidth = n == 1 ? CardWidthOne : CardWidthMany;
        float CardHeight = Mathf.Round(CardWidth * CardAspect);
        float totalWidth = n * CardWidth + (n - 1) * CardGap;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, BottomMargin);
        rt.sizeDelta = new Vector2(totalWidth, CardHeight);

        Sprite stamp = Resources.Load<Sprite>("ExhibitionCards/stamp_done");

        for (int i = 0; i < n; i++)
        {
            var card = cards[i];
            bool cleared = DataManager.Instance != null && DataManager.Instance.ClearedMissions.Contains(card.Mission);

            var cardGo = new GameObject("Card_" + card.Key, typeof(RectTransform), typeof(Image), typeof(Button));
            var crt = (RectTransform)cardGo.transform;
            crt.SetParent(rt, false);
            crt.anchorMin = crt.anchorMax = new Vector2(0f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(CardWidth, CardHeight);
            crt.anchoredPosition = new Vector2(CardWidth * 0.5f + i * (CardWidth + CardGap), 0f);

            var img = cardGo.GetComponent<Image>();
            img.sprite = Resources.Load<Sprite>("ExhibitionCards/card_" + card.Key);
            img.preserveAspect = true;
            if (img.sprite == null)
                Debug.LogWarning($"[전시 모드] 카드 그림이 없어요: ExhibitionCards/card_{card.Key}");

            var btn = cardGo.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 0.97f, 0.85f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            btn.colors = colors;
            string id = card.Id;
            btn.onClick.AddListener(() => OnCardClicked(crt, id));
            buttons.Add(btn);

            if (cleared && stamp != null)
            {
                var st = new GameObject("DoneStamp", typeof(RectTransform), typeof(Image));
                var srt = (RectTransform)st.transform;
                srt.SetParent(crt, false);
                srt.anchorMin = srt.anchorMax = new Vector2(1f, 0f);
                srt.pivot = new Vector2(1f, 0f);
                srt.sizeDelta = new Vector2(150f, 150f);
                srt.anchoredPosition = new Vector2(-12f, 40f);
                var simg = st.GetComponent<Image>();
                simg.sprite = stamp;
                simg.raycastTarget = false;
            }
            else
            {
                // 아직 안 한 카드는 살짝 숨 쉬듯 커졌다 작아짐 (눌러 보라는 신호)
                tweens.Add(crt.DOScale(1.04f, 0.9f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(i * 0.3f));
            }
        }
    }

    void OnCardClicked(RectTransform card, string id)
    {
        if (entering) return;
        entering = true;
        foreach (var b in buttons) b.interactable = false;
        foreach (var t in tweens) t?.Kill();
        tweens.Clear();

        card.localScale = Vector3.one;
        tweens.Add(card.DOPunchScale(Vector3.one * 0.08f, 0.25f, 8, 0.8f)
            .OnComplete(() => nfc.HandleCardId(id)));

        // 미니게임으로 넘어가지 않은 경우(안내만 뜬 경우)에는 다시 누를 수 있게
        tweens.Add(DOVirtual.DelayedCall(1.5f, () =>
        {
            entering = false;
            foreach (var b in buttons) if (b != null) b.interactable = true;
        }));
    }

    void OnDestroy()
    {
        foreach (var t in tweens) t?.Kill();
    }
}
