using UnityEngine;
using System.Collections.Generic;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    public List<CharacterState> GetAllCharacterStates()
    {
        List<CharacterState> states = new List<CharacterState>();
        foreach (Targetable t in TargetManager.Instance.GetTargets())
        {
            if (t == null || !t.isActiveAndEnabled) continue;

            CharacterState state = new CharacterState
            {
                id = t.myId, // Make sure each targetable has a unique ID
                position = t.transform.position,
                rotation = t.transform.rotation,
                health = t.currentHealth,
                isAlive = t.isActiveAndEnabled
            };
            states.Add(state);
        }
        return states;
    }

    public List<BulletState> GetAllBulletStates()
    {
        List<BulletState> states = new List<BulletState>();
        Bullet[] bullets = FindObjectsByType<Bullet>(FindObjectsSortMode.None);

        foreach (Bullet bullet in bullets)
        {
            BulletState state = new BulletState(
                bullet.transform.position,
                bullet.transform.rotation,
                bullet.GetVelocity(), // Or bullet.rb.velocity if public
                bullet.bulletType
            );

            states.Add(state);
        }

        return states;
    }

    // You can add similar methods for tiles, bullets, pickups, etc.
}