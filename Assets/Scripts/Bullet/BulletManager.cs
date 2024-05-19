using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

public class BulletManager : MonoBehaviour
{
    [Header("Channels")]

    [Header("Transform")]
    [SerializeField] private Transform bulletParent;
    [SerializeField] private Transform world;
    [SerializeField] private Transform turretWorld;

    [FormerlySerializedAs("bullet")] [SerializeField]
    private Bullet bulletPrefab;
    private BulletFactory factory = new BulletFactory();
    private ObjectPool<Bullet> _pool;


    public void Awake()
    {

        _pool = new ObjectPool<Bullet>(() => Instantiate(bulletPrefab, bulletParent),
            bullet => { bullet.gameObject.SetActive(true); }, bullet => { bullet.gameObject.SetActive(false); },
            bullet => { Destroy(bullet.gameObject); }, false, 20, 100);
    }

    public void OnDisable()
    {

    }

    public void KillBullet(Bullet bul)
    {
        _pool.Release(bul);
    }

    private void SpawnBullet(Transform pos, string layer, Quaternion rotation)
    {
        var newBullet = _pool.Get();
     
        factory.ConfigureBullet(ref newBullet, pos, layer, world, bulletParent, rotation);
            
        newBullet.Init(KillBullet);
        newBullet.StartBullet();
    }
}