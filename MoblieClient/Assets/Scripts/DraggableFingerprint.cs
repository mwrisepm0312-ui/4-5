using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggableFingerprint : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private Vector2 originalPosition;
    private Transform originalParent;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();

        // CanvasGroup 없으면 추가
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 원래 위치 저장
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        // Canvas 최상위로 이동 (다른 UI 위에 보이게)
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();

        // 드래그 중 레이캐스트 무시 (드롭존 감지용)
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 마우스/손가락 따라 이동
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 레이캐스트 다시 활성화
        canvasGroup.blocksRaycasts = true;

        // 드롭존 위에 없으면 원래 위치로 복귀
        ReturnToOriginal();
    }

    public void ReturnToOriginal()
    {
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = originalPosition;
    }
}