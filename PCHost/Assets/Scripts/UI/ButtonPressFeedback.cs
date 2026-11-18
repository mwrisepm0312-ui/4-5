using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 버튼을 눌렀을 때 살짝 작아지고(쑥 들어감) 글자가 내려가는 효과.
/// pulse 를 켜면 누르기 전까지 "여기를 누르세요" 하듯 통통 커졌다 작아짐 (느낌표 버튼 등).
/// 비활성(interactable=false) 버튼은 회색으로 보이게 함.
/// </summary>
[DisallowMultipleComponent]
public class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Tooltip("눌렀을 때 크기 (1 = 그대로)")]
    public float pressScale = 0.96f;

    [Tooltip("눌렀을 때 글자가 내려가는 거리 (픽셀). 버튼 스킨의 받침 두께와 같게")]
    public float textDrop = 0f;

    [Tooltip("누르기 전까지 통통 튀는 강조 효과")]
    public bool pulse = false;

    private RectTransform _rt;
    private Vector3 _baseScale;
    private Button _btn;
    private Graphic _graphic;
    private TMP_Text[] _labels;
    private Vector2[] _labelPos;
    private bool _pressed;

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
        Release();
    }

    private void Update()
    {
        if (pulse && !_pressed)
            _rt.localScale = _baseScale * (1f + 0.06f * Mathf.Sin(Time.unscaledTime * 4f));

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
        _rt.localScale = _baseScale * pressScale;
        for (int i = 0; i < _labels.Length; i++)
            _labels[i].rectTransform.anchoredPosition = _labelPos[i] + Vector2.down * textDrop;
    }

    public void OnPointerUp(PointerEventData eventData) { Release(); }
    public void OnPointerExit(PointerEventData eventData) { Release(); }

    private void Release()
    {
        _pressed = false;
        if (_rt != null) _rt.localScale = _baseScale;
        if (_labels == null) return;
        for (int i = 0; i < _labels.Length; i++)
            if (_labels[i] != null) _labels[i].rectTransform.anchoredPosition = _labelPos[i];
    }
}
