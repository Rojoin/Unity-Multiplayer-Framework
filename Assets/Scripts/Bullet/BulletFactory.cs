
using UnityEngine;


public class BulletFactory
{
    public void ConfigureBullet(ref Bullet newBullet,Vector3 pos,Vector3 forw,  Transform bulletParent)
    {
        newBullet.transform.rotation = Quaternion.identity;
        newBullet.transform.position = pos;
        newBullet.transform.forward = forw;
        newBullet.enabled = true;
    }
}