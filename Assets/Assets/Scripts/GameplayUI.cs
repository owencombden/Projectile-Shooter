using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;

public class GameplayUI : MonoBehaviour
{
    public static GameplayUI Instance;

    [Header("UI Elements")]
    private UIDocument uiDocument;

    private Label pausedLabel;
    private Label gameOverLabel;    
    private Label noAmmoLabel;
    private ProgressBar ammoProgressBar;

    private bool noAmmoIsBusy = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        uiDocument = GetComponent<UIDocument>();
        pausedLabel = uiDocument.rootVisualElement.Q("PausedLabel") as Label;
        pausedLabel.visible = false;
        gameOverLabel = uiDocument.rootVisualElement.Q("GameOverLabel") as Label;
        gameOverLabel.visible = false;
        noAmmoLabel = uiDocument.rootVisualElement.Q("NoAmmoLabel") as Label;
        noAmmoLabel.visible = false;
        ammoProgressBar = uiDocument.rootVisualElement.Q("AmmoProgressBar") as ProgressBar;
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

    public void SetMaxLimitOnAmmoProgressBar(int currMaxValue)
    {
        ammoProgressBar.highValue = currMaxValue;
    }

    public void SetAmmoUI(int currValue)
    {
        if (currValue < 0) { return; }

        ammoProgressBar.value = currValue;
    }

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

    // this Instance is a global static reference.  Need to ensure that ref is cleared whenever reloading a scene.
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }    
}
