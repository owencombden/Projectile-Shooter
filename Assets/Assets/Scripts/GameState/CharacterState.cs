// CharacterState.cs
using UnityEngine;

[System.Serializable]
public struct CharacterState
{
    public string id;
    public Vector3 position;
    public Quaternion rotation;
    public float health;
    public bool isAlive;
}
