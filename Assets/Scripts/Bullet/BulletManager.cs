using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

public class BulletManager : MonoBehaviour
{
    [Header("Channels")]
    [SerializeField]
    private AskforBulletChannelSO askforBullet;
    [Header("Transform")]
    [SerializeField] private Transform bulletParent;

    [FormerlySerializedAs("bullet")] [SerializeField]
    private Bullet bulletPrefab;
    private BulletFactory factory = new BulletFactory();
    private ObjectPool<Bullet> _pool;

    private int currentBulletId = 0;

    public void OnEnable()
    {
        askforBullet.Subscribe(SpawnBullet);
        _pool = new ObjectPool<Bullet>(() => Instantiate(bulletPrefab),
            bullet => { bullet.gameObject.SetActive(true); }, bullet => { OnRelease(bullet); },
            bullet => { Destroy(bullet.gameObject); }, false, 10, 100);
    }

    private static void OnRelease(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);
    }

    public void OnDisable()
    {
        askforBullet.Unsubscribe(SpawnBullet);
    }

    public void KillBullet(Bullet bul)
    {
        _pool.Release(bul);
    }

    private void SpawnBullet(int id,Vector3 pos, Vector3 forw)
    {
        var newBullet = _pool.Get();
        newBullet.ID = id;
        factory.ConfigureBullet(ref newBullet, pos, forw, bulletParent);
        newBullet.Init(KillBullet);
        newBullet.StartBullet();

        //Todo:
    }
}