using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class AmmoSpawnPrefab : MonoBehaviour
{
    private List<string> ammoTypes;  //gets set during spawn.  a collection of bullet types ie: [normal, normal, ice, fire]
    private AssetManager assetManager;
    private PickupManager pickupManager;
    private PlayerMaster playerMaster;

    private float minLifetime = 6f;
    private float maxLifetime = 8f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        assetManager = GameObject.Find("AssetManager").GetComponent<AssetManager>();
        pickupManager = GameObject.Find("PickupManager").GetComponent<PickupManager>();
        playerMaster = assetManager.GetPlayer().GetComponent<PlayerMaster>();
        
        // if prefab is not picked up after a short time have it blink, then fade out & despawn
        // note:  material on the gfx obj needs to be set to 'transparent' for alpha changes to have any effect
        
        
        float lifetime = Random.Range(minLifetime, maxLifetime);
        float fadeTime = 0.1f;
        int numFadeRepeats = 15;
        float solidTime = lifetime - (fadeTime*numFadeRepeats);
        Transform gfxContainer = transform.GetChild(0);
        for (int i = 0; i < gfxContainer.childCount; i++)
        {            
            GameObject prefabBullet = gfxContainer.GetChild(i).gameObject;
            LeanTween.alpha(prefabBullet, 0f, fadeTime).setDelay(solidTime).setRepeat(numFadeRepeats).setLoopPingPong();
        }
        // despawn if the ammoDrop reaches it's lifetime
        Invoke("AmmoDropExpired", lifetime);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {        
        if(other.tag == "Player")
        {
            if(LeanTween.isTweening(gameObject)) { LeanTween.cancel(gameObject); }       
            playerMaster.ReloadGun(ammoTypes);
            pickupManager.RemoveAmmoDrop(); // trigger a new spawn
            assetManager.RemoveGameObject(gameObject, 0.1f);           
        }
    }

    private void AmmoDropExpired()
    { 
        if(LeanTween.isTweening(gameObject)) { LeanTween.cancel(gameObject); }  
        pickupManager.RemoveAmmoDrop(); // trigger a new spawn                     
        assetManager.RemoveGameObject(gameObject, 0f);
    }

    public void SetAmmoTypes(List<string> bulletTypes)
    {
        ammoTypes = bulletTypes;
    }
  
}
