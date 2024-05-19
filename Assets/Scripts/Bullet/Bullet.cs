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
    [SerializeField] float Velocity  = 400f;
    [SerializeField] public float Damage  = 1f;
    public static float maxAliveTime = 7f;

    private Vector3 direction;

    public int ID;
    private Coroutine startBullet;
    public void Init(Action<Bullet> onKill)
    {
        killAction = onKill;
    }

    private IEnumerator OnStart()
    {
        float timer = 0;
        direction = transform.forward;
        while (gameObject.activeSelf)
        {
            Debug.Log("Im Here");
            timer += Time.deltaTime;
            //TODO:CHANGE for current player direction
            transform.position += Time.deltaTime * Velocity * direction;
            if (timer > maxAliveTime)
            {
                killAction(this);
            }

            yield return null;
        }

        DestroyGameObject();
    }



    /// <summary>
    /// Set bullet spawnPosition
    /// </summary>
    /// <param name="spawnPosition">Spawn position of the bullet</param>
    public void SetStartPosition(Transform spawnPosition)
    {
        transform.position = spawnPosition.position;
        transform.forward = spawnPosition.forward;
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
        if (startBullet != null)
        {
            StopCoroutine(startBullet);
        }
        startBullet = StartCoroutine(OnStart());
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

    }
}