using UnityEngine;

public struct BulletState
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public Bullet.BulletType type;

    public BulletState(Vector3 pos, Quaternion rot, Vector3 vel, Bullet.BulletType type)
    {
        // need to figure out 'ownerID' for here.  it's stored on Targetable
        this.position = pos;
        this.rotation = rot;
        this.velocity = vel;
        this.type = type;
    }    
}