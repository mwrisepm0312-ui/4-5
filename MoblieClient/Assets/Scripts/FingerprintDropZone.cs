using UnityEngine;
using UnityEngine.EventSystems;

public class FingerprintDropZone : MonoBehaviour, IDropHandler
{
    [Header("이 슬롯의 용의자 이름")]
    public string suspectName;

    public void OnDrop(PointerEventData eventData)
    {
        // 드래그된 오브젝트가 DraggableFingerprint인지 확인
        DraggableFingerprint draggable =
            eventData.pointerDrag?.GetComponent<DraggableFingerprint>();

        if (draggable == null) return;

        // ForensicGame에 결과 전달
        ForensicGame forensicGame = FindFirstObjectByType<ForensicGame>();
        if (forensicGame != null)
        {
            forensicGame.OnSuspectDropped(suspectName, draggable);
        }
    }
}