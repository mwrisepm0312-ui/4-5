using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// TimeSlider 위에 역삼각형 5개를 자동 배치하고
/// 위아래 왕복 애니메이션을 적용합니다.
/// CCTVGameManage 오브젝트에 붙여주세요.
/// </summary>
public class SliderArrowSpawner : MonoBehaviour
{
    [Header("참조")]
    public Slider timeSlider;
    public Sprite triangleSprite;

    [Header("삼각형 크기 · 색상")]
    private Vector2 arrowSize = new Vector2(130f, 105f);
    private Color arrowColor;

    [Header("위치")]
    [Tooltip("슬라이더 상단에서 위로 띄우는 거리 (px)")]
    public float offsetAboveSlider = 40f;

    [Header("왕복 애니메이션")]
    public float bounceDistance = 12f;
    public float bounceDuration = 0.65f;
    public Ease bounceEase = Ease.InOutSine;

    // 5개 시간 눈금 위치 (23:00 / 23:30 / 00:00 / 00:30 / 01:00)
    private readonly float[] _ratios = { 0f, 0.25f, 0.5f, 0.75f, 1f };

    private readonly RectTransform[] _arrows = new RectTransform[5];

    void Start()
    {
        if (timeSlider == null) { Debug.LogError("[SliderArrowSpawner] TimeSlider 미연결"); return; }
        if (triangleSprite == null) { Debug.LogError("[SliderArrowSpawner] triangleSprite 미연결"); return; }
        ColorUtility.TryParseHtmlString("#87CEEB", out arrowColor);
        SpawnArrows();
    }

    void SpawnArrows()
    {
        RectTransform sliderRT = timeSlider.GetComponent<RectTransform>();
        float sliderWidth = sliderRT.rect.width;
        float sliderHeight = sliderRT.rect.height;

        for (int i = 0; i < _ratios.Length; i++)
        {
            GameObject go = new GameObject($"SwipeArrow_{i}",
                                           typeof(RectTransform), typeof(Image));
            go.transform.SetParent(sliderRT, false);
            go.transform.SetAsLastSibling();

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = arrowSize;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(
                _ratios[i] * sliderWidth,
                sliderHeight + offsetAboveSlider
            );

            Image img = go.GetComponent<Image>();
            img.sprite = triangleSprite;
            img.color = arrowColor;
            img.preserveAspect = true;
            img.raycastTarget = false;

            _arrows[i] = rt;

            rt.DOAnchorPos(rt.anchoredPosition + Vector2.down * bounceDistance, bounceDuration)
              .SetEase(bounceEase)
              .SetLoops(-1, LoopType.Yoyo)
              .SetDelay(i * 0.1f);
        }
    }

    void OnDestroy()
    {
        foreach (var rt in _arrows)
            rt?.DOKill();
    }
}
