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

    [Header("완료 후 설정")]
    public string nextScene = "PC_LobbyScene";
    public bool unlockJobSelect = false;
    public bool closeServer = false;

    private AudioSource _audioSource;
    private bool _finishedAll = false;

    void Start()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;

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
            if (imageGroups != null && i < imageGroups.Length && imageGroups[i] != null)
                yield return StartCoroutine(FadeIn(imageGroups[i]));

            if (narrationClips != null && i < narrationClips.Length && narrationClips[i] != null)
            {
                _audioSource.clip = narrationClips[i];
                _audioSource.Play();
            }

            if (storyText != null && storyLines != null && i < storyLines.Length)
                yield return StartCoroutine(TypeLine(storyLines[i]));

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

    private void OnAllStepsFinished()
    {
        if (_finishedAll) return;
        _finishedAll = true;

        var gm = FindFirstObjectByType<GameManager>();

        if (unlockJobSelect)
            gm?.UnlockJobSelectServerRpc();

        if (closeServer)
            StartCoroutine(CloseServerAfterRpc(gm));
        else
            SceneManager.LoadScene(nextScene);
    }

    private IEnumerator CloseServerAfterRpc(GameManager gm)
    {
        var nm = FishNet.InstanceFinder.NetworkManager;

        if (gm == null || nm == null || !nm.IsServerStarted)
        {
            Debug.LogWarning("[StoryStep] GameManager 또는 서버 없음 — 그냥 씬 전환");
            SceneManager.LoadScene(nextScene);
            yield break;
        }

        // ObserversRpc를 ServerRpc 거치지 않고 서버에서 직접 호출
        // gm.LoadClientSceneServerRpc() 대신 아래처럼:
        // ServerRpc → ObserversRpc 체인 대신, 서버에서 바로 ObserversRpc 트리거
        gm.SendClientToLobbyFromServer();

        Debug.Log("[StoryStep] SendClientToLobbyFromServer 호출 — 2초 대기");

        // 모바일이 씬 로드할 충분한 시간 확보
        yield return new WaitForSeconds(2f);

        Debug.Log("[StoryStep] 서버 종료");
        if (nm.IsServerStarted)
            nm.ServerManager.StopConnection(true);

        yield return new WaitForSeconds(0.1f);

        SceneManager.LoadScene(nextScene);
    }
}