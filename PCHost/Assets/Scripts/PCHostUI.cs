using FishNet;
using FishNet.Transporting.Tugboat;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class PCHostUI : MonoBehaviour
{
    [Header("타이틀")]
    public TextMeshProUGUI titleText;

    [Header("시작 버튼")]
    public Button startButton;

    [Header("팝업 UI")]
    public GameObject dimBG;
    public GameObject popupPanel;
    public Button minusButton;
    public Button plusButton;
    public TextMeshProUGUI countText;
    public Button confirmButton;

    private int maxPlayers = 2;

    void Start()
    {
        // 타이틀 타이핑 효과
        if (titleText != null)
        {
            string title = titleText.text;
            titleText.text = "";
            titleText.DOText(title, 1.5f).SetEase(Ease.Linear);
        }

        // 버튼 둥실둥실 애니메이션
        startButton.transform
            .DOLocalMoveY(startButton.transform.localPosition.y + 20f, 0.8f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        startButton.onClick.AddListener(OnStartClicked);
        minusButton.onClick.AddListener(OnMinusClicked);
        plusButton.onClick.AddListener(OnPlusClicked);
        confirmButton.onClick.AddListener(OnConfirmClicked);
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

        var tugboat = InstanceFinder.NetworkManager.GetComponent<Tugboat>();
        tugboat.SetPort(7770);
        tugboat.SetMaximumClients(maxPlayers + 1);
        tugboat.SetTimeout(10, false);

        InstanceFinder.ServerManager.StartConnection();
        InstanceFinder.ClientManager.StartConnection();
        StartCoroutine(SpawnGameManagerAndMove());
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
}