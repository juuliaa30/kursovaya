using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using MyNetwork;
using System.Linq;

public class UIManager : MonoBehaviour
{
    private static UIManager _singleton;

    public static UIManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(UIManager)}: instance already exists, destroing duplicate!");
                Destroy(value);
            }
        }
    }

    [Header("Connect")]
    [SerializeField] private GameObject connectUI;
    [SerializeField] private InputField usernameField;

    [Header("Finish UI")]
    [SerializeField] private GameObject finishUI;
    [SerializeField] private Text winnerText;
    [SerializeField] private Camera finishCamera;

    [Header("Race UI")]
    [SerializeField] private GameObject raceUI;
    [SerializeField] private Text countdownText;
    [SerializeField] private Text raceMessageText;

    private void Awake()
    {
        Singleton = this;
        connectUI.SetActive(true);
        finishUI.SetActive(false);
        raceUI.SetActive(false);
    }



    public void ConnectClicked()
    {
        usernameField.interactable = false;
        connectUI.SetActive(false);

        NetworkManager.Singleton.Connect();
    }

    public void BackToMain()
    {
        usernameField.interactable = true;
        connectUI.SetActive(true);
    }
    
    public void SendName()
    {
        Message message = Message.Create(MessageSendMode.reliable, (ushort)ClientToServerId.name);
        message.AddString(usernameField.text);
        NetworkManager.Singleton.Client.Send(message);
    }

    public void ShowWinner(string winnerName)
    {
        StartCoroutine(SwitchCameraCoroutine());

        connectUI.SetActive(false);
        raceUI.SetActive(false);
        
        winnerText.text = $"{winnerName} wins the race!";
        finishUI.SetActive(true);
        finishUI.transform.SetAsLastSibling();
    }

    private IEnumerator SwitchCameraCoroutine()
    {
        finishCamera.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < 5f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public void UpdateCountdown(int seconds)
    {
        countdownText.text = seconds.ToString();
        countdownText.gameObject.SetActive(true);

        if (seconds <= 0)
            countdownText.gameObject.SetActive(false);
    }

    public void ShowRaceMessage(string message)
    {
        raceMessageText.text = message;
        StartCoroutine(HideMessageAfter(2f));
    }

    private IEnumerator HideMessageAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        raceMessageText.text = "";
    }

    public void StartRaceUI()
    {
        connectUI.SetActive(false);
        finishUI.SetActive(false);
        raceUI.SetActive(true);

        countdownText.gameObject.SetActive(false);
        raceMessageText.text = "";
    }

    [MessageHandler((ushort)ServerToClientId.RaceStateUpdate)]
    private static void HandleRaceState(Message message)
    {
        string state = message.GetString();
        UIManager.Singleton.ShowRaceMessage(state);

        if (state == "GO!")
        {
            var localPlayer = Player.list.Values.FirstOrDefault(p => p.IsLocal);
            if (localPlayer != null)
            {
                var controller = localPlayer.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.EnableInput();
                    Debug.Log("Movement enabled for local player");
                }
            }
        }
    }
}
