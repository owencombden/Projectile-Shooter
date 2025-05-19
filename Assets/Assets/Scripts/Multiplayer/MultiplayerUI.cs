using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MultiplayerUI : MonoBehaviour
{
    public Button hostButton;
    public Button joinButton;
    public TMP_Text statusText;
    public TMP_InputField ipInputField;

    void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
    }

    void OnHostClicked()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");

        statusText.text = "Starting Host...";

        MultiplayerManager.Instance.StartHost();
    }

    void OnJoinClicked()
    {
        string ip = string.IsNullOrWhiteSpace(ipInputField.text)
                    ? "127.0.0.1"
                    : ipInputField.text.Trim();

        statusText.text = $"Joining {ip}…";
        MultiplayerManager.Instance.StartClient(ip);
    }

    void Update()
    {
        if (MultiplayerManager.Instance == null) return;

        if (MultiplayerManager.Instance.IsHost)
            statusText.text = "Status: Host";
        else if (MultiplayerManager.Instance.IsClient)
            statusText.text = "Status: Client";
    }
}
