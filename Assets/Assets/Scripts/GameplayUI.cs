//*  SET THIS SAME SCRIPT TO WORK WITH TRADITIONAL UNITY UI
//  SEARCH FOR GAMEPLAYUI IN SOLUTION AND REINSTATE EVERYTHING

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Linq;

public class GameplayUI : NetworkBehaviour
{
    public static GameplayUI Instance;

    [Header("UI Elements")]
    private UIDocument uiDocument;
    private AudioSource audioSource;

    // ui containers
    private VisualElement optionsContainer;
    private VisualElement gameplayUIContainer;
    
    // gameplay ui
    private Button optionsButton;
    private Label pausedLabel;
    private Label gameOverLabel;    
    private Label noAmmoLabel;
    private ProgressBar ammoProgressBar;

    // options menu
    private Button optionsBackButton;
    private Button quitGameButton;

    // all buttons
    private List<Button> allMenuButtons = new List<Button>();

    private bool noAmmoIsBusy = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        uiDocument = GetComponent<UIDocument>();
        audioSource = GetComponent<AudioSource>();

        // ui containers
        optionsContainer    = uiDocument.rootVisualElement.Q("OptionsContainer") as VisualElement;
        gameplayUIContainer = uiDocument.rootVisualElement.Q("GameplayUIContainer") as VisualElement;

        // gameplay UI
        optionsButton   = uiDocument.rootVisualElement.Q("OptionsButton") as Button;
        pausedLabel     = uiDocument.rootVisualElement.Q("PausedLabel") as Label;
        gameOverLabel   = uiDocument.rootVisualElement.Q("GameOverLabel") as Label;
        noAmmoLabel     = uiDocument.rootVisualElement.Q("NoAmmoLabel") as Label;
        ammoProgressBar = uiDocument.rootVisualElement.Q("AmmoProgressBar") as ProgressBar;

        // options menu
        optionsBackButton = uiDocument.rootVisualElement.Q("OptionsBackButton") as Button;
        quitGameButton    = uiDocument.rootVisualElement.Q("QuitGameButton") as Button;

        // button callbacks
        // register callbacks for each button, as well as 'all' buttons

        // menu swapping callbacks
        optionsButton.RegisterCallback<ClickEvent>(OnOptionsButtonClicked);
        optionsBackButton.RegisterCallback<ClickEvent>(OnOptionsBackButtonClicked);
        quitGameButton.RegisterCallback<ClickEvent>(OnQuitGameClicked);

        // all-button callbacks
        allMenuButtons = uiDocument.rootVisualElement.Query<Button>().ToList();
        foreach (Button menuButton in allMenuButtons)
        {
            menuButton.RegisterCallback<ClickEvent>(OnAnyButtonClicked);
        }

        // initial visibility
        gameplayUIContainer.style.display = DisplayStyle.Flex;
        optionsContainer.style.display    = DisplayStyle.None;
        pausedLabel.visible   = false;
        gameOverLabel.visible = false;
        noAmmoLabel.visible   = false;
    }

    
    //----------------------
    // BUTTON CALLBACKS
    //----------------------
    
    private void OnOptionsButtonClicked(ClickEvent evt)
    {
        // hide the gameplay ui, show the options menu
        Debug.Log("switching to single player menu container");
        gameplayUIContainer.style.display = DisplayStyle.None;
        optionsContainer.style.display = DisplayStyle.Flex;
    }

    private void OnOptionsBackButtonClicked(ClickEvent evt)
    {
        // hide the gameplay ui, show the options menu
        Debug.Log("switching to single player menu container");
        optionsContainer.style.display = DisplayStyle.None;
        gameplayUIContainer.style.display = DisplayStyle.Flex;
    }

    private void OnQuitGameClicked(ClickEvent evt)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("GameplayUI has detected that the host is quitting.");
            GameManager.Instance.EndGameServerRpc(); // notify everyone
        }
        else
        {
            Debug.Log("GameplayUI has detected that a client is quitting.");
            ulong clientId = NetworkManager.Singleton.LocalClientId;
            GameManager.Instance.NotifyServerClientIsQuittingServerRpc(clientId);            

            SceneManager.LoadScene("MainMenuScene");
        }
    }

    private void OnAnyButtonClicked(ClickEvent evt)
    {
        // play button-click sound
        audioSource.Play();
    }


    //----------------------
    // DISPLAY MESSAGES
    //----------------------

    public void DisplayPausedMessage()
    {

        Debug.Log("UI has been triggered to show pause message");
        pausedLabel.text = "GAME IS PAUSED";
        pausedLabel.visible = true;
    }

    public void ClearPausedMessage()
    {
        pausedLabel.text = "";
        pausedLabel.visible = false;
    }

    public void DisplayGameOverMessage(string message)
    {
        Debug.Log("UI has been triggered to show game over message");
        gameOverLabel.text = message;
        gameOverLabel.visible = true;
        float duration = 5f;

        StartCoroutine(ClearGameOverMessage(duration));
    }

    private IEnumerator ClearGameOverMessage(float duration)
    {
        yield return new WaitForSeconds(duration);
        gameOverLabel.visible = false;
    }    

    public void DisplayNoAmmoMessage()
    {
        if(noAmmoIsBusy) { return; }        
        noAmmoIsBusy = true;

        Debug.Log("UI has been triggered to show no ammo message");
        noAmmoLabel.text = "!!  NO AMMO  !!";
        noAmmoLabel.visible = true;
        float duration = 1.5f;

        StartCoroutine(ClearNoAmmoMessage(duration));
    }

    private IEnumerator ClearNoAmmoMessage(float duration)
    {
        yield return new WaitForSeconds(duration);
        noAmmoLabel.visible = false;        
        noAmmoIsBusy = false;
    }

    
    //----------------------
    // AMMO
    //----------------------

    public void SetMaxLimitOnAmmoProgressBar(int currMaxValue)
    {
        ammoProgressBar.highValue = currMaxValue;
    }

    public void SetAmmoUI(int currValue)
    {
        if (currValue < 0) { return; }

        ammoProgressBar.value = currValue;
    }
    

    // this Instance is a global static reference.  Need to ensure that ref is cleared whenever reloading a scene.
    private new void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }    
}


