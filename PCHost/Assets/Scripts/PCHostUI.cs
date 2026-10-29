using DG.Tweening;
using FishNet;
using FishNet.Transporting.Tugboat;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PCHostUI : MonoBehaviour
{
    [Header("타이틀")]
    public TextMeshProUGUI titleText;

    [Header("시작 버튼")]
    public Button startButton;

    [Header("게임종료 버튼")]
    public Button quitButton;

    [Header("팝업 UI")]
    public GameObject dimBG;
    public GameObject popupPanel;
    public Button minusButton;
    public Button plusButton;
    public TextMeshProUGUI countText;
    public Button confirmButton;

    private int maxPlayers = 2;
    private Tween quitFloatTween;
    private float quitBaseY;


    void Start()
    {
        if (titleText != null)
        {
            string title = titleText.text;
            titleText.text = "";
            titleText.DOText(title, 1.5f).SetEase(Ease.Linear);
        }

        startButton.transform
            .DOLocalMoveY(startButton.transform.localPosition.y + 20f, 0.8f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        quitBaseY = quitButton.transform.localPosition.y;

        var trigger = quitButton.gameObject.AddComponent<EventTrigger>();

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ =>
        {
            quitFloatTween?.Kill();
            quitButton.transform.localPosition = new Vector3(
                quitButton.transform.localPosition.x, quitBaseY, quitButton.transform.localPosition.z);
            quitFloatTween = quitButton.transform
                .DOLocalMoveY(quitBaseY + 20f, 0.8f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        });
        trigger.triggers.Add(enterEntry);


        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ =>
        {
            quitFloatTween?.Kill();
            quitButton.transform
                .DOLocalMoveY(quitBaseY, 0.15f)
                .SetEase(Ease.OutSine);
        });
        trigger.triggers.Add(exitEntry);

        startButton.onClick.AddListener(OnStartClicked);
        minusButton.onClick.AddListener(OnMinusClicked);
        plusButton.onClick.AddListener(OnPlusClicked);
        confirmButton.onClick.AddListener(OnConfirmClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
        UpdateCountText();
    }

    void OnStartClicked()
    {
        dimBG.SetActive(true);
        popupPanel.SetActive(true);
    }

    void OnMinusClicked()
    {
        if (maxPlayers > 2) maxPlayers--;
        UpdateCountText();
    }

    void OnPlusClicked()
    {

        if (maxPlayers < 4) maxPlayers++;
        UpdateCountText();
    }

    void UpdateCountText()
    {
        countText.text = $"{maxPlayers}명";
        minusButton.interactable = maxPlayers > 2;
        plusButton.interactable = maxPlayers < 4;
    }

    void OnConfirmClicked()
    {
        dimBG.SetActive(false);
        popupPanel.SetActive(false);
        startButton.interactable = false;

        // 새 게임 시작 시 증거 초기화
        GameManager.ReceivedEvidences.Clear();

        var tugboat = InstanceFinder.NetworkManager.GetComponent<Tugboat>();
        tugboat.SetPort(7770);
        tugboat.SetMaximumClients(maxPlayers + 1);
        tugboat.SetTimeout(10, false);

        InstanceFinder.ServerManager.StartConnection();
        InstanceFinder.ClientManager.StartConnection();
        StartCoroutine(SpawnGameManagerAndMove());
    }

    void OnQuitClicked()
    {
        if (InstanceFinder.IsServerStarted)
            InstanceFinder.ServerManager.StopConnection(true);


        if (InstanceFinder.IsClientStarted)
            InstanceFinder.ClientManager.StopConnection();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator SpawnGameManagerAndMove()
    {
        yield return new WaitUntil(() => InstanceFinder.ServerManager.Started);
        var prefab = Resources.Load<GameObject>("GameManager");
        if (prefab != null)
        {
            var obj = Instantiate(prefab);
            InstanceFinder.ServerManager.Spawn(obj);
        }
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("WaitingScene");
    }

    void OnDestroy()
    {
        quitFloatTween?.Kill();
    }
}