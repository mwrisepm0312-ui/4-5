using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 버튼을 눌렀을 때 "꾹" 들어가고, 떼면 "통" 튀어 돌아오는 효과.
///  - 누르는 동안: 빠르게 작아지고(쑥 들어감) 글자가 아래로 내려감 + 눌린 그림(위가 꺼진 그림)으로 바뀜
///  - 떼면: 살짝 커졌다가(1.04배) 원래 크기로 돌아옴
/// pulse 를 켜면 누르기 전까지 "여기를 누르세요" 하듯 통통 커졌다 작아짐.
/// 비활성(interactable=false) 버튼은 회색으로 보이게 함.
/// </summary>
[DisallowMultipleComponent]
public class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Tooltip("눌렀을 때 크기 (1 = 그대로). 0.92 보다 크면 0.92 로 씀 — 눌린 느낌이 잘 보이게")]
    public float pressScale = 0.92f;

    [Tooltip("눌렀을 때 글자가 내려가는 거리 (픽셀). 0 이면 버튼 높이의 5%")]
    public float textDrop = 0f;

    [Tooltip("누르기 전까지 통통 튀는 강조 효과")]
    public bool pulse = false;

    private const float MaxPressScale = 0.92f;   // 이보다 덜 줄어들면 눌렸는지 잘 안 보임
    private const float PressTime = 0.06f;       // 들어가는 시간 (초)
    private const float ReleaseTime = 0.18f;     // 튀어 돌아오는 시간 (초)
    private const float Overshoot = 1.04f;       // 뗄 때 살짝 커지는 정도

    private RectTransform _rt;
    private Vector3 _baseScale;
    private Button _btn;
    private Graphic _graphic;
    private TMP_Text[] _labels;
    private Vector2[] _labelPos;
    private bool _pressed;
    private float _k = 1f;          // 지금 크기 배율
    private float _from = 1f;       // 움직임 시작 배율
    private float _t = 1f;          // 0 → 1 진행
    private bool _releasing;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        _baseScale = _rt.localScale;
        _btn = GetComponent<Button>();
        _graphic = _btn != null ? _btn.targetGraphic : GetComponent<Graphic>();
        _labels = GetComponentsInChildren<TMP_Text>(true);
        _labelPos = new Vector2[_labels.Length];
        for (int i = 0; i < _labels.Length; i++) _labelPos[i] = _labels[i].rectTransform.anchoredPosition;
    }

    private void OnDisable()
    {
        _pressed = false;
        _releasing = false;
        _k = 1f;
        _t = 1f;
        if (_rt != null) _rt.localScale = _baseScale;
        MoveLabels(0f);
    }

    private float Target => Mathf.Min(pressScale, MaxPressScale);

    private float Drop
    {
        get
        {
            if (textDrop > 0f) return textDrop;
            return _rt != null ? _rt.rect.height * 0.05f : 0f;
        }
    }

    private void Update()
    {
        if (_t < 1f)
        {
            if (_pressed)
            {
                _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / PressTime);
                _k = Mathf.Lerp(_from, Target, 1f - (1f - _t) * (1f - _t));
            }
            else if (_releasing)
            {
                _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / ReleaseTime);
                // 앞 40%: 눌린 크기 → 1.04배, 뒤 60%: 1.04배 → 1
                _k = _t < 0.4f
                    ? Mathf.Lerp(_from, Overshoot, Mathf.SmoothStep(0f, 1f, _t / 0.4f))
                    : Mathf.Lerp(Overshoot, 1f, Mathf.SmoothStep(0f, 1f, (_t - 0.4f) / 0.6f));
                if (_t >= 1f) { _releasing = false; _k = 1f; }
            }
            _rt.localScale = _baseScale * _k;
        }
        else if (pulse && !_pressed)
        {
            _rt.localScale = _baseScale * (1f + 0.06f * Mathf.Sin(Time.unscaledTime * 4f));
        }

        if (_btn != null && _graphic != null)
        {
            Color target = _btn.interactable ? Color.white : new Color(0.65f, 0.65f, 0.65f, 0.85f);
            if (_graphic.color != target && _btn.transition == Selectable.Transition.SpriteSwap)
                _graphic.color = target;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_btn != null && !_btn.interactable) return;
        _pressed = true;
        _releasing = false;
        _from = _k;
        _t = 0f;
        MoveLabels(Drop);
    }

    public void OnPointerUp(PointerEventData eventData) { Release(); }
    public void OnPointerExit(PointerEventData eventData) { Release(); }

    private void Release()
    {
        if (!_pressed) return;
        _pressed = false;
        _releasing = true;
        _from = _k;
        _t = 0f;
        MoveLabels(0f);
    }

    private void MoveLabels(float drop)
    {
        if (_labels == null) return;
        for (int i = 0; i < _labels.Length; i++)
            if (_labels[i] != null) _labels[i].rectTransform.anchoredPosition = _labelPos[i] + Vector2.down * drop;
    }
}
