using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class StoryStepManager : MonoBehaviour
{
    [Header("스텝별 이미지 (CanvasGroup) - 순서대로")]
    public CanvasGroup[] imageGroups;

    [Header("스텝별 텍스트 - 순서대로")]
    [TextArea(2, 5)]
    public string[] storyLines;

    [Header("스텝별 나레이션 오디오 - 순서대로")]
    public AudioClip[] narrationClips;

    [Header("표시할 TMP 텍스트 (하단 중앙 하나)")]
    public TMP_Text storyText;

    [Header("페이드인 시간 (초)")]
    public float fadeDuration = 1f;

    [Header("타이핑 속도 (초)")]
    public float charInterval = 0.03f;

    [Header("타이핑 완료 후 다음 스텝까지 대기 시간 (초)")]
    public float waitAfterTyping = 2f;

    private AudioSource _audioSource;
    private bool _finishedAll = false;

    void Start()
    {
        // AudioSource 자동 추가
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;

        // 이미지 전부 숨기기
        if (imageGroups != null)
        {
            foreach (var cg in imageGroups)
            {
                if (cg == null) continue;
                cg.alpha = 0f;
                cg.gameObject.SetActive(false);
            }
        }

        if (storyText != null)
            storyText.text = "";

        StartCoroutine(AutoPlay());
    }

    private IEnumerator AutoPlay()
    {
        int maxSteps = Mathf.Max(
            imageGroups != null ? imageGroups.Length : 0,
            storyLines != null ? storyLines.Length : 0
        );

        for (int i = 0; i < maxSteps; i++)
        {
            // 1) 이미지 페이드인
            if (imageGroups != null &&
                i < imageGroups.Length &&
                imageGroups[i] != null)
            {
                yield return StartCoroutine(FadeIn(imageGroups[i]));
            }

            // 2) 나레이션 오디오 재생 (자막 타이핑과 동시)
            if (narrationClips != null &&
                i < narrationClips.Length &&
                narrationClips[i] != null)
            {
                _audioSource.clip = narrationClips[i];
                _audioSource.Play();
            }

            // 3) 하단 텍스트 타이핑
            if (storyText != null &&
                storyLines != null &&
                i < storyLines.Length)
            {
                yield return StartCoroutine(TypeLine(storyLines[i]));
            }

            // 4) 타이핑 완료 후 대기
            // 오디오가 아직 재생 중이면 끝날 때까지 기다림
            if (_audioSource.isPlaying)
                yield return new WaitWhile(() => _audioSource.isPlaying);
            else
                yield return new WaitForSeconds(waitAfterTyping);
        }

        OnAllStepsFinished();
    }

    private IEnumerator FadeIn(CanvasGroup cg)
    {
        if (!cg.gameObject.activeSelf)
            cg.gameObject.SetActive(true);

        cg.alpha = 0f;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }

        cg.alpha = 1f;
    }

    private IEnumerator TypeLine(string line)
    {
        storyText.text = "";

        for (int i = 0; i < line.Length; i++)
        {
            storyText.text += line[i];
            yield return new WaitForSeconds(charInterval);
        }
    }

    [Header("완료 후 설정")]
    public string nextScene = "PC_LobbyScene";
    public bool unlockJobSelect = false;  // 오프닝씬에서만 true
    public bool closeServer = false;      // 성공/실패씬에서만 true

    private void OnAllStepsFinished()
    {
        if (_finishedAll) return;
        _finishedAll = true;

        var gm = FindFirstObjectByType<GameManager>();

        if (unlockJobSelect)
            gm?.UnlockJobSelectServerRpc();

        if (closeServer)
        {
            var nm = FishNet.InstanceFinder.NetworkManager;
            if (nm != null && nm.IsServerStarted)
                nm.ServerManager.StopConnection(true);
        }

        SceneManager.LoadScene(nextScene);
    }
}