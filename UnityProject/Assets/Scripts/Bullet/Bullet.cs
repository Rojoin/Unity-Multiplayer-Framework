using System;
using System.Collections;
using System.Threading;
using RojoinNetworkSystem;
using UnityEngine;

/// <summary>
/// Class for the BulletClass
/// </summary>
public class Bullet : MonoBehaviour, INetObject
{
    private Action<Bullet> killAction;
    [SerializeField] float Velocity = 400f;
    [SerializeField] public float Damage = 1f;
    public static float maxAliveTime = 7f;

    private Vector3 direction;

    public int ID;
    private Coroutine startBullet;
    private NetObject _netObject = new();

    public void Init(Action<Bullet> onKill)
    {
        killAction = onKill;
    }

    private IEnumerator Start()
    {
        Vector3 startPos = this.transform.position;
        Vector3 endPos = startPos + transform.forward * 10f;
        float distance = Vector3.Distance(startPos, endPos);
        float travelTime = distance / Velocity;
        float elapsedTime = 0;

        while (elapsedTime < travelTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / travelTime;
            float height = Mathf.Sin(Mathf.PI * t) * 15;
            transform.position = Vector3.Lerp(startPos, endPos, t) + Vector3.up * height;
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
        gameObject.SetActive(false);
        SendDeleteMessage();
        // Destroy(this.gameObject);
    }

    public void StartBullet()
    {
        if (startBullet != null)
        {
            StopCoroutine(startBullet);
        }

       // startBullet = StartCoroutine(OnStart());
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


    public int GetID()
    {
        return _netObject.id;
    }

    public int GetOwner()
    {
        return _netObject.owner;
    }

    public NetObject GetObject()
    {
        return _netObject;
    }

    public TRS GetTRS()
    {
        return transform.GetTRS();
    }

    public void SetTRS(TRS trs, TRSFlags flags)
    {
        transform.SetTRS(trs, flags);
    }

    public void SendDeleteMessage()
    {
        NetObjectFactory.Instance.SendDeleteMessage(GetObject());
    }
}