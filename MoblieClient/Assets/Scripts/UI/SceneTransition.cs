using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 화면 전환을 부드럽게: 지금 화면을 찍어 맨 위에 덮어 둔 채로 다음 씬을 불러오고,
/// 그 사진을 천천히 투명하게 해서 이전 화면이 다음 화면으로 자연스럽게 녹아들게 합니다 (크로스 페이드).
/// 사용: SceneManager.LoadScene("이름") 대신 SceneTransition.Load("이름")
/// 씬에 따로 넣을 것은 없습니다 (처음 쓸 때 저절로 만들어지고 씬이 바뀌어도 남아 있음).
/// </summary>
public static class SceneTransition
{
    public const float DefaultFade = 0.45f;

    public static void Load(string sceneName, float fadeSeconds = DefaultFade)
    {
        Runner.Get().Go(sceneName, fadeSeconds);
    }

    private class Runner : MonoBehaviour
    {
        private static Runner _inst;
        private bool _busy;

        public static Runner Get()
        {
            if (_inst == null)
            {
                var go = new GameObject("[SceneTransition]");
                DontDestroyOnLoad(go);
                _inst = go.AddComponent<Runner>();
            }
            return _inst;
        }

        public void Go(string sceneName, float fade)
        {
            if (_busy) { SceneManager.LoadScene(sceneName); return; }   // 전환 중에 또 부르면 바로 이동
            StartCoroutine(Run(sceneName, fade));
        }

        private IEnumerator Run(string sceneName, float fade)
        {
            _busy = true;

            // 1) 지금 화면을 그대로 찍어 둠
            yield return new WaitForEndOfFrame();
            Texture2D shot = null;
            try { shot = ScreenCapture.CaptureScreenshotAsTexture(); } catch { shot = null; }

            GameObject cover = null;
            CanvasGroup group = null;
            if (shot != null)
            {
                cover = new GameObject("Cover", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
                cover.transform.SetParent(transform, false);
                var canvas = cover.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32000;                         // 모든 UI 보다 위
                group = cover.GetComponent<CanvasGroup>();
                group.blocksRaycasts = true;                         // 전환 중 두 번 눌리지 않게

                var img = new GameObject("Shot", typeof(RectTransform), typeof(RawImage));
                img.transform.SetParent(cover.transform, false);
                var rt = (RectTransform)img.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                img.GetComponent<RawImage>().texture = shot;
            }

            // 2) 다음 씬 불러오기 (원래와 같은 방식)
            SceneManager.LoadScene(sceneName);
            yield return null;
            yield return null;                                       // 새 씬의 첫 화면이 그려질 때까지

            // 3) 찍어 둔 이전 화면을 천천히 투명하게
            if (group != null)
            {
                float t = 0f;
                while (t < fade)
                {
                    t += Time.unscaledDeltaTime;
                    group.alpha = 1f - Mathf.SmoothStep(0f, 1f, t / fade);
                    yield return null;
                }
            }

            if (cover != null) Destroy(cover);
            if (shot != null) Destroy(shot);
            _busy = false;
        }
    }
}
