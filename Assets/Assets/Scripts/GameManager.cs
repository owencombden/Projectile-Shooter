using UnityEngine.SceneManagement;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public AssetManager assetManager;

    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
         // Confines the cursor to the game window
        Cursor.lockState = CursorLockMode.Confined;        
        //Cursor.visible = false; 
        //Cursor.lockState = CursorLockMode.Locked;  

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void reloadScene()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
