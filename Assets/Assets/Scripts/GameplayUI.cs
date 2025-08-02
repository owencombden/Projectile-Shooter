using UnityEngine;
using System.Collections;
using TMPro;

public class GameplayUI : MonoBehaviour
{
    public static GameplayUI Instance;

    [SerializeField] private CanvasGroup gameOverPanel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float gameOverFadeInDuration = 0.5f;
    [SerializeField] private float gameOverPanelDuration = 3f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);


        gameOverPanel.alpha = 0f;
        gameOverPanel.gameObject.SetActive(false);
    }

    public void ShowGameOverBanner(bool isWinner)
    {
        //Debug.Log("Showing panel...");
        string message = isWinner ? "You Won!" : "Better Luck Next Time!";
        messageText.text = message;

        gameOverPanel.gameObject.SetActive(true);
        StartCoroutine(FadeInAndOut());
    }

    private IEnumerator FadeInAndOut()
    {
        // Fade in
        float t = 0f;
        while (t < gameOverFadeInDuration)
        {
            t += Time.deltaTime;
            gameOverPanel.alpha = t / gameOverFadeInDuration;
            yield return null;
        }

        yield return new WaitForSeconds(gameOverPanelDuration);

        // Fade to black (optional)

        // hide gameOver panel
        gameOverPanel.alpha = 0f;
        gameOverPanel.gameObject.SetActive(false);
    }
    
    // this Instance is a global static reference.  Need to ensure that ref is cleared whenever reloading a scene.
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }    
}
