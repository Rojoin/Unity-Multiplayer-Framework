using System;
using System.Collections;
using System.Threading;

using UnityEngine;

/// <summary>
/// Class for the BulletClass
/// </summary>
public class Bullet : MonoBehaviour
{
    private Action<Bullet> killAction;
    public float Velocity { get; set; } = 50f;
    public float Damage { get; set; } = 30f;
    public static float maxAliveTime = 7f;
   // public DirectionHandler DirHandler { get; set; }
    private Transform world;
    private Vector3 direction;
    
    public void Init(Action<Bullet> onKill)
    {
        killAction = onKill;
    }
    private IEnumerator OnStart()
    {
        float timer = 0;
        while (gameObject.activeSelf)
        {
            timer += Time.deltaTime;
         //TODO:CHANGE for current player direction
         // direction = DirHandler.GetDirection(transform, world);
            transform.localPosition += Time.deltaTime * Velocity * direction;
            if (timer > maxAliveTime)
            {
                killAction(this);
            }
            yield return null;
        }
    }
    
    /// <summary>
    /// Set the World of the bullet
    /// </summary>
    /// <param name="worldTransform">World to use calculations</param>
    public void SetWorld(Transform worldTransform)
    {
        world = worldTransform;
        direction = world.transform.InverseTransformDirection(transform.forward);
    }

    /// <summary>
    /// Set bullet spawnPosition
    /// </summary>
    /// <param name="spawnPosition">Spawn position of the bullet</param>
    public void SetStartPosition(Transform spawnPosition)
    {
        transform.position = spawnPosition.position;
    }

    /// <summary>
    /// Destroy the GameObject attached to the bullet
    /// </summary>
    public void DestroyGameObject()
    {
        Destroy(this.gameObject);
    }
    public void StartBullet()
    {
        StartCoroutine(OnStart());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<HealthSystem>(out var health))
        {
            health.ReceiveDamage(Damage);
            if (!health.IsAlive())
            {
                health.Deactivate();
            }

        }
        DestroyGameObject();
    }
}