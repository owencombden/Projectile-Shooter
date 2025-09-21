using UnityEngine;

// this interface is shared by both player and AI
public interface ICharacterInputProvider
{
    Vector2 MoveInput { get; }
    Vector2 LookInput { get; }
    bool ShootAtTarget { get; }
}
