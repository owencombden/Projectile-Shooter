using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HexTile : MonoBehaviour
{
    public Vector3       startPos;
    [SerializeField] private float remainingHitPoints;    
    public float         maxHitPoints = 100f;
    public Renderer      tileRenderer;
    public ulong         ownerId;    // the parent platform
    public Vector2Int    gridCoords; // axial or offset grid coordinates
    public List<HexTile> neighbors = new List<HexTile>();

    
    
    private void OnEnable()
    {
        //Debug.Log($"{transform.name} has been enabled.");
        startPos     = transform.position;
        tileRenderer = transform.GetChild(0).GetComponent<Renderer>();
        remainingHitPoints    = maxHitPoints;
        UpdateColor();  
        
    }
    
    private void OnDisable()
    {
        //Debug.Log($"{transform.name} has been disabled.");            
    }
    

    public void ApplyBlastDamage(float baseDamage, int radius)
    {
        // store the coordinates of this hit-tile beforehand as it may get destroyed here
        Vector2Int blastOrigin = gridCoords;

        // center tile gets full damage
        ApplyDamage(baseDamage, 1f);

        //find the neighbours out to radius, and apply decreasing damage as distance increases
        var map = LevelManager.Instance.GetHexMap(ownerId);
        for (int ring = 1; ring <= radius; ring++)
        {
            float damagePercent = 1f - (ring / (float)(radius + 1));  // linear falloff        
            List<HexTile> ringTiles = HexUtils.GetHexRing(blastOrigin, map, ring);

            foreach (HexTile tile in ringTiles)
            {
                tile.ApplyDamage(baseDamage, damagePercent);
            }
        }
    }

    // New damage method with scaled percent
    public void ApplyDamage(float damage, float damagePercent = 1f)
    {
        float scaledDamage = damage * damagePercent;
        remainingHitPoints -= scaledDamage;

        if (remainingHitPoints <= 0)
        {
            RemoveFromPlay();
        }
        else
        {
            UpdateColor();
        }
    }


    void UpdateColor()
    {
        // Compute percent of health remaining
        float healthPercent = Mathf.Clamp01(remainingHitPoints / maxHitPoints);

        // Lerp from white (full health) to blue (no health)
        Color fullHealthColor = Color.white;
        Color lowHealthColor;

        if (ColorUtility.TryParseHtmlString("#023A7D", out lowHealthColor))
        {
            tileRenderer.material.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
        }        
    }

    void RemoveFromPlay()
    {
        //print("Tile " + gameObject.name + " has been destroyed.");
        LevelManager.Instance.RemoveHexTile(ownerId, gridCoords);
        AssetManager.Instance.ReturnHexTile(gameObject);
    } 
       
}
