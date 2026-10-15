using UnityEngine;

public class TouchEffect : MonoBehaviour
{
    [Header("터치 이펙트 프리팹")]
    public GameObject effectPrefab;

    [Header("카메라 (비워두면 Camera.main 사용)")]
    public Camera targetCamera;

    [Header("파티클이 카메라에서 떨어진 거리")]
    public float distanceFromCamera = 5f;

    void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    SpawnEffect(touch.position);
                }
            }
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            SpawnEffect(Input.mousePosition);
        }
#endif
    }

    void SpawnEffect(Vector2 screenPos)
    {
        if (effectPrefab == null) return;

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        Vector3 worldPos = cam.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, distanceFromCamera));

        GameObject fx = Instantiate(effectPrefab, worldPos, Quaternion.identity);

        // 파티클이 카메라를 향하도록 회전
        fx.transform.LookAt(cam.transform);

        Destroy(fx, 2f);
    }
}
